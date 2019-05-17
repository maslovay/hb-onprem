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

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using HBData;
using HBData.Models;
using HBLib;
using HBLib.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.Extensions.DependencyInjection;
using ReferenceController.Models;

namespace ReferenceController
{
    [Route("[controller]")]
    [ApiController]
    public class FileRefController : Controller
    {
        private IConfiguration _conf;

        private readonly SftpClient _client;
        public FileRefController(IConfiguration conf,
            SftpClient client)
        {
            _conf = conf;
            _client = client;
        }
        
        [HttpGet("GetFile")]
        public async Task<IActionResult> GetFile([FromQuery(Name = "path")] string path,
                                    [FromQuery(Name = "exp")] DateTime exp,
                                    [FromQuery(Name = "token")] string token)
        {
            string hash = Methods.MakeExpiryHash(exp);
            if (String.IsNullOrEmpty(token))
                return Ok("Token is empty");
            if (token != hash) 
                return Ok("Token is not valid");
            if (exp.ToUniversalTime() < DateTime.UtcNow)
                return Ok("Time is over");
            if (!await _client.IsFileExistsAsync(path)) 
                return BadRequest();
            try
            {
                var ms = await _client.DownloadFromFtpAsMemoryStreamAsync(path);
                
                if (ms.Length == 0)
                    return BadRequest();
                ms.Position = 0;
                var fileName = path.Split("/").LastOrDefault();
                var contentType = "application/octet-stream";
                return File(ms, contentType, fileName);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return BadRequest(ex.ToString());
            }
        }

        [HttpGet("GetReference")]
        public IActionResult GetReference([FromQuery(Name = "containerName")] string containerName,
                                        [FromQuery(Name = "fileName")] string fileName,
                                        [FromQuery(Name = "expirationDate")] DateTime expirationDate)
        {
            if (String.IsNullOrEmpty(containerName))
                return Ok("containerName is empty");
            if (String.IsNullOrEmpty(fileName))
                return Ok("fileName is empty");
            if (expirationDate == default(DateTime))
                return Ok("expirationDate is empty");
                
            List<string> references = new List<string>();
            string hash = Methods.MakeExpiryHash(expirationDate);
            
            string link = string.Format($"http://filereference.northeurope.cloudapp.azure.com/FileRef/GetFile?path=/home/nkrokhmal/storage/{containerName}/{fileName}&expirationDate={expirationDate.ToString("s")}&token={hash}");
            
            references.Add(link);
            return Ok(JsonConvert.SerializeObject(references));
        }        
    }
}