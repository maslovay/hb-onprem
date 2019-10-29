using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using HBData;
using HBData.Models;
using HBData.Models.AccountViewModels;
using UserOperations.Services;
using UserOperations.AccountModels;
using System.Transactions;
using UserOperations.Providers;
using Newtonsoft.Json;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : Controller
    {
        private readonly ILoginService _loginService;
//        private readonly ElasticClient _log;
        private Dictionary<string, string> userClaims;
        private readonly MailSender _mailSender;
        private readonly AccountProvider _accountProvider;

        public AccountController(
            ILoginService loginService,
//            ElasticClient log,      
            MailSender mailSender,
            AccountProvider accountProvider
            )
        {
            _loginService = loginService;
//            _log = log;
            _mailSender = mailSender;
            _accountProvider = accountProvider;
        }

        [HttpPost("Register")]
        [SwaggerOperation(Summary = "Create user, company, trial tariff",
            Description = "Create new active company, new active user, add manager role, create new trial Tariff on 5 days/2 employee and new finished Transaction if no exist")]
        public async Task<IActionResult> UserRegister([FromBody,
                        SwaggerParameter("User and company data", Required = true)]
                        UserRegister message)
        {
//            _log.Info("Account/Register started");
            
            var statusActiveId = _accountProvider.GetStatusId("Active");
            if (await _accountProvider.CompanyExist(message.CompanyName) || await _accountProvider.EmailExist(message.Email))
                return BadRequest("Company name or user email not unique");
           // using (var transactionScope = new TransactionScope())
            {
                try
                {
                    //---1---company---
                    var companyId = Guid.NewGuid();                
                    var company = await _accountProvider.AddNewCompanysInBase(message, companyId);
//                    _log.Info("Company created");
                    //---2---user---
                    var user = await _accountProvider.AddNewUserInBase(message, companyId);
                    //                    _log.Info("User created");
                    //---3--user role---
                    await _accountProvider.AddUserRoleInBase(message, user);

                    if (_accountProvider.GetTariffs(companyId) == 0)
                    {
                        //---4---tariff and transaction---
                        await _accountProvider.CreateCompanyTariffAndtransaction(company);

                        //---6---ADD WORKER TYPES CATALOGUE CONNECTED TO NEW COMPANY
                        await _accountProvider.AddWorkerType(company);

                        //---7---content and campaign clone
                        await _accountProvider.AddContentAndCampaign(company);

                        _accountProvider.SaveChangesAsync();
                     //   transactionScope.Complete();

                        //_context.Dispose();
//                        _log.Info("All saved in DB");
                    }
                    try
                    {                       
                        await _mailSender.SendRegisterEmail(user);
                    }
                    catch { }
//                    _log.Info("Account/register finished");
                    return Ok("Registred");
                }
                catch (Exception e)
                {
//                    _log.Fatal($"Exception occurred {e}");
                    return BadRequest(e.Message);
                }
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
//                _log.Info("Account/generate token started");
                var user = _accountProvider.GetApplicationUser(message.UserName);
                if (user is null) return BadRequest("No such user");
                //---blocked?
                if (user.StatusId != _accountProvider.GetStatusId("Active")) return BadRequest("User not activated");
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
                        user.StatusId = _accountProvider.GetStatusId("Inactive");
                        _accountProvider.SaveChangesAsync();
                        return StatusCode((int)System.Net.HttpStatusCode.Unauthorized, "Blocked");
                    }
                }
            }
            catch (Exception e)
            {
//                _log.Fatal($"Exception occurred {e}");
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
//                _log.Info("Account/Change password started");
                ApplicationUser user = null;
                //---FOR LOGGINED USER CHANGE PASSWORD WITH INPUT (receive new password in body message.Password)
                if (_loginService.GetDataFromToken(Authorization, out userClaims))
                {
                    var userId = Guid.Parse(userClaims["applicationUserId"]);
                    user = _accountProvider.GetApplicationUser(userId, message);
                    user.PasswordHash = _loginService.GeneratePasswordHash(message.Password);
                    if (!_loginService.SavePasswordHistory(user.Id, user.PasswordHash))//---check 5 last passwords
                        return BadRequest("password was used");
                }
                //---IF USER NOT LOGGINED HE RECEIVE GENERATED PASSWORD ON EMAIL
                else
                {
                    user = _accountProvider.GetApplicationUser(message.UserName);
                    if (user == null)
                        return BadRequest("No such user");
                    string password = _loginService.GeneratePass(6);
                    await _mailSender.SendPasswordChangeEmail(user, password);
                    user.PasswordHash = _loginService.GeneratePasswordHash(password);
                }
                
                _accountProvider.SaveChanges();

                return Ok("password changed");
            }
            catch (Exception e)
            {
//                _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }

        [HttpPost("ChangePasswordOnDefault")]
        [SwaggerOperation(Summary = "For own use", Description = "Change password for user on Test_User12345")]
        public async Task<IActionResult> UserChangePasswordOnDefaultAsync( [FromBody] string email )
        {
            try
            {             
                var user = _accountProvider.GetApplicationUser(email);
                if (user == null) return BadRequest("No such user");
                user.PasswordHash = _loginService.GeneratePasswordHash("Test_User12345");                  
                _accountProvider.SaveChanges();
                return Ok("password changed");
            }
            catch (Exception e)
            {
//                _log.Fatal($"Exception occurred {e}");
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
//                _log.Info("Account/unblock started");
                ApplicationUser user = _accountProvider.GetApplicationUser(email);
                if (_loginService.GetDataFromToken(Authorization, out userClaims))
                {
                    string password = _loginService.GeneratePass(6);
                    string text = string.Format("<table>" +
                     "<tr><td>login:</td><td> {0}</td></tr>" +
                     "<tr><td>password:</td><td> {1}</td></tr>" +
                     "</table>", user.Email, password);
                    _mailSender.SendPasswordChangeEmail(user, text);
                    user.PasswordHash = _loginService.GeneratePasswordHash(password);
                    user.StatusId = _accountProvider.GetStatusId("Active");
                    _loginService.SaveErrorLoginHistory(user.Id, "success");
                }
                _accountProvider.SaveChanges();
//                _log.Info("Account/unblock finished");
                return Ok("password changed");
            }
            catch (Exception e)
            {
//                _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("Remove")]
        [SwaggerOperation(Summary = "Delete user, company, trial tariff - only for developers")]
        public async Task<IActionResult> AccountDelete([FromQuery,
                        SwaggerParameter("user email", Required = true)]
                        string email)
        {
            using (var transactionScope = new
                        TransactionScope(TransactionScopeOption.Suppress, new TransactionOptions()
                        {
                            IsolationLevel = IsolationLevel.Serializable
                        }))
            {
                try
                {
                    _accountProvider.RemoveAccount(email);
                    transactionScope.Complete();

//                    _log.Info("Account/remove finished");
                    return Ok("Removed");
                }
                catch (Exception e)
                {
//                    _log.Fatal($"Exception occurred {e}");
                    return BadRequest(e.Message);
                }
            }
        }

    }
}