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
        [SwaggerOperation(Summary = "Create user, company, trial tariff", 
            Description = "Create new active company, new active user, add manager role, create new trial Tariff on 5 days/2 employee and new finished Transaction if no exist")]
        public async Task<IActionResult> UserRegister([FromBody, 
                        SwaggerParameter("User and company data", Required = true)] 
                        UserRegister message)
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
                    StatusId = _context.Statuss.FirstOrDefault(p => p.StatusName == "Inactive").StatusId//---inactive
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
                    StatusId = _context.Statuss.FirstOrDefault(p => p.StatusName == "Active").StatusId//---active
                    };
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
                        StatusId = _context.Statuss.FirstOrDefault(p => p.StatusName == "Trial").StatusId//---Trial
                    };

                var transaction = new Transaction
                    {
                        TransactionId = Guid.NewGuid(),
                        Amount = 0,
                        OrderId = "",
                        PaymentId = "",
                        TariffId = tariff.TariffId,
                        StatusId = _context.Statuss.FirstOrDefault(p => p.StatusName == "Finished").StatusId,//---finished
                        PaymentDate = DateTime.UtcNow,
                        TransactionComment = "TRIAL TARIFF;FAKE TRANSACTION"
                    };
                        company.StatusId = _context.Statuss.FirstOrDefault(p => p.StatusName == "Active").StatusId;//---Active
                        
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
        [SwaggerOperation(Summary = "Loggin user", Description = "Loggin for user. Return jwt token")]
        [SwaggerResponse(400, "The user data is invalid", typeof(string))]
        [SwaggerResponse(200, "JWT token")]
        public IActionResult GenerateToken([FromBody, 
                        SwaggerParameter("User data", Required = true)]
                        AccountAuthorization message)
        {
            try
            {
                    ApplicationUser user = _context.ApplicationUsers.Include(p => p.Company).Where(p => p.NormalizedEmail == message.UserName.ToUpper()).FirstOrDefault();
                    if (user == null) return BadRequest("No such user");

                    if (message.UserName != null && message.Password != null && _loginService.CheckUserLogin(message.UserName, message.Password))
                    {
                        if (user.StatusId != _context.Statuss.FirstOrDefault(x => x.StatusName == "Active").StatusId) return BadRequest("User not activated");
                        return Ok( _loginService.CreateTokenForUser(user, message.Remember) );
                    }
                    else return StatusCode((int)System.Net.HttpStatusCode.Unauthorized, "Error in username or password");
            }
            catch (Exception e)
            {
                return BadRequest($"Could not create token {e}");
            }
        }
   
        [HttpPost("ChangePassword")]
        [SwaggerOperation(Summary = "two cases", Description = "Change password for user. Receive email. Receive new password for loggined user(with token) or send new password on email")]
        public async Task<IActionResult> UserChangePasswordAsync(
                    [FromBody, SwaggerParameter("email required, password only with token")] AccountAuthorization message,  
                    [FromHeader,  SwaggerParameter("JWT token not required, if exist receive new password, if not - generate new password", Required = false)] string Authorization)
        {
            try
            {
                ApplicationUser user = null;
                //---FOR LOGGINED USER CHANGE PASSWORD WITH INPUT (receive new password in body message.Password)
                if (_loginService.GetDataFromToken(Authorization, out userClaims))
                {
                    var userId = Guid.Parse(userClaims["applicationUserId"]);
                    user = _context.ApplicationUsers.FirstOrDefault(x => x.Id == userId && x.NormalizedEmail == message.UserName.ToUpper());
                    user.PasswordHash = _loginService.GeneratePasswordHash(message.Password);
                }
                //---IF USER NOT LOGGINED HE RECEIVE GENERATED PASSWORD ON EMAIL
                else
                {
                    user = _context.ApplicationUsers.FirstOrDefault(x => x.NormalizedEmail == message.UserName.ToUpper());
                    if ( user == null )
                        return BadRequest("No such user");
                    string password = _loginService. GeneratePass(6);               
                    string msg = _loginService.GenerateEmailMsg(password, user);
                    _loginService.SendEmail(user.Email, "Password changed", msg);
                    user.PasswordHash = _loginService.GeneratePasswordHash(password);
                }
                await _context.SaveChangesAsync();
                return Ok("password changed");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}