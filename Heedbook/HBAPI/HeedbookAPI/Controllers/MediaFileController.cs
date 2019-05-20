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
    public class MediaFileController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly SftpClient _sftpClient;
        private readonly string _containerName;
        private Dictionary<string, string> userClaims;

        public MediaFileController(
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
        public async Task<IActionResult> FileGet([FromHeader]string Authorization,
                                                        [FromQuery]string containerName = null, 
                                                        [FromQuery]string fileName = null)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                        return BadRequest("Token wrong");
                var companyId = userClaims["companyId"];
                containerName = containerName ?? _containerName;
                if (fileName != null)
                {
                       var data = new 
                        {
                        // expirationDate = null,
                            containerName = _containerName+"/test",
                            //"+companyId,
                            fileName
                        };
                    return RedirectToAction("GetReference", "FileRef", data);
                    var result = new { path = await _sftpClient.GetFileUrl($"{containerName}/{companyId}/{fileName})"), ext = Path.GetExtension(fileName)};
                    return Ok(result);
                }
                else
                {
                      var data = new 
                        {
                        // expirationDate = null,
                            containerName = _containerName+"/"+companyId,
                            fileName
                        };
                    return RedirectToAction("GetReference", "FileRef", data);
                   // var result = await _sftpClient.GetAllFilesUrl(containerName, new []{ companyId.ToString()});
                   // return Ok(result.Select(x => new {path = x, ext = Path.GetExtension(x).Trim('.')}));
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("File")]
        [SwaggerOperation(Description = "Save file on sftp. Can take containerName in body or save to media container. Folder determined by company id in token")]
        public async Task<IActionResult> FilePost([FromHeader] string Authorization, [FromForm] IFormCollection formData)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                        return BadRequest("Token wrong");
                var companyId = userClaims["companyId"];
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
                return Ok(result.Select(x => new {path = x, ext = Path.GetExtension(x).Trim('.')}));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        
        [HttpPut("File")]
        [SwaggerOperation(Description = "Remove old and save new file on sftp. Can take containerName in body or save to media container. Folder determined by company id in token")]
        public async Task<IActionResult> FilePut([FromHeader] string Authorization, [FromForm] IFormCollection formData)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                        return BadRequest("Token wrong");
                var companyId = userClaims["companyId"];
                var containerNameParam = formData.FirstOrDefault(x => x.Key == "containerName");
                var containerName = containerNameParam.Value.Any() ? containerNameParam.Value.ToString() : _containerName;
                var fileName = formData.FirstOrDefault(x => x.Key == "fileName").Value.ToString();

                await Task.Run(() => _sftpClient.DeleteFileIfExistsAsync($"{containerName}/{companyId}/{fileName}"));

                FileInfo fileInfo = new FileInfo(formData.Files[0].FileName);
                var fn = Guid.NewGuid() + fileInfo.Extension;
                var memoryStream = formData.Files[0].OpenReadStream();
                await _sftpClient.UploadAsMemoryStreamAsync(memoryStream, $"{containerName}/{companyId}", fn, true);
                var result = new { 
                    path =  await _sftpClient.GetFileUrl($"{containerName}/{companyId}/{fn}"), 
                    ext = Path.GetExtension(fileName.Trim('.'))};
                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("File")]
        [SwaggerOperation(Description = "Remove file from sftp. Take containerName in params and filename. Or remove from media container. Folder determined by company id in token")]
        public async Task<IActionResult> FileDelete([FromHeader] string Authorization, 
                                                    [FromQuery] string containerName = null, 
                                                    [FromQuery] string fileName = null)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                        return BadRequest("Token wrong");
                var companyId = userClaims["companyId"];
                var container = containerName?? _containerName;
                await Task.Run(() => _sftpClient.DeleteFileIfExistsAsync($"{container}/{companyId}/{fileName}"));
                return Ok("OK");
            }
            catch (Exception e)
            {
                 return BadRequest(e.Message);
            }

        }
        #endregion
    }
}