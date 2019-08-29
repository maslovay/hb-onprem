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
using HBData.Models;
using HBData.Models.AccountViewModels;
using UserOperations.Services;
using UserOperations.AccountModels;
using HBData;

///REMOVE IT
using System.Data.SqlClient;


using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using HBLib.Utils;
using UserOperations.Utils;
using Npgsql;
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : Controller
    {
        private readonly RecordsContext _context;

        public TestController ( RecordsContext context )
        {
            _context = context;
        }     

        [HttpGet("TestToList")]
        public IActionResult TestToList( [FromQuery]Guid id)
        {
            var session = _context.Sessions
                       .Where(p => p.ApplicationUserId == id)
                       .ToList()?.OrderByDescending(p => p.BegTime);
            return Ok(session);
        }

        [HttpGet("TestWithoutToList")]
        public IActionResult TestWithoutToList([FromQuery]string id)
        {
            var session = _context.Sessions
                       .Where(p => p.ApplicationUserId.ToString() == id)?
                       .OrderByDescending(p => p.BegTime).ToList();
            return Ok(session);
        }

    }
}