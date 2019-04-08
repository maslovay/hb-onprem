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
using UserOperations.Models.AnalyticModels;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using HBData;
using HBLib.Utils;
using UserOperations.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DemonstrationController : Controller
    {
        private readonly RecordsContext _context;
        private readonly IConfiguration _config;
        private readonly DBOperations _dbOperation;

        public DemonstrationController(
            RecordsContext context,
            IConfiguration config,
            DBOperations dbOperation
            )
        {
            _context = context;
            _config = config;
            _dbOperation = dbOperation;
        }

        [HttpPost("AnalyzeFrames")]
        public IActionResult AnalyzeFrames([FromQuery(Name = "applicationUserId")] Guid applicationUserId, 
                                            [FromBody] string fileString)
        {
            try
            {
                var imgBytes = Convert.FromBase64String(fileString);
                var memoryStream = new MemoryStream(imgBytes);
                if (FaceDetection.IsFaceDetected(localPath, out var faceLength))
                {
                    Console.WriteLine("face detected!");
                }
                
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
    }
}