using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMqEventBus.Events;
using HBLib;
using HBData;
using PersonDetectionAkBarsService.Exceptions;
using PersonDetectionAkBarsService.Models;
using System.Net;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;

namespace PersonDetectionAkBarsService
{
    public class PersonDetection
    {
        private readonly RecordsContext _context;
        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly DescriptorCalculations _calc;
        private readonly HeedbookSettingsInAkBars _akbarsSettings;
        private readonly SftpClient _sftpClient;

        public PersonDetection(
            IServiceScopeFactory factory,
            ElasticClientFactory elasticClientFactory,
            DescriptorCalculations calc,
            HeedbookSettingsInAkBars akbarsSettings,
            SftpClient sftpClient
        )
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _elasticClientFactory = elasticClientFactory;
            _calc = calc;
            _akbarsSettings = akbarsSettings;
            _sftpClient = sftpClient;
        }

        public async Task Run(PersonDetectionRun message)
        {
            System.Console.WriteLine("start");
            var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{deviceIds}");
            _log.Info("Function started");
            _log.SetArgs(JsonConvert.SerializeObject(message.DeviceIds));
            try
            {
                var begTime = DateTime.Now.AddMonths(-1);
                var companyIds = _context.Devices.Where(x => message.DeviceIds.Contains(x.DeviceId)).Select(x => x.CompanyId).Distinct().ToList();

                //---dialogues for devices in company
                var dialogues = _context.Dialogues
                    .Include(p => p.DialogueClientProfile)
                    .Where(p => ( companyIds.Contains(p.Device.CompanyId)) && p.BegTime >= begTime)
                    .OrderBy(p => p.BegTime)
                    .ToList();

                foreach (var curDialogue in dialogues.Where(p => p.ClientId == null).ToList())
                {
                    var dialoguesProceeded = dialogues
                        .Where(p => p.ClientId != null && p.DeviceId == curDialogue.DeviceId)
                        .ToList();
                    
                    var clientId = await FindClientIdInAkBarsApi(curDialogue.DialogueClientProfile.FirstOrDefault().Avatar);
                    if(clientId is null)
                        continue;
                    try
                    {
                        CreateNewClient(curDialogue, clientId);
                    }
                    catch( Exception ex )
                    {
                        _log.Error($"client for dialogue {curDialogue.DialogueId} creation error: " + ex.Message);
                    }
                }
                _log.Info("Function finished");
            }
            catch (Exception e)
            {
                _log.Info($"Exception occured {e}");
                throw new PersonDetectionException(e.Message, e);
            }
            System.Console.WriteLine($"FunctionFinished");
        }

