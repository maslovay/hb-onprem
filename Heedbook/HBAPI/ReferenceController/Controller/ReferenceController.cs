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
                                    [FromQuery(Name = "expirationDate")] DateTime expirationDate,
                                    [FromQuery(Name = "token")] string token)
        {
            var hash = Methods.MakeExpiryHash(expirationDate);
            if (string.IsNullOrEmpty(token))
                return BadRequest("Token is empty");
            if (token != hash) 
                return BadRequest("Token is not valid");
            if (expirationDate.ToUniversalTime() < DateTime.UtcNow)
                return BadRequest("Time is over");
            if (!await _client.IsFileExistsAsync(path)) 
                return BadRequest();
            try
            {
                var ms = await _client.DownloadFromFtpAsMemoryStreamAsync(path);
                
                if (ms.Length == 0)
                    return BadRequest();
                ms.Position = 0;
                var fileName = path.Split("/").LastOrDefault();
                return File(ms, "application/octet-stream", fileName);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return BadRequest(ex.ToString());
            }
        }
        [HttpGet("GetNewReference")]
        public IActionResult GetNewReference([FromQuery(Name = "containerName")] string containerName,
                                        [FromQuery(Name = "fileName")] string fileName,
                                        [FromQuery(Name = "expirationDate")] DateTime expirationDate)
        {           

            FileReference fileref = new FileReference(new SftpSettings()
            {
                Host = _conf.GetSection("SftpSettings")["Host"],
                Port = int.Parse(_conf.GetSection("SftpSettings")["Port"]),
                UserName = _conf.GetSection("SftpSettings")["UserName"],
                Password = _conf.GetSection("SftpSettings")["Password"],
                DestinationPath = _conf.GetSection("SftpSettings")["DestinationPath"],
                DownloadPath = _conf.GetSection("SftpSettings")["DownloadPath"]
            });

            var link = fileref.GetReference(containerName, fileName, expirationDate);
                        
            return Ok(JsonConvert.SerializeObject(link));
        }
    }
}