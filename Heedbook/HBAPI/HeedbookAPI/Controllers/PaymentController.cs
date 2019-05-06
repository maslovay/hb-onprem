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


using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : Controller
    {
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;   

        public PaymentController(
            ILoginService loginService,
            RecordsContext context
            )
        {
            _loginService = loginService;
            _context = context;
        }

        [HttpGet("Tariff")]
        [SwaggerOperation(Summary = "Return tariffs and Transaction", Description = "Return tariffs and Transaction")]
        public async Task<IActionResult> TariffGet( [FromHeader,  SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);
                var tariffs = _context.Tariffs.Include(x => x.Transactions).Where(x=>x.CompanyId == companyId).OrderByDescending(x => x.CreationDate).ToList();
                return Ok(tariffs);
            }
            catch (Exception e)
            {
                return BadRequest(e.ToString());
            }
        }
    }
}