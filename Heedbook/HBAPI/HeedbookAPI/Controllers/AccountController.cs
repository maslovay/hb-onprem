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
using HBLib;
using HBLib.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : Controller
    {
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly ElasticClient _log;
        private Dictionary<string, string> userClaims;
        private readonly SmtpSettings _smtpSettings;
        private readonly SmtpClient _smtpClient;

        public AccountController(
            ILoginService loginService,
            RecordsContext context,
            ElasticClient log,            
            SmtpSettings smtpSettings,
            SmtpClient smtpClient
            )
        {
            _loginService = loginService;
            _context = context;
            _log = log;
            _smtpSettings = smtpSettings;  
            _smtpClient = smtpClient;
        }

        [HttpPost("Register")]
        [SwaggerOperation(Summary = "Create user, company, trial tariff",
            Description = "Create new active company, new active user, add manager role, create new trial Tariff on 5 days/2 employee and new finished Transaction if no exist")]
        public async Task<IActionResult> UserRegister([FromBody,
                        SwaggerParameter("User and company data", Required = true)]
                        UserRegister message)
        {
            _log.Info("Account/Register started");
            if (_context.Companys.Where(x => x.CompanyName == message.CompanyName).Any() || _context.ApplicationUsers.Where(x => x.NormalizedEmail == message.Email.ToUpper()).Any())
                return BadRequest("Company name or user email not unique");
            try
            {
                var companyId = Guid.NewGuid();
                var company = new Company
                {
                    CompanyId = companyId,
                    CompanyIndustryId = message.CompanyIndustryId,
                    CompanyName = message.CompanyName,
                    LanguageId = message.LanguageId,
                    CreationDate = DateTime.UtcNow,
                    CountryId = message.CountryId,
                    StatusId = _context.Statuss.FirstOrDefault(p => p.StatusName == "Inactive").StatusId//---inactive
                };
                await _context.Companys.AddAsync(company);
                _log.Info("Company created");

                var user = new ApplicationUser
                {
                    UserName = message.Email,
                    NormalizedUserName = message.Email.ToUpper(),
                    Email = message.Email,
                    NormalizedEmail = message.Email.ToUpper(),
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    CreationDate = DateTime.UtcNow,
                    FullName = message.FullName,
                    PasswordHash = _loginService.GeneratePasswordHash(message.Password),
                    StatusId = _context.Statuss.FirstOrDefault(p => p.StatusName == "Active").StatusId//---active
                };
                await _context.AddAsync(user);
                _loginService.SavePasswordHistory(user.Id, user.PasswordHash);
                _log.Info("User created");

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
                    _log.Info("Tariff created");
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
                    _log.Info("Transaction created");
                    await _context.Tariffs.AddAsync(tariff);
                    await _context.Transactions.AddAsync(transaction);
                    var ids = _context.ApplicationUsers.Where(p => p.Id == user.Id).ToList();
                    await _context.SaveChangesAsync();
                    _context.Dispose();
                    AccountCreatedMailSend(message);
                    _log.Info("All saved in DB");
                }
                else
                {

                }
                _log.Info("Account/register finished");
                return Ok("Registred");
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("GenerateToken")]
        [SwaggerOperation(Summary = "Loggin user", Description = "Loggin for user. Return jwt token. Save errors passwords history (Block user)")]
        [SwaggerResponse(400, "The user data is invalid", typeof(string))]
        [SwaggerResponse(200, "JWT token")]
        public IActionResult GenerateToken([FromBody,
                        SwaggerParameter("User data", Required = true)]
                        AccountAuthorization message)
        {
            try
            {
                _log.Info("Account/generate token started");
                ApplicationUser user = _context.ApplicationUsers.Include(p => p.Company).Where(p => p.NormalizedEmail == message.UserName.ToUpper()).FirstOrDefault();
                //---wrong email?
                if (user == null) return BadRequest("No such user");
                //---blocked?
                if (user.StatusId != _context.Statuss.FirstOrDefault(x => x.StatusName == "Active").StatusId) return BadRequest("User not activated");
                //---success?
                if (_loginService.CheckUserLogin(message.UserName, message.Password))
                {
                    _loginService.SaveErrorLoginHistory(user.Id, "success");
                    return Ok(_loginService.CreateTokenForUser(user, message.Remember));
                }
                //---failed?
                else
                {
                    if (_loginService.SaveErrorLoginHistory(user.Id, "error"))//---save failed attempt to log in and check amount of attempts (<3)
                        return StatusCode((int)System.Net.HttpStatusCode.Unauthorized, "Error in username or password");
                    else//---block user if this is the 3-rd failed attempt to log in
                    {
                        user.StatusId = _context.Statuss.FirstOrDefault(x => x.StatusName == "Inactive").StatusId;
                        _context.SaveChanges();
                        return StatusCode((int)System.Net.HttpStatusCode.Unauthorized, "Blocked");
                    }
                }
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occurred {e}");
                return BadRequest($"Could not create token {e}");
            }
        }

        [HttpPost("ChangePassword")]
        [SwaggerOperation(Summary = "two cases", Description = "Change password for user. Receive email. Receive new password for loggined user(with token) or send new password on email")]
        public async Task<IActionResult> UserChangePasswordAsync(
                    [FromBody, SwaggerParameter("email required, password only with token")] AccountAuthorization message,
                    [FromHeader, SwaggerParameter("JWT token not required, if exist receive new password, if not - generate new password", Required = false)] string Authorization)
        {
            try
            {
                _log.Info("Account/Change password started");
                ApplicationUser user = null;
                //---FOR LOGGINED USER CHANGE PASSWORD WITH INPUT (receive new password in body message.Password)
                if (_loginService.GetDataFromToken(Authorization, out userClaims))
                {
                    var userId = Guid.Parse(userClaims["applicationUserId"]);
                    user = _context.ApplicationUsers.FirstOrDefault(x => x.Id == userId && x.NormalizedEmail == message.UserName.ToUpper());
                    user.PasswordHash = _loginService.GeneratePasswordHash(message.Password);
                    if (!_loginService.SavePasswordHistory(user.Id, user.PasswordHash))//---check 5 last passwords
                        return BadRequest("password was used");
                }
                //---IF USER NOT LOGGINED HE RECEIVE GENERATED PASSWORD ON EMAIL
                else
                {
                    user = _context.ApplicationUsers.FirstOrDefault(x => x.NormalizedEmail == message.UserName.ToUpper());
                    if (user == null)
                        return BadRequest("No such user");
                    string password = _loginService.GeneratePass(6);
                    string msg = _loginService.GenerateEmailMsg(password, user);
                    _loginService.SendEmail(user.Email, "Password changed", msg);
                    user.PasswordHash = _loginService.GeneratePasswordHash(password);
                }
                await _context.SaveChangesAsync();
                _log.Info("Account/ change password finished");
                return Ok("password changed");
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }

        [HttpPost("Unblock")]
        [SwaggerOperation(Summary = "Unblock in case failed attempts to log in", Description = "Unblock, zero counter of failed log in, hange password for user. Send email with new password")]
        public async Task<IActionResult> Unblock(
                    [FromBody, SwaggerParameter("email required")] string email,
                    [FromHeader, SwaggerParameter("JWT token required ( token of admin or manager )", Required = true)] string Authorization)
        {
            try
            {
                _log.Info("Account/unblock started");
                ApplicationUser user = _context.ApplicationUsers.FirstOrDefault(x => x.NormalizedEmail == email.ToUpper());
                if (_loginService.GetDataFromToken(Authorization, out userClaims))
                {
                    string password = _loginService.GeneratePass(6);
                    string msg = _loginService.GenerateEmailMsg(password, user);
                    _loginService.SendEmail(user.Email, "Password changed", msg);
                    user.PasswordHash = _loginService.GeneratePasswordHash(password);
                    user.StatusId = _context.Statuss.FirstOrDefault(x => x.StatusName == "Active").StatusId;
                    _loginService.SaveErrorLoginHistory(user.Id, "success");
                }
                await _context.SaveChangesAsync();
                _log.Info("Account/unblock finished");
                return Ok("password changed");
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }

        private void AccountCreatedMailSend(UserRegister message)
        {
            var mail = new System.Net.Mail.MailMessage();
            mail.From = new System.Net.Mail.MailAddress(_smtpSettings.FromEmail);
            mail.To.Add(new System.Net.Mail.MailAddress(message.Email)); 
            
            mail.Subject = "Heedbook registration completed successfully";

            mail.Body = "Здравствуйте.\n" +
                        "Вы зарегистрированы как Сотрудник в личном кабинете системы Heedbook\n" +
                        "Для использования системы введите на сайте https://app.heedbook.com/login следующие данные:\n" +
                        $"Login: {message.Email}\n" +
                        $"Password: {message.Password}\n";
            mail.BodyEncoding = System.Text.Encoding.UTF8;
            mail.IsBodyHtml = false;

            try
            {
                _smtpClient.Send(mail);
                _log.Info($"Registration successfully mail Sended to {message.Email}");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                _log.Fatal($"Failed Registration successfully mail to {message.Email}\n{ex.Message}\n");
            }
        }
    }
}