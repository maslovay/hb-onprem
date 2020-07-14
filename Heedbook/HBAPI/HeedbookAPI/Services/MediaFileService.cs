using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.AspNetCore.Http;
using HBLib.Utils.Interfaces;
using HBLib.Utils;
using HBLib;
using System.Net;
using Newtonsoft.Json;
using System.Text;
using RabbitMqEventBus.Events;

namespace UserOperations.Services
{
    public class MediaFileService
    {
        private readonly ILoginService _loginService;
        private readonly ISftpClient _sftpClient;
        private readonly IFileRefUtils _fileRef;
        private readonly string _containerName;
        private readonly ElasticClient _log;
        private readonly URLSettings _urlSettings;
        public MediaFileService(
            ILoginService loginService,
            ISftpClient sftpClient,
            IFileRefUtils fileRef,
            ElasticClient log,
            URLSettings urlSettings)
        {
            _loginService = loginService;
            _sftpClient = sftpClient;
            _fileRef = fileRef;
            _containerName = "media";
            _log = log;
            _urlSettings = urlSettings;
        }

        public async Task<object> FileGet(
                [FromQuery(Name = "containerName")] string containerName = null,
                [FromQuery(Name = "fileName")] string fileName = null,
                [FromQuery(Name = "expirationDate")] DateTime? expirationDate = null)
        {
            var companyId = _loginService.GetCurrentCompanyId();
            containerName = containerName ?? _containerName;
            if (expirationDate == null) expirationDate = default(DateTime);

            if (fileName != null)
            {
                var result = _fileRef.GetFileLink(containerName + "/" + companyId, fileName, (DateTime)expirationDate);
                return result;
            }
            else
            {
                await _sftpClient.CreateIfDirNoExistsAsync(_containerName + "/" + companyId);
                var files = await _sftpClient.GetFileNames(_containerName + "/" + companyId);
                List<string> result = new List<string>();
                foreach (var file in files)
                {
                    result.Add(_fileRef.GetFileLink(containerName + "/" + companyId, file, (DateTime)expirationDate));
                }
                return result;
            }
        }

        public async Task<List<string>> FilePost([FromForm] IFormCollection formData)
        {
            // _log.Info("MediaFile/File POST started");                 
            var companyId = _loginService.GetCurrentCompanyId();
            var containerNameParam = formData.FirstOrDefault(x => x.Key == "containerName");
            var containerName = containerNameParam.Value.Any() ? containerNameParam.Value.ToString() : _containerName;

            var tasks = new List<Task>();
            var fileNames = new List<string>();
            foreach (var file in formData.Files)
            {
                FileInfo fileInfo = new FileInfo(file.FileName);
                var fn = Guid.NewGuid() + fileInfo.Extension;
                var memoryStream = file.OpenReadStream();
                tasks.Add(_sftpClient.UploadAsMemoryStreamAsync(memoryStream, $"{containerName}/{companyId}", fn, true));
                fileNames.Add(fn);
                await SendMessageCreateGif($"{containerName}/{companyId}/{fn}");
                //memoryStream.Close();
            }
            await Task.WhenAll(tasks);

            List<string> result = new List<string>();
            foreach (var file in fileNames)
            {
                result.Add(_fileRef.GetFileLink(containerName + "/" + companyId, file, default));
            }
            // _log.Info("MediaFile/File POST finished"); 
            return result;
        }
        public async Task<string> FilePut([FromForm] IFormCollection formData)
        {
            // _log.Info("MediaFile/File PUT started");                 
            var companyId = _loginService.GetCurrentCompanyId();
            var containerNameParam = formData.FirstOrDefault(x => x.Key == "containerName");
            var containerName = containerNameParam.Value.Any() ? containerNameParam.Value.ToString() : _containerName;
            var fileName = formData.FirstOrDefault(x => x.Key == "fileName").Value.ToString();

            await Task.Run(() => _sftpClient.DeleteFileIfExistsAsync($"{containerName}/{companyId}/{fileName}"));

            FileInfo fileInfo = new FileInfo(formData.Files[0].FileName);
            var fn = Guid.NewGuid() + fileInfo.Extension;
            var memoryStream = formData.Files[0].OpenReadStream();
            await _sftpClient.UploadAsMemoryStreamAsync(memoryStream, $"{containerName}/{companyId}", fn, true);
            // var result = new { 
            //     path =  await _sftpClient.GetFileUrl($"{containerName}/{companyId}/{fn}"), 
            //     ext = Path.GetExtension(fileName.Trim('.'))};
            var result = _fileRef.GetFileLink(containerName + "/" + companyId, fn, default);
            // _log.Info("MediaFile/File PUT finished"); 
            return result;
        }
        public async Task<object> FileDelete(
                [FromQuery] string containerName = null,
                [FromQuery] string fileName = null)
        {
            // _log.Info("MediaFile/File DELETE started");
            var companyId = _loginService.GetCurrentCompanyId();
            var container = containerName ?? _containerName;
            await Task.Run(() => _sftpClient.DeleteFileIfExistsAsync($"{container}/{companyId}/{fileName}"));
            // _log.Info("MediaFile/File DELETE finished"); 
            return "OK";
        }
        public async Task<List<string>> FilePostInContainer([FromForm] IFormCollection formData)
        {
            // _log.Info("MediaFile/File POST started");
            var containerNameParam = formData.FirstOrDefault(x => x.Key == "containerName");
            var fileNameParam = formData.FirstOrDefault(x => x.Key == "fileName");

            if(!containerNameParam.Value.Any())
                throw new ArgumentNullException("containerName in null");

            if(!fileNameParam.Value.Any())
                throw new ArgumentNullException("fileName in null");
            var containerName = containerNameParam.Value.ToString();
            var fileName = fileNameParam.Value.ToString();

            var tasks = new List<Task>();
            var fileNames = new List<string>();
            foreach (var file in formData.Files)
            {
                FileInfo fileInfo = new FileInfo(file.FileName);
                var memoryStream = file.OpenReadStream();
                tasks.Add(_sftpClient.UploadAsMemoryStreamAsync(memoryStream, $"{containerName}", fileName, true));
                fileNames.Add(fileName);
                //memoryStream.Close();
            }
            await Task.WhenAll(tasks);

            List<string> result = new List<string>();
            foreach (var file in fileNames)
            {
                result.Add(_fileRef.GetFileLink(containerName, file, default));
            }
            // _log.Info("MediaFile/File POST finished"); 
            return result;
        }
        public async Task TmpWebHook([FromForm] IFormCollection formData)
        {
            var resultLog = "";
            foreach(var FD in formData)
            {
                var fdString = $"{FD.Key} {FD.Value}\n";
                resultLog += fdString;
            }
            _log.Info(resultLog);
        }
        private async Task SendMessageCreateGif(string fileName)
        {
            var url = _urlSettings.Host + $"user/VideoToSound/VideoToGif";
                
            var request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";                

            var model = new VideoContentToGifRun
            {
                Path = $"{fileName}"                    
            };
            var json = JsonConvert.SerializeObject(model);
            var data = Encoding.ASCII.GetBytes(json);

            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = request.GetResponseAsync();
        }
    }
}