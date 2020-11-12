using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using QuartzExtensions.Utils.WeeklyReport;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using Renci.SshNet.Common;
using UserOperations.Models;
using UserOperations.Services.Interfaces;

namespace TabletRequestService
{
    public class TabletRequestService
    {        
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        private readonly ElasticClientFactory _elasticClientFactory;        
        private string baseUrl;
        // private string baseUrl = "https://heedbookapitest.westeurope.cloudapp.azure.com";
        // private string baseUrl = "https://heedbookapi.northeurope.cloudapp.azure.com";
        private string testImagePath = "./UploadFiles/TestImage.jpg";
        private string testVideoPath = "./UploadFiles/TestVideo.mkv";
        private List<Task> tabletTasks;
        private IGenericRepository _repository;
        private Object lockObject = new Object();
        private Device _device;
        private Company _company;
        private ApplicationUser _user;
        private string JWTtoken;
        private IConfiguration _configuration;
        public TabletRequestService(SftpClient sftpClient,
            SftpSettings sftpSettings,
            ElasticClientFactory elasticClientFactory,
            IGenericRepository repository,
            IConfiguration configuration)
        {
            _sftpClient = sftpClient;
            _sftpSettings = sftpSettings;
            _elasticClientFactory = elasticClientFactory;
            _repository = repository;
            _configuration = configuration;
            baseUrl = _configuration.GetSection("Url").GetValue<string>("baseUrl");
        }
        public void Run(TabletRequestRun message)
        {
            System.Console.WriteLine($"Received Message");
            System.Console.WriteLine(JsonConvert.SerializeObject(message));
            var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{Path}");
            _log.SetArgs(message.RequestName);

            _device = _repository.GetAsQueryable<Device>().FirstOrDefault(p => p.DeviceId == message.DeviceId);
            _company = _repository.GetAsQueryable<Company>().FirstOrDefault(p => p.CompanyId == message.CompanyId);
            _user = _repository.GetAsQueryable<ApplicationUser>().FirstOrDefault(p => p.Id == message.ApplicationUserId);

            System.Console.WriteLine(_device is null);
            System.Console.WriteLine(_company is null);
            System.Console.WriteLine(_user is null);
            try
            {
                var begTime = DateTime.Now;  

                switch(message.RequestName)
                {
                    case "GenerateTokenRequest":
                        GenerateTokenRequest().Wait();
                        break;
                    case "DeviceGenerateTokenRequest":               
                        DeviceGenerateTokenRequest().Wait();
                        break;
                    case "AccountChangePasswordRequest":
                        GenerateTokenRequest().Wait();
                        AccountChangePasswordRequest().Wait();
                        break;
                    case "CampaignContentRequest":
                        GenerateTokenRequest().Wait();
                        CampaignContentRequest().Wait();
                        break;
                    case "DeviceEmployeeRequest":
                        GenerateTokenRequest().Wait();
                        DeviceEmployeeRequest().Wait();
                        break;
                    case "CampaignContentCampaignRequest":
                        GenerateTokenRequest().Wait();
                        CampaignContentCampaignRequest().Wait();
                        break;
                    case "DemonstrationV2FlushStatsRequest":
                        GenerateTokenRequest().Wait();
                        DemonstrationV2FlushStatsRequest().Wait();
                        break;
                    case "DemonstrationV2PoolAnswerRequest":
                        GenerateTokenRequest().Wait();
                        DemonstrationV2PoolAnswerRequest().Wait();
                        break;
                    case "SessionAlertNotSmileRequest":
                        SessionAlertNotSmileRequest().Wait();
                        break;
                    case "FillingFileFrameRequest":
                        if(!_company.IsExtended)
                            FillingFileFrameRequest().Wait();
                        break;
                    case "FaceRequest":
                        if(!_company.IsExtended)
                            FaceRequest().Wait();
                        break;
                    case "LogSaveRequest":
                        LogSaveRequest().Wait();
                        break;
                    case "VideoSaveInfoRequest":
                        VideoSaveInfoRequest().Wait();
                        break;
                }
                System.Console.WriteLine($"TaskId: {Task.CurrentId}\nIsExtended: {_company.IsExtended}\ntime: {DateTime.Now.Subtract(begTime)}");
            }
            catch (Exception e)
            {
                _log.Fatal($"exception occured {e}");
                System.Console.WriteLine(e);
            }
        }
        private async Task GenerateTokenRequest()
        {
            var url = $"{baseUrl}/api/Account/GenerateToken";
                
            var request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";

            var model = new AccountAuthorization
            {
                UserName = _user.Email,
                Password = "Test_User12345"
            };
            
            var json = JsonConvert.SerializeObject(model);
            var data = Encoding.ASCII.GetBytes(json);              

            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = (await request.GetResponseAsync());
            System.Console.WriteLine($"responce: {JsonConvert.SerializeObject(responce)}"); 
            
            var responceStream = responce.GetResponseStream();  
            var SR = new StreamReader(responceStream, Encoding.UTF8);
            var str = SR.ReadToEnd();
            JWTtoken = str;   
            System.Console.WriteLine(str);         
        }
        private async Task DeviceGenerateTokenRequest()
        {
            var url = $"{baseUrl}/api/Device/GenerateToken";
                
            var request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";
            var code = _device.Code;
            var data = Encoding.ASCII.GetBytes($"\"{code}\"");              
            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = (await request.GetResponseAsync());
            // System.Console.WriteLine($"responce: {JsonConvert.SerializeObject(responce)}");
            
            var responceStream = responce.GetResponseStream();  
            var SR = new StreamReader(responceStream, Encoding.UTF8);
            var str = SR.ReadToEnd();
            System.Console.WriteLine(str);    
        }
        private async Task AccountChangePasswordRequest()
        {
            var url = $"{baseUrl}/api/Account/ChangePassword";
                
            var request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";
            request.Headers.Add("Authorization", $"Bearer {JWTtoken}");

            var model = new AccountAuthorization
            {
                UserName = _user.Email,
                Password = "Test_Password12345"
            };
            
            var json = JsonConvert.SerializeObject(model);
            var data = Encoding.ASCII.GetBytes(json);              

            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = (await request.GetResponseAsync());
            // System.Console.WriteLine($"responce: {JsonConvert.SerializeObject(responce)}");  

            var responceStream = responce.GetResponseStream();  
            var SR = new StreamReader(responceStream, Encoding.UTF8);
            var str = SR.ReadToEnd();
            System.Console.WriteLine(str);             
        }
        private async Task CampaignContentRequest()
        {
            var url = $"{baseUrl}/api/CampaignContent/Content?inActive=false&screenshot=false";
                
            var request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";
            request.Headers.Add("Authorization", $"Bearer " + JWTtoken);

            var responce = (await request.GetResponseAsync());
            // System.Console.WriteLine($"responce: {JsonConvert.SerializeObject(responce)}"); 

            var responceStream = responce.GetResponseStream();  
            var SR = new StreamReader(responceStream, Encoding.UTF8);
            var str = SR.ReadToEnd();
            System.Console.WriteLine(str);                
        }
        private async Task DeviceEmployeeRequest()
        {
            var url = $"{baseUrl}/api/Device/EmployeeList?active=true";
                
            var request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";
            request.Headers.Add("Authorization", $"Bearer " + JWTtoken);

            var responce = (await request.GetResponseAsync());
            // System.Console.WriteLine($"responce: {JsonConvert.SerializeObject(responce)}");   

            var responceStream = responce.GetResponseStream();  
            var SR = new StreamReader(responceStream, Encoding.UTF8);
            var str = SR.ReadToEnd();
            System.Console.WriteLine(str);    
        }
        private async Task CampaignContentCampaignRequest()
        {
            var url = $"{baseUrl}/api/CampaignContent/Campaign?isActual=false";
                
            var request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";
            request.Headers.Add("Authorization", $"Bearer " + JWTtoken);

            var responce = (await request.GetResponseAsync());
            // System.Console.WriteLine($"responce: {JsonConvert.SerializeObject(responce)}");   

            var responceStream = responce.GetResponseStream();  
            var SR = new StreamReader(responceStream, Encoding.UTF8);
            var str = SR.ReadToEnd();
            System.Console.WriteLine(str);    
        }
        private async Task DemonstrationV2FlushStatsRequest()
        {
            var url = $"{baseUrl}/api/DemonstrationV2/FlushStats";
                
            var request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";
            request.Headers.Add("Authorization", $"Bearer " + JWTtoken);
            var campaignContentId = _repository.GetAsQueryable<CampaignContent>().FirstOrDefault(p => p.Campaign.CompanyId == _company.CompanyId).CampaignContentId;
            var model = new List<SlideShowSession>
            {
                new SlideShowSession
                {
                    BegTime = DateTime.Now,
                    EndTime = DateTime.Now,
                    CampaignContentId = campaignContentId,
                    DeviceId = _device.DeviceId,
                    ContentType = "url",
                    Url = "https://www.heedbook.com/"
                }
            };
            var json = JsonConvert.SerializeObject(model);
            var data = Encoding.ASCII.GetBytes(json);

            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = (await request.GetResponseAsync());
            // System.Console.WriteLine($"responce: {JsonConvert.SerializeObject(responce)}");   

            var responceStream = responce.GetResponseStream();  
            var SR = new StreamReader(responceStream, Encoding.UTF8);
            var str = SR.ReadToEnd();
            System.Console.WriteLine(str);   
        }
        private async Task DemonstrationV2PoolAnswerRequest()
        {
            var url = $"{baseUrl}/api/DemonstrationV2/PollAnswer";
                
            var request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";
            request.Headers.Add("Authorization", $"Bearer " + JWTtoken);
            var campaignContentId = _repository.GetAsQueryable<CampaignContent>().FirstOrDefault(p => p.Campaign.CompanyId == _company.CompanyId).CampaignContentId;
            var model = new CampaignContentAnswerModel
            {
                Answer = "yes",
                AnswerText = "test offer",
                CampaignContentId = campaignContentId,
                DeviceId = _device.DeviceId,
                ApplicationUserId = _user.Id,
                Time = DateTime.Now
            };
            var json = JsonConvert.SerializeObject(model);
            var data = Encoding.ASCII.GetBytes(json);

            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = (await request.GetResponseAsync());
            // System.Console.WriteLine($"responce: {JsonConvert.SerializeObject(responce)}");    

            var responceStream = responce.GetResponseStream();  
            var SR = new StreamReader(responceStream, Encoding.UTF8);
            var str = SR.ReadToEnd();
            System.Console.WriteLine(str);  
        }
        private async Task SessionAlertNotSmileRequest()
        {
            var url = $"{baseUrl}/api/Session/AlertNotSmile";
                
            var request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";
            request.Headers.Add("Authorization", $"Bearer " + JWTtoken);
            var model = new AlertModel
            {
                ApplicationUserId = _user.Id,
                DeviceId = _device.DeviceId
            };
            var json = JsonConvert.SerializeObject(model);
            var data = Encoding.ASCII.GetBytes(json);

            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = (await request.GetResponseAsync());
            // System.Console.WriteLine($"responce: {JsonConvert.SerializeObject(responce)}");  

            var responceStream = responce.GetResponseStream();  
            var SR = new StreamReader(responceStream, Encoding.UTF8);
            var str = SR.ReadToEnd();
            System.Console.WriteLine(str);     
        }
        private async Task FillingFileFrameRequest()
        {
            var url = $"{baseUrl}/api/FillingFileFrame/FillingFileFrame";
                
            var request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";
            request.Headers.Add("Authorization", $"Bearer " + JWTtoken);
            var fileFrames = new List<FileFramePostModel>
            {
                new FileFramePostModel
                {
                    Age = 20,
                    Gender = "male",
                    Yaw = 12d,
                    Smile = 0.7d,
                    ApplicationUserId = _user.Id,
                    DeviceId = _device.DeviceId,
                    Time = DateTime.Now,
                    Descriptor = new double[]{0.036590903997421265,-0.45248401165008545,0.22564370930194855,0.36514058709144592,-1.6246206760406494,-2.0423965454101562,2.5401210784912109,-1.322155237197876,0.351826548576355,0.85817372798919678,-1.2903289794921875,-0.3543856143951416,-2.1267626285552979,1.2695562839508057,0.044435508549213409,0.46538078784942627,0.69430506229400635,-0.14751529693603516,0.1611286997795105,0.14215992391109467,0.056277245283126831,1.1287029981613159,-0.41836261749267578,-0.10298246145248413,0.0481702983379364,0.91841471195220947,-0.18365117907524109,2.144636869430542,-1.0299328565597534,-0.19180877506732941,1.332775354385376,1.2243648767471313,-1.2739658355712891,0.48727923631668091,-0.10609912872314453,-0.031125515699386597,0.075899720191955566,0.11165764927864075,0.15337711572647095,0.54946470260620117,0.31726476550102234,0.16357937455177307,0.31238070130348206,0.19086311757564545,0.88108783960342407,-0.86680620908737183,-0.66334843635559082,-0.771631121635437,0.68298333883285522,-0.87927508354187012,1.3423969745635986,0.37405508756637573,0.568973183631897,0.34963950514793396,-0.38855266571044922,-1.4399445056915283,-0.88864469528198242,0.16134749352931976,1.561267614364624,-0.86775344610214233,-0.47880291938781738,0.31511965394020081,-0.55011951923370361,1.3043991327285767,1.2294207811355591,0.72473526000976562,-1.024371862411499,-0.0040703956037759781,-0.69051933288574219,2.5037379264831543,0.4346279501914978,1.045583963394165,-0.97079497575759888,-0.12502226233482361,1.5454915761947632,0.13307063281536102,1.4339585304260254,-0.38952672481536865,-1.3023710250854492,-1.2223318815231323,0.16481673717498779,-0.56291729211807251,0.49480587244033813,0.94554305076599121,1.4339380264282227,0.80563944578170776,-2.0156407356262207,0.95784777402877808,1.4279853105545044,0.607536792755127,1.023460865020752,-0.15222486853599548,0.31573516130447388,1.0529937744140625,-0.18251743912696838,1.282428503036499,-1.0239853858947754,1.9679113626480103,0.38208156824111938,0.49910777807235718,-0.513724148273468,-0.86307615041732788,-1.0142831802368164,-2.1140120029449463,-0.6608583927154541,-0.14615520834922791,-0.59838622808456421,-1.9315247535705566,-0.411886602640152,-0.99490797519683838,-0.57940387725830078,-1.8917374610900879,-0.35594978928565979,1.2458157539367676,-0.38197484612464905,-0.17011290788650513,1.7388226985931396,-0.36883831024169922,0.21994265913963318,1.715409517288208,-0.21771037578582764,0.58738851547241211,0.8016207218170166,-1.1370444297790527,0.068325743079185486,-1.4716193675994873,-0.81747221946716309,1.5442755222320557,1.8671140670776367,-0.34722727537155151,0.92099565267562866,0.18952366709709167,0.83914989233016968,1.4113942384719849,1.3720467090606689,-0.29531434178352356,0.50972867012023926,0.32961529493331909,-0.055112868547439575,0.45779469609260559,0.013338536024093628,-0.83108896017074585,0.2949138879776001,1.273682713508606,-0.6741032600402832,-1.238231897354126,-0.38767480850219727,-0.94745725393295288,-0.72299331426620483,0.46981585025787354,-0.5934290885925293,0.90009933710098267,0.51801574230194092,0.39248514175415039,-0.42750087380409241,-0.65880966186523438,-0.640190064907074,0.17740993201732635,-0.079375565052032471,1.3008708953857422,0.073983848094940186,1.2963384389877319,0.074476301670074463,0.22491362690925598,0.30076515674591064,-1.0740416049957275,0.55824440717697144,-1.3048701286315918,-1.3282091617584229,-0.002783358097076416,0.42691045999526978,1.3793721199035645,0.18133768439292908,1.952617883682251,0.080122321844100952,0.35434269905090332,0.26210358738899231,-1.5490326881408691,1.0906952619552612,0.003831028938293457,2.3049750328063965,-0.41782695055007935,-0.56488239765167236,0.33841279149055481,0.43550765514373779,0.14253807067871094,2.0234646797180176,-0.87994349002838135,1.6425745487213135,0.44411900639533997,0.133477121591568,0.62813782691955566,0.81409317255020142,0.55506044626235962,1.3309752941131592,0.39953243732452393,0.9426233172416687,1.438948392868042,2.5096702575683594,0.864279568195343,-0.1265733540058136,-1.4934309720993042,-0.4659804105758667,0.73956608772277832,1.0798991918563843,0.38360434770584106,0.23807832598686218,0.090887978672981262,-0.86277735233306885,0.36729925870895386,0.12041464447975159,0.1985383927822113,1.4588463306427002,-0.043898958712816238,-1.064484715461731,-2.3532357215881348,-0.16514182090759277,-1.2512420415878296,1.0468472242355347,1.0758881568908691,1.0267729759216309,-0.52271848917007446,-0.16590192914009094,0.91694164276123047,-0.751372754573822,-0.13189452886581421,0.16560018062591553,-0.95956635475158691,-0.378156840801239,-0.56824707984924316,-0.49931538105010986,-0.30240455269813538,1.8659197092056274,-1.171684741973877,0.92449486255645752,0.22399097681045532,-1.0539145469665527,-1.2861649990081787,-0.017867892980575562,-0.19017177820205688,-1.0002566576004028,2.1189851760864258,-0.34067511558532715,1.7240524291992188,0.90872842073440552,2.3485360145568848,0.1661822646856308,1.6330459117889404,2.1642427444458008,-0.34614002704620361,0.6874312162399292,-1.7950029373168945,0.54190349578857422,0.37255764007568359,0.44388002157211304,-0.74723386764526367},
                    FaceArea = 400,
                    Top = 20,
                    Left = 20,
                    VideoHeight = 400,
                    VideoWidth = 600
                }
            };
            var json = JsonConvert.SerializeObject(fileFrames);
            var data = Encoding.ASCII.GetBytes(json);

            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = (await request.GetResponseAsync());
            // System.Console.WriteLine($"responce: {JsonConvert.SerializeObject(responce)}");   

            var responceStream = responce.GetResponseStream();  
            var SR = new StreamReader(responceStream, Encoding.UTF8);
            var str = SR.ReadToEnd();
            System.Console.WriteLine(str);        
        }
        private async Task FaceRequest()
        {
            var url = $"{baseUrl}/face/Face?deviceId={_device.DeviceId}&companyId={_company.CompanyId}&description=true&emotions=true&headpose=true&attributes=true";
            
            var imageBase64String = "";
            using (Image image = Image.FromFile(testImagePath))
            {
                using (MemoryStream m = new MemoryStream())
                {
                    image.Save(m, image.RawFormat);
                    byte[] imageBytes = m.ToArray();
                    imageBase64String = Convert.ToBase64String(imageBytes);
                }
            }            
            var request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";
            
            var data = Encoding.ASCII.GetBytes("\"" + imageBase64String + "\"");

            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = (await request.GetResponseAsync());
            // System.Console.WriteLine($"responce: {JsonConvert.SerializeObject(responce)}"); 

            var responceStream = responce.GetResponseStream();  
            var SR = new StreamReader(responceStream, Encoding.UTF8);
            var str = SR.ReadToEnd();
            System.Console.WriteLine(str);    
        }
        private async Task LogSaveRequest()
        {
            var url = $"{baseUrl}/logs/LogSave";
            string boundary = "----" + System.Guid.NewGuid();

            string Dateiname = Path.GetFileName(testImagePath);

            // Read file data
            FileStream fs = new FileStream(testImagePath, FileMode.Open, FileAccess.Read);
            byte[] data = new byte[fs.Length];
            fs.Read(data, 0, data.Length);
            fs.Close();

            // Generate post objects
            Dictionary<string,object> postParameters = new Dictionary<string,object>();
            //postParameters.Add("name", "file");
            postParameters.Add("file", new FormUpload.FileParameter(data, Dateiname, "application/octet-stream"));

            // Create request and receive response
            string postURL = url;
            string userAgent = "Someone";
            HttpWebResponse webResponse = FormUpload.MultipartFormDataPost(postURL, userAgent, postParameters);

            // Process response
            StreamReader responseReader = new StreamReader(webResponse.GetResponseStream());
            var fullResponse = responseReader.ReadToEnd();
            // System.Console.WriteLine($"responce: {fullResponse}");
            webResponse.Close();
            
            System.Console.WriteLine(fullResponse);    
        }
        private async Task VideoSaveInfoRequest()
        {
            var url = "";
            var stringFormat = "yyyyMMddHHmmss";
            var begTime = DateTime.Now.ToString(stringFormat);
            var endTime = DateTime.Now.AddMinutes(2).ToString(stringFormat);
            if(_company.IsExtended)
            {
                url = $"{baseUrl}/user/VideoSaveInfo?deviceId={_device.DeviceId}&begTime={begTime}&duration={120}&applicationUserId={_user.Id}&endTime={endTime}";
            }
            else
            {
                url = $"{baseUrl}/user/VideoSaveInfo?deviceId={_device.DeviceId}&begTime={begTime}&duration={120}";        
            }
            
            var request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";
            request.Headers.Add("Authorization", $"Bearer " + JWTtoken);
            
            var responce = (await request.GetResponseAsync());
            // System.Console.WriteLine($"responce: {JsonConvert.SerializeObject(responce)}");

            var responceStream = responce.GetResponseStream();  
            var SR = new StreamReader(responceStream, Encoding.UTF8);
            var str = SR.ReadToEnd();
            var fileVideo = JsonConvert.DeserializeObject<FileVideo>(str);
            // System.Console.WriteLine(fileVideo.FileName);
            SendVideoOnFtpServer(fileVideo.FileName);
            var fullResponse = SR.ReadToEnd();
            System.Console.WriteLine(fullResponse);
        }
        private async Task SendVideoOnFtpServer(string fileName)
        {
            await _sftpClient.UploadAsync(testVideoPath, "videos/", fileName);
        }
    }
    public class UploadFile
    {
        public UploadFile()
        {
            ContentType = "application/octet-stream";
        }
        public string Name { get; set; }
        public string Filename { get; set; }
        public string ContentType { get; set; }
        public Stream Stream { get; set; }
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