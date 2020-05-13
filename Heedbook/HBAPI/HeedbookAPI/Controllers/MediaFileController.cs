using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using UserOperations.Services;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;
using UserOperations.Utils;
using HBLib.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]
    public class MediaFileController : Controller
    {        
        private readonly MediaFileService _mediaFileService;
      

        public MediaFileController(
            MediaFileService mediaFileService
            )
        {
            _mediaFileService = mediaFileService;
        }

        [HttpGet("File")]
        [SwaggerOperation(Description = "Return all files from sftp. If no parameters are passed return files from 'media', for loggined company")]
        [SwaggerResponse(400, "No such file / Exception message", typeof(string))]
        [SwaggerResponse(200, "File exist")]
        public async Task<object> FileGet(
                [FromQuery(Name= "containerName")] string containerName = null, 
                [FromQuery(Name = "fileName")] string fileName = null,
                [FromQuery(Name = "expirationDate")]  DateTime? expirationDate = null) =>
            await _mediaFileService.FileGet(
                containerName,
                fileName,
                expirationDate);
        
        [HttpPost("File")]
        [SwaggerOperation(Description = "Save file on sftp. Can take containerName in body or save to media container. Folder determined by company id in token")]
        [SwaggerResponse(400, "Filed to upload file / Exception message", typeof(string))]
        [SwaggerResponse(200, "File uploaded")]
        public async Task<object> FilePost([FromForm] IFormCollection formData) =>
            await _mediaFileService.FilePost(formData);
        
        
        [HttpPut("File")]
        [SwaggerOperation(Description = "Remove old and save new file on sftp. Can take containerName in body or save to media container. Folder determined by company id in token")]
        [SwaggerResponse(400, "Filed to edit file / Exception message", typeof(string))]
        [SwaggerResponse(200, "File changed")]
        public async Task<object> FilePut([FromForm] IFormCollection formData) =>
            await _mediaFileService.FilePut(formData);
        
        [HttpDelete("File")]
        [SwaggerOperation(Description = "Remove file from sftp. Take containerName in params and filename. Or remove from media container. Folder determined by company id in token")]
        [SwaggerResponse(400, "Filed to delete file / Exception message", typeof(string))]
        [SwaggerResponse(200, "File deleted")]
        public async Task<object> FileDelete(
                [FromQuery] string containerName = null, 
                [FromQuery] string fileName = null) =>
            await _mediaFileService.FileDelete(containerName, fileName);
    }
}