        public async Task<Guid?> FindClientIdInAkBarsApi(string fileName)
        {
            string boundary = "----" + System.Guid.NewGuid();

            var file = await _sftpClient.DownloadFromFtpAsMemoryStreamAsync($"clientavatars/{fileName}");
            byte[] data = file.ToArray();
            if(data.Length == 0)
                return null;

            System.Console.WriteLine($"data.Length in validate: {data.Length}");

            // Generate post objects
            Dictionary<string,object> postParameters = new Dictionary<string,object>();
            postParameters.Add("ProjectId", _akbarsSettings.ProjectId);
            postParameters.Add("ClientId", _akbarsSettings.ClientId);
            postParameters.Add("isCropped", false);
            postParameters.Add("image", new FormUpload.FileParameter(data, fileName, "application/octet-stream"));
            postParameters.Add("Modelid", 2);
            postParameters.Add("Authorization", "Bearer");

            // Create request and receive response
            string postURL = _akbarsSettings.ValidateCustomerUrl;
            string userAgent = "Someone";
            
            try
            {
                var webResponse = FormUpload.MultipartFormDataPost(postURL, userAgent, postParameters);
                // Process response
                StreamReader responseReader = new StreamReader(webResponse.GetResponseStream());
                var fullResponse = responseReader.ReadToEnd();
                var responceModel = JsonConvert.DeserializeObject<Dictionary<string, object>>(fullResponse);
                webResponse.Close();

                if(!(bool)responceModel["success"])
                    return await CreateNewCustomerInAkBarsApi(fileName, data);

                var dictionary = GetDataFromToken((string)responceModel["result"]);
                System.Console.WriteLine($"find client in akBars, validate token dictionary: {JsonConvert.SerializeObject(dictionary)}");
                
                if(dictionary.Keys.Contains("customerId") && dictionary["customerId"] != null)
                    return Guid.Parse(dictionary["customerId"]);
                else
                    return null;
            }
            catch(WebException e)
            {
                //When customer not registered in AkBars, AkBars Api responce BadRequest and throw WebException
                var wex = (WebException)e;
                var webResponseFromException = (HttpWebResponse)wex.Response;
                StreamReader responseReader = new StreamReader(webResponseFromException.GetResponseStream());
                var fullResponse = responseReader.ReadToEnd();
                System.Console.WriteLine($"fullexceptionResponce:\n{fullResponse}");

                var responceModel = JsonConvert.DeserializeObject<Dictionary<string, object>>(fullResponse);
                webResponseFromException.Close();

                if(!(bool)responceModel["success"])
                    return await CreateNewCustomerInAkBarsApi(fileName, data);              
                else
                    return null;
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
                return null;
            }
        }
        public async Task<Guid?> CreateNewCustomerInAkBarsApi(string avatarName, byte[] fileByteArray = null)
        {                
            string boundary = "----" + System.Guid.NewGuid();
            string fileName = Path.GetFileName(avatarName);

            // Read file data
            byte[] data;
            if(fileByteArray is null)
            {
                var file = await _sftpClient.DownloadFromFtpAsMemoryStreamAsync($"clientavatars/{avatarName}");
                data = file.ToArray();         
            }
            else
                data = fileByteArray;

            // Generate post objects
            Dictionary<string,object> postParameters = new Dictionary<string,object>();
            postParameters.Add("ProjectId", _akbarsSettings.ProjectId);
            postParameters.Add("ClientId", _akbarsSettings.ClientId);
            postParameters.Add("isCropped", false);
            postParameters.Add("image", new FormUpload.FileParameter(data, fileName, "application/octet-stream"));

            // Create request and receive response
            string postURL = _akbarsSettings.RegisterNewCustomerUrl;
            string userAgent = "Someone";
            HttpWebResponse webResponse = FormUpload.MultipartFormDataPost(postURL, userAgent, postParameters);

            // Process response
            try
            {
                StreamReader responseReader = new StreamReader(webResponse.GetResponseStream());
                var fullResponse = responseReader.ReadToEnd();
                var responceModel = JsonConvert.DeserializeObject<AkBarsRegistrationResponceModel>(fullResponse);
                webResponse.Close();
                if(!responceModel.success)
                    return null;

                var dictionary = GetDataFromToken(responceModel.result.customerToken);
                if(dictionary.Keys.Contains("customerId") && dictionary["customerId"] != null)
                    return Guid.Parse(dictionary["customerId"]);
                else
                    return null;
            }
            catch(Exception e)
            {
                System.Console.WriteLine($"Exception:\n {e}");
                return null;
            }            
        }
        public Guid? CreateNewClient(Dialogue curDialogue, Guid? clientId)
        {
            Company company = _context.Devices
                              .Where(x => x.DeviceId == curDialogue.DeviceId).Select(x => x.Company).FirstOrDefault();
            var findClient = _context.Clients//---search only client with status 3 (record context)
                        .Where(x => x.ClientId == clientId).FirstOrDefault();
            if (findClient != null)
            {
                findClient.LastDate = DateTime.UtcNow;
                curDialogue.ClientId = findClient.ClientId;
                _context.SaveChanges();
                return findClient.ClientId;
            }

            var dialogueClientProfile = _context.DialogueClientProfiles
                            .FirstOrDefault(x => x.DialogueId == curDialogue.DialogueId);
            if (dialogueClientProfile == null) return null;
            if (dialogueClientProfile.Age == null || dialogueClientProfile.Gender == null) return null;

            var activeStatusId = _context.Statuss
                            .Where(x => x.StatusName == "Active")
                            .Select(x => x.StatusId)
                            .FirstOrDefault();

            double[] faceDescr = new double[0];
            try
            {
                faceDescr = JsonConvert.DeserializeObject<double[]>(curDialogue.PersonFaceDescriptor);
            }
            catch { }
                Client client = new Client
                {
                    ClientId = (Guid)clientId,
                    CompanyId = (Guid)company?.CompanyId,
                    CorporationId = company?.CorporationId,
                    FaceDescriptor = faceDescr,
                    Age = (int)dialogueClientProfile?.Age,
                    Avatar = dialogueClientProfile?.Avatar,
                    Gender = dialogueClientProfile?.Gender,
                    StatusId = activeStatusId,
                    LastDate = curDialogue.EndTime
                };
            curDialogue.ClientId = client.ClientId;
            _context.Clients.Add(client);
            _context.SaveChanges();
            return client.ClientId;
        }
        public Dictionary<string, string> GetDataFromToken(string token)
        {
            var jwt = new JwtSecurityToken(token);
            var claims = jwt.Payload.ToDictionary(key => key.Key.ToString(), value => value.Value.ToString());
            return claims;        
        }
    }
    public static class FormUpload
    {
        private static readonly Encoding encoding = Encoding.UTF8;
        public static HttpWebResponse MultipartFormDataPost(string postUrl, string userAgent, Dictionary<string,object> postParameters)
        {
            string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
            string contentType = "multipart/form-data; boundary=" + formDataBoundary;
            byte[] formData = GetMultipartFormData(postParameters, formDataBoundary);
            return PostForm(postUrl, userAgent, contentType, formData);
        }

        private static HttpWebResponse PostForm(string postUrl, string userAgent, string contentType, byte[] formData)
        {
            HttpWebRequest request = WebRequest.Create(postUrl) as HttpWebRequest;

            if (request == null)
            {
                throw new NullReferenceException("request is not a http request");
            }

            // Set up the request properties.
            request.Method = "POST";
            request.ContentType = contentType;
            request.UserAgent = userAgent;
            request.CookieContainer = new CookieContainer();
            request.ContentLength = formData.Length;

            // You could add authentication here as well if needed:
            request.PreAuthenticate = true;
            request.AuthenticationLevel = System.Net.Security.AuthenticationLevel.MutualAuthRequested;
            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.Default.GetBytes("USER" + ":" + "PASSWORD")));

            // Send the form data to the request.


            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(formData, 0, formData.Length);
                requestStream.Close();
            }
            return request.GetResponse() as HttpWebResponse;
        }

        private static byte[] GetMultipartFormData(Dictionary<string,object> postParameters, string boundary)
        {
            Stream formDataStream = new System.IO.MemoryStream();
            bool needsCLRF = false;

            foreach (var param in postParameters)
            {
                // Thanks to feedback from commenters, add a CRLF to allow multiple parameters to be added.
                // Skip it on the first parameter, add it to subsequent parameters.
                if (needsCLRF)
                    formDataStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));

                needsCLRF = true;

                if (param.Value is FileParameter)
                {
                    FileParameter fileToUpload = (FileParameter)param.Value;

                    // Add just the first part of this param, since we will write the file data directly to the Stream
                    string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n",
                        boundary,
                        param.Key,
                        fileToUpload.FileName ?? param.Key,
                        fileToUpload.ContentType ?? "application/octet-stream");

                    formDataStream.Write(encoding.GetBytes(header), 0, encoding.GetByteCount(header));

                    // Write the file data directly to the Stream, rather than serializing it to a string.
                    formDataStream.Write(fileToUpload.File, 0, fileToUpload.File.Length);
                }
                else
                {
                    string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
                        boundary,
                        param.Key,
                        param.Value);
                    formDataStream.Write(encoding.GetBytes(postData), 0, encoding.GetByteCount(postData));
                }
            }

            // Add the end of the request.  Start with a newline
            string footer = "\r\n--" + boundary + "--\r\n";
            formDataStream.Write(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer));

            // Dump the Stream into a byte[]
            formDataStream.Position = 0;
            byte[] formData = new byte[formDataStream.Length];
            formDataStream.Read(formData, 0, formData.Length);
            formDataStream.Close();

            return formData;
        }
    
        public class FileParameter
        {
            public byte[] File { get; set; }
            public string FileName { get; set; }
            public string ContentType { get; set; }
            public FileParameter(byte[] file) : this(file, null) { }
            public FileParameter(byte[] file, string filename) : this(file, filename, null) { }
            public FileParameter(byte[] file, string filename, string contenttype)
            {
                File = file;
                FileName = filename;
                ContentType = contenttype;
            }
        }
    }
}