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
        private readonly ElasticClient _log;
        private readonly string _containerName;
        private Dictionary<string, string> userClaims;
      

        public MediaFileController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            SftpClient sftpClient,
            ElasticClient log  
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _sftpClient = sftpClient;
            _log = log;
            _containerName = "media";         
        }

        [HttpGet("File")]
        [SwaggerOperation(Description = "Return all files from sftp. If no parameters are passed return files from 'media', for loggined company")]
        public async Task<IActionResult> FileGet([FromHeader]string Authorization,
                                                        [FromQuery(Name= "containerName")] string containerName = null, 
                                                        [FromQuery(Name = "fileName")] string fileName = null,
                                                        [FromQuery(Name = "expirationDate")]  DateTime? expirationDate = null)
        {
            try
            {
                _log.Info("MediaFile/File GET started");    
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                        return BadRequest("Token wrong");
                var companyId = userClaims["companyId"];
                containerName = containerName ?? _containerName; 
                if (expirationDate == null) expirationDate = default(DateTime);     

                if (fileName != null)
                {
                    var result = _sftpClient.GetFileLink(containerName + "/" + companyId, fileName, (DateTime)expirationDate);
                    _log.Info("MediaFile/File finished"); 
                    return Ok(JsonConvert.SerializeObject(result));
                }
                else
                {
                    var files = await _sftpClient.GetFileNames(_containerName+"/"+companyId);  
                    List<object> result = new List<object>();        
                    foreach(var file in files)         
                    {
                        result.Add( _sftpClient.GetFileLink(containerName + "/" + companyId, file, (DateTime)expirationDate));
                    }
                    _log.Info("MediaFile/File GET finished"); 
                    return Ok(JsonConvert.SerializeObject(result));
                }
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }

      
        [HttpPost("File")]
        [SwaggerOperation(Description = "Save file on sftp. Can take containerName in body or save to media container. Folder determined by company id in token")]
        public async Task<IActionResult> FilePost([FromHeader] string Authorization, [FromForm] IFormCollection formData)
        {
            try
            {
                _log.Info("MediaFile/File POST started"); 
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
                    fileNames.Add(fn);
                    //memoryStream.Close();
                }
                await Task.WhenAll(tasks);

                List<object> result = new List<object>();   
                foreach (var file in fileNames)
                {
                    result.Add( _sftpClient.GetFileLink(containerName + "/" + companyId, file, default(DateTime)));
                }
                _log.Info("MediaFile/File POST finished"); 
                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }
        
        [HttpPut("File")]
        [SwaggerOperation(Description = "Remove old and save new file on sftp. Can take containerName in body or save to media container. Folder determined by company id in token")]
        public async Task<IActionResult> FilePut([FromHeader] string Authorization, [FromForm] IFormCollection formData)
        {
            try
            {
                _log.Info("MediaFile/File PUT started"); 
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
                // var result = new { 
                //     path =  await _sftpClient.GetFileUrl($"{containerName}/{companyId}/{fn}"), 
                //     ext = Path.GetExtension(fileName.Trim('.'))};
                var result = _sftpClient.GetFileLink(containerName + "/" + companyId, fn, default(DateTime));
                _log.Info("MediaFile/File PUT finished"); 
                return Ok(result);
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occurred {e}");
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
                _log.Info("MediaFile/File DELETE started"); 
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                        return BadRequest("Token wrong");
                var companyId = userClaims["companyId"];
                var container = containerName?? _containerName;
                await Task.Run(() => _sftpClient.DeleteFileIfExistsAsync($"{container}/{companyId}/{fileName}"));
                _log.Info("MediaFile/File DELETE finished"); 
                return Ok("OK");
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occurred {e}");
                 return BadRequest(e.Message);
            }
        }
    }
}