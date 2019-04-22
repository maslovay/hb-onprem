using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using UserOperations.AccountModels;
using HBData.Models;
using HBData.Models.AccountViewModels;
using UserOperations.Services;

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using HBData;
using HBLib.Utils;
using HBLib;
using Microsoft.Extensions.Primitives;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly SftpClient _sftpClient;
        private readonly string _containerName;

        public FileController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            SftpClient sftpClient
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _sftpClient = sftpClient;
            _containerName = "media";
        }
        #region File
        [HttpGet("File")]
        [SwaggerOperation(Description = "Return all files from sftp. If no parameters are passed return files from 'media', for loggined company")]
        public async Task<IEnumerable<string>> FileGet([FromQuery]string containerName = null, [FromQuery]string[] directoryNames = null, [FromQuery]string fileName = null)
        {
            string companyId = GetCompanyIdFromToken();
            if (companyId == null) return null; 

            IEnumerable<string> files = null;
            containerName = containerName ?? _containerName;
            if (fileName != null)
            {
                files = new List<string> { await _sftpClient.GetFileUrl(containerName + "/" + companyId + "/" + fileName) };
                return files;
            }
            else
                files = await _sftpClient.GetAllFilesUrl(containerName, directoryNames);
            return files;
        }

        [HttpPost("File")]
        [SwaggerOperation(Description = "Save file on sftp. Can take containerName in body or save to media container. Folder determined by company id in token")]
        public async Task<IEnumerable<string>> FilePost([FromHeader] string Authorization, [FromForm] IFormCollection formData)
        {
            var companyId = _loginService.GetDataFromToken(Authorization)["companyId"];
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
                fileNames.Add($"{containerName}/{companyId}/{fn}");
            }
            await Task.WhenAll(tasks);
            
            var urlTasks = new List<Task<String>>();
            foreach (var fileName in fileNames)
            {
                urlTasks.Add(_sftpClient.GetFileUrl(fileName));
            }
            var result = await Task.WhenAll(urlTasks);
            return result;
        }
        
        [HttpPut("File")]
        [SwaggerOperation(Description = "Remove old and save new file on sftp. Can take containerName in body or save to media container. Folder determined by company id in token")]
        public async Task<IEnumerable<string>> FilePut()
        {
            string companyId = GetCompanyIdFromToken();
            if (companyId == null) return null; 

            var provider = new MultipartMemoryStreamProvider();
            var form = await Request.ReadFormAsync();
            var files = form.Files;
            var containerNameParam = form.FirstOrDefault(x => x.Key == "containerName");
            var containerName = containerNameParam.Value.Count() != 0 ? containerNameParam.Value.ToString() : _containerName;
            var fileName = form.FirstOrDefault(x => x.Key == "fileName");

            await _sftpClient.DeleteFileIfExistsAsync(containerName+"/"+ companyId+"/"+ fileName);

            List<string> res = new List<string>();
            foreach (var file in files)
            {
                var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fs = memoryStream.ToArray();
                FileInfo f = new FileInfo(file.FileName);
                var fn = Guid.NewGuid() + f.Extension;
                var mymeType = file.ContentType.ToString();
                memoryStream.Flush();
                await _sftpClient.UploadAsMemoryStreamAsync(memoryStream, containerName + "/" + companyId, fn);
                res.Add(await _sftpClient.GetFileUrl(containerName + "/" + companyId + "/" + fn));
            }
            return res;
        }

        [HttpDelete("File")]
        [SwaggerOperation(Description = "Remove file from sftp. Take containerName in params and filename. Or remove from media container. Folder determined by company id in token")]
        public async Task<IActionResult> FileDelete([FromQuery] string containerName = null, [FromQuery] string fileName = null)
        {
            string companyId = GetCompanyIdFromToken();
            if (companyId == null) return null; 
            var container = containerName?? _containerName;
            await _sftpClient.DeleteFileIfExistsAsync(container + "/" + companyId + "/" + fileName);
            return Ok("OK");
        }
        #endregion

        private string GetCompanyIdFromToken()
        {
            try
            {
            if (!Request.Headers.TryGetValue("Authorization", out StringValues authToken)) return null;
                string token = authToken.First();
                var claims = _loginService.GetDataFromToken(token);
                return claims["companyId"];
            }
            catch
            {
                return null;
            }
        }
    }
}