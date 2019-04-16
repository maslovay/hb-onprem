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


namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly ITokenService _tokenService;
        private readonly RecordsContext _context;
        private readonly SftpClient _sftpClient;


        public FileController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration config,
            ITokenService tokenService,
            RecordsContext context,
            SftpClient sftpClient
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _tokenService = tokenService;
            _context = context;
            _sftpClient = sftpClient;
        }
        #region File
        [HttpGet("File")]
        public async Task<IEnumerable<string>> FileGet([FromQuery]string containerName = null, [FromQuery]string[] directoryNames = null, [FromQuery]string fileName = null)
        {
            IEnumerable <string> files = null;
            if( fileName != null )
            {
                files =  new List<string> { await _sftpClient.GetFileUrl(containerName + "/" + fileName)};
                return files;
            }
            if (containerName != null)
                files = await _sftpClient.GetAllFilesUrl(containerName, directoryNames);
            return files;
        }

        [HttpPost("File")]
        public string FilePost([FromBody] string file)
        {
          
            return "";
        }
        [HttpPut("File")]
        public string FilePut([FromBody] string file)
        {
        
            return "";
        }
        [HttpDelete("File")]
        public IActionResult FileDelete([FromQuery] Guid campaignId)
        {
         
                return Ok("OK");            
                //return BadRequest("No such campaign");
        }
        #endregion
}
}