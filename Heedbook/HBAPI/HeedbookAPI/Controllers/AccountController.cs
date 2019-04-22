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
    public class AccountController : Controller
    {
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;     
        private Dictionary<string, string> userClaims; 

        public AccountController(
            ILoginService loginService,
            RecordsContext context
            )
        {
            _loginService = loginService;
            _context = context;
        }

        [HttpPost("Register")]
        [SwaggerOperation(Description = "Create new company, new user, add manager role, create ew Tariff and newTransaction if no exist ")]
        public async Task<IActionResult> UserRegister([FromBody] UserRegister message, [FromHeader] string Authorization)
        {
            if (_context.Companys.Where(x => x.CompanyName == message.CompanyName).Any() || _context.ApplicationUsers.Where(x => x.NormalizedEmail == message.Email.ToUpper()).Any())
                return BadRequest("Company name or user email not unique");
            try
            { 
                var companyId = Guid.NewGuid();
                var company = new Company{
                    CompanyId = companyId,
                    CompanyIndustryId = message.CompanyIndustryId,
                    CompanyName = message.CompanyName,
                    LanguageId = message.LanguageId,
                    CreationDate = DateTime.UtcNow,
                    CountryId = message.CountryId,
                    StatusId = 5
                };
                await _context.Companys.AddAsync(company);

                var user = new ApplicationUser { 
                    UserName = message.Email,
                    NormalizedUserName = message.Email.ToUpper(),
                    Email = message.Email,
                    NormalizedEmail = message.Email.ToUpper(),
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    CreationDate = DateTime.UtcNow,
                    FullName = message.FullName,
                    PasswordHash =  _loginService.GeneratePasswordHash(message.Password),
                    StatusId = 3};
                await _context.AddAsync(user);

                var userRole = new ApplicationUserRole()
                    {
                        UserId = user.Id,
                        RoleId = _context.Roles.First(p => p.Name == "Manager").Id //Manager role
                    };
                await _context.ApplicationUserRoles.AddAsync(userRole);
                
                if (_context.Tariffs.Where(item => item.CompanyId == companyId).ToList().Count() == 0)
                {
                var tariff = new Tariff
                    {
                        TariffId = Guid.NewGuid(),
                        TotalRate = 0,
                        CompanyId = companyId,
                        CreationDate = DateTime.UtcNow,
                        CustomerKey = "",
                        EmployeeNo = 2,
                        ExpirationDate = DateTime.UtcNow.AddDays(5),
                        isMonthly = false,
                        Rebillid = "",
                        StatusId = 10
                    };

                var transaction = new Transaction
                    {
                        TransactionId = Guid.NewGuid(),
                        Amount = 0,
                        OrderId = "",
                        PaymentId = "",
                        TariffId = tariff.TariffId,
                        StatusId = 7,
                        PaymentDate = DateTime.UtcNow,
                        TransactionComment = "TRIAL TARIFF;FAKE TRANSACTION"
                    };
                        company.StatusId = 3;
                        
                        await _context.Tariffs.AddAsync(tariff);
                        await _context.Transactions.AddAsync(transaction);
                        var ids = _context.ApplicationUsers.Where(p => p.Id == user.Id).ToList();
                        await _context.SaveChangesAsync();
                        _context.Dispose();
                    }
                    else
                    {
                        
                    }
                return Ok("Registred");
            }
            catch (Exception e)
            {
                return BadRequest(e.ToString());
            }
        }

        [AllowAnonymous]
        [HttpPost("GenerateToken")]
        [SwaggerOperation(Description = "Loggin for user. Return jwt token")]
        public IActionResult GenerateToken([FromBody]AccountAuthorization message)
        {
            try
            {
                    if (message.UserName != null && message.Password != null && _loginService.CheckUserLogin(message.UserName, message.Password))
                    {
                        ApplicationUser user = _context.ApplicationUsers.Include(p => p.Company).Where(p => p.NormalizedEmail == message.UserName.ToUpper()).FirstOrDefault();
                        if (user == null)
                            return BadRequest("No such user");
                        return Ok( _loginService.CreateTokenForUser(user, message.Remember) );
                    }
                    else return BadRequest("Error in username or password");
            }
            catch (Exception e)
            {
                return BadRequest($"Could not create token {e}");
            }
        }
    }
}