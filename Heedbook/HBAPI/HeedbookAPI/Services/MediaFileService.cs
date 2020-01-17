using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using HBLib.Utils;

namespace UserOperations.Services
{
    public class MediaFileService
    {
        private readonly LoginService _loginService;
        private readonly SftpClient _sftpClient;
        private readonly string _containerName;
        private Dictionary<string, string> userClaims;
      

        public MediaFileService(
            LoginService loginService,
            SftpClient sftpClient
            )
        {
            _loginService = loginService;
            _sftpClient = sftpClient;
            _containerName = "media";         
        }

        public async Task<object> FileGet(
                [FromQuery(Name= "containerName")] string containerName = null, 
                [FromQuery(Name = "fileName")] string fileName = null,
                [FromQuery(Name = "expirationDate")]  DateTime? expirationDate = null)
        {
            var companyId = _loginService.GetCurrentCompanyId();
            containerName = containerName ?? _containerName; 
            if (expirationDate == null) expirationDate = default(DateTime);     

            if (fileName != null)
            {
                var result = _sftpClient.GetFileLink(containerName + "/" + companyId, fileName, (DateTime)expirationDate);
                return result;
            }
            else
            {
                await _sftpClient.CreateIfDirNoExistsAsync(_containerName + "/" + companyId);
                var files = await _sftpClient.GetFileNames(_containerName+"/"+companyId);  
                //List<object> result = new List<object>();
                //foreach(var file in files)
                //{
                //    result.Add( _sftpClient.GetFileLink(containerName + "/" + companyId, file, (DateTime)expirationDate));
                //}
                return files;
            }            
        }
      
        public async Task<object> FilePost([FromForm] IFormCollection formData)
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
                //memoryStream.Close();
            }
            await Task.WhenAll(tasks);

            List<object> result = new List<object>();   
            foreach (var file in fileNames)
            {
                result.Add( _sftpClient.GetFileLink(containerName + "/" + companyId, file, default(DateTime)));
            }
            // _log.Info("MediaFile/File POST finished"); 
            return result;            
        }
        public async Task<object> FilePut([FromForm] IFormCollection formData)
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
                var result = _sftpClient.GetFileLink(containerName + "/" + companyId, fn, default(DateTime));
                // _log.Info("MediaFile/File PUT finished"); 
                return result;            
        }
        public async Task<object> FileDelete(
                [FromQuery] string containerName = null, 
                [FromQuery] string fileName = null)
        {
            // _log.Info("MediaFile/File DELETE started");
            var companyId = _loginService.GetCurrentCompanyId();
            var container = containerName?? _containerName;
            await Task.Run(() => _sftpClient.DeleteFileIfExistsAsync($"{container}/{companyId}/{fileName}"));
            // _log.Info("MediaFile/File DELETE finished"); 
            return "OK";
        }
    }
}