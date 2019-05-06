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
using FileRef.Models;
using Newtonsoft.Json;
using HBData;
using HBData.Models;
using HBLib;
using HBLib.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.Extensions.DependencyInjection;

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
        //https://localhost:5001/FileRef/GetFile?path=/test/2.png&exp=2019-04-25T18:10:29&token=3bf12646e0941dec6d81c3b35470425e
        [HttpGet("GetFile")]
        public async Task<IActionResult> GetFile([FromQuery(Name = "path")] string path,
                                    [FromQuery(Name = "exp")] DateTime exp,
                                    [FromQuery(Name = "token")] string token)
        {
            string hash = Methods.MakeExpiryHash(exp);
            if (String.IsNullOrEmpty(token))
                return BadRequest();
            if (token != hash) 
                return BadRequest();
            if (exp.ToUniversalTime() < DateTime.UtcNow)
                return BadRequest();
            if (!await _client.IsFileExistsAsync(path)) 
                return BadRequest();
            try
            {
                using (var ms = await _client.DownloadFromFtpAsMemoryStreamAsync(path))
                {
                    if (ms.Length == 0)
                        return BadRequest();
                    ms.Position = 0;
                    var fileName = path.Split("/").LastOrDefault();
                    var contentType = "application/octet-stream";
                    return File(ms, contentType, fileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return BadRequest();
            }
        }

        [HttpGet("GetReference")]
        public IActionResult GetReference()
        {
            //https://localhost:5001/FileRef/GetFile?path=/test/2.png&exp=2019-04-26T16:15:02&token=c43b9dfd2ce23fd58cff5dacca50ccad

            List<string> references = new List<string>();
            DateTime expires = DateTime.Now + TimeSpan.FromMinutes(10);
            string hash = Methods.MakeExpiryHash(expires);

            string path = "/test/2.png";

            string link = string.Format("https://localhost:5001/FileRef/GetFile?path={0}&exp={1}&token={2}", path, expires.ToString("s"), hash);
            references.Add(link);
            return Ok(JsonConvert.SerializeObject(references));
        }        
    }
}