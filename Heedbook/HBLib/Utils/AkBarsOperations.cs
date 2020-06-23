using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HBLib.Model;
using Newtonsoft.Json;

namespace HBLib.Utils
{
    public class AkBarsOperations
    {
        private readonly HeedbookSettingsInAkBars _akbarsSettings;
        private readonly SftpClient _sftpClient;
        public AkBarsOperations(
            HeedbookSettingsInAkBars akbarsSettings,
            SftpClient sftpClient)
        {
            _akbarsSettings = akbarsSettings;
            _sftpClient = sftpClient;
        }
        public async Task<Guid?> FindClientIdInAkBarsApi(string path)
        {
            var fileName = Path.GetFileName(path);
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
                System.Console.WriteLine($"WebException");
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
            System.Console.WriteLine($"create new customer in akbars");
                
            string boundary = "----" + System.Guid.NewGuid();
            string fileName = Path.GetFileName(avatarName);

            // Read file data
            byte[] data;
            if(fileByteArray is null)
            {
                var file = await _sftpClient.DownloadFromFtpAsMemoryStreamAsync($"clientavatars/{avatarName}");
                data = file.ToArray();  
                System.Console.WriteLine($"create customer. file array is null");          
            }
            else
                data = fileByteArray;
            
            System.Console.WriteLine($"data.Length in create: {data.Length}");

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
                System.Console.WriteLine($"create customer responce: {JsonConvert.SerializeObject(responceModel)}");
                if(!responceModel.success)
                    return null;

                var dictionary = GetDataFromToken(responceModel.result.customerToken);
                System.Console.WriteLine($"create customer in akBars, validate token dictionary: {JsonConvert.SerializeObject(dictionary)}");
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
        public Dictionary<string, string> GetDataFromToken(string token)
        {
            var jwt = new JwtSecurityToken(token);
            var claims = jwt.Payload.ToDictionary(key => key.Key.ToString(), value => value.Value.ToString());
            return claims;        
        }
    }
}