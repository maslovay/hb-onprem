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
using UserOperations.Providers.Interfaces;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : Controller
    {
        private readonly LoginService _loginService;
        private readonly MailSender _mailSender;
        private readonly IAccountProvider _accountProvider;
        private readonly IHelpProvider _helpProvider;
        private Dictionary<string, string> userClaims;

        public AccountController(
            LoginService loginService,
            MailSender mailSender,
            IAccountProvider accountProvider,
            IHelpProvider helpProvider
            )
        {
            _loginService = loginService;
            _mailSender = mailSender;
            _accountProvider = accountProvider;
            _helpProvider = helpProvider;
        }

        [HttpPost("Register")]
        [SwaggerOperation(Summary = "Create user, company, trial tariff",
            Description = "Create new active company, new active user, add manager role, create new trial Tariff on 5 days/2 employee and new finished Transaction if no exist")]
        [SwaggerResponse(400, "Exception message")]
        [SwaggerResponse(200, "Registred")]
        public async Task<IActionResult> UserRegister([FromBody,
                        SwaggerParameter("User and company data", Required = true)]
                        UserRegister message)
        {
            var statusActiveId = _accountProvider.GetStatusId("Active");
            if (await _accountProvider.CompanyExist(message.CompanyName) || await _accountProvider.EmailExist(message.Email))
                return BadRequest("Company name or user email not unique");
           // using (var transactionScope = new TransactionScope())
            {
                try
                {
                    //---1---company---
                    var company = _accountProvider.AddNewCompanysInBase(message);
                    var user = await _accountProvider.AddNewUserInBase(message, company?.CompanyId);
                    await _accountProvider.AddUserRoleInBase(message, user);

                    if (await _accountProvider.GetTariffsAsync(company?.CompanyId) == 0)
                    {
                        await _accountProvider.CreateCompanyTariffAndTransaction(company);
                        await _accountProvider.AddWorkerType(company);
                        await _accountProvider.AddContentAndCampaign(company);
                    }
                    await _accountProvider.SaveChangesAsync();
                    try
                    {
                        await _mailSender.SendRegisterEmail(user);
                    }
                    catch { }
                    return Ok("Registred");
                }
                catch (Exception e)
                {
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
                var user = _accountProvider.GetUserIncludeCompany(message.UserName);
                if (user is null) return BadRequest("No such user");
                //---blocked?
                if (user.StatusId != _accountProvider.GetStatusId("Active")) return BadRequest("User not activated");
                //---success?
                if (_loginService.CheckUserLogin(message.UserName, message.Password))
                    return Ok(_loginService.CreateTokenForUser(user, message.Remember));
                //---failed?
                else
                    return StatusCode((int)System.Net.HttpStatusCode.Unauthorized, "Error in username or password");
            }
            catch (Exception e)
            {
                return BadRequest($"Could not create token {e}");
            }
        }

        [HttpPost("ChangePassword")]
        [SwaggerOperation(Summary = "two cases", Description = "Change password for user. Receive email. Receive new password for loggined user(with token) or send new password on email")]
        [SwaggerResponse(400, "No such user / Exception message", typeof(string))]
        [SwaggerResponse(200, "Password changed")]
        public async Task<IActionResult> UserChangePasswordAsync(
                    [FromBody, SwaggerParameter("email required, password only with token")] AccountAuthorization message,
                    [FromHeader, SwaggerParameter("JWT token not required, if exist receive new password, if not - generate new password", Required = false)] string Authorization)
        {
            try
            {
                ApplicationUser user = null;
                //---FOR LOGGINED USER CHANGE PASSWORD WITH INPUT (receive new password in body message.Password)
                if (_loginService.GetDataFromToken(Authorization, out userClaims))
                {
                    var userId = Guid.Parse(userClaims["applicationUserId"]);
                    user = _accountProvider.GetUserIncludeCompany(userId, message);
                    user.PasswordHash = _loginService.GeneratePasswordHash(message.Password);
                }
                //---IF USER NOT LOGGINED HE RECEIVE GENERATED PASSWORD ON EMAIL
                else
                {
                    user = _accountProvider.GetUserIncludeCompany(message.UserName);
                    if (user == null)
                        return BadRequest("No such user");
                    string password = _loginService.GeneratePass(6);
                    await _mailSender.SendPasswordChangeEmail(user, password);
                    user.PasswordHash = _loginService.GeneratePasswordHash(password);
                }                
                _accountProvider.SaveChanges();
                return Ok("Password changed");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("ChangePasswordOnDefault")]
        [SwaggerOperation(Summary = "For own use", Description = "Change password for user on Test_User12345")]
        [SwaggerResponse(400, "No such user / Exception message", typeof(string))]
        [SwaggerResponse(200, "Password changed")]
        public async Task<IActionResult> UserChangePasswordOnDefaultAsync( [FromBody] string email )
        {
            try
            {             
                var user = _accountProvider.GetUserIncludeCompany(email);
                if (user == null) return BadRequest("No such user");
                user.PasswordHash = _loginService.GeneratePasswordHash("Test_User12345");
                await _accountProvider.SaveChangesAsync();
                return Ok("Password changed");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("Unblock")]
        [SwaggerOperation(Summary = "Unblock in case failed attempts to log in", Description = "Unblock, zero counter of failed log in, hange password for user. Send email with new password")]
        [SwaggerResponse(400, "No such user / Token wrong", typeof(string))]
        [SwaggerResponse(200, "Password changed")]
        public async Task<IActionResult> Unblock(
                    [FromBody, SwaggerParameter("email required")] string email,
                    [FromHeader, SwaggerParameter("JWT token required ( token of admin or manager )", Required = true)] string Authorization)
        {
            try
            {
                ApplicationUser user = _accountProvider.GetUserIncludeCompany(email);
                if (_loginService.GetDataFromToken(Authorization, out userClaims))
                {
                    string password = _loginService.GeneratePass(6);
                    user.PasswordHash = _loginService.GeneratePasswordHash(password);
                    user.StatusId = _accountProvider.GetStatusId("Active");
                    await _mailSender.SendPasswordChangeEmail(user, password);
                    _accountProvider.SaveChanges();
                    return Ok("Password changed");
                }
                return BadRequest("Token wrong");
            }
            catch (Exception e)
            {
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
                    await _accountProvider.RemoveAccountWithSave(email);
                    transactionScope.Complete();
                    return Ok("Removed");
                }
                catch (Exception e)
                {
                    return BadRequest(e.Message);
                }
            }
        }


        [HttpGet("[action]")]
        public void AddCompanyDictionary(string fileName)
        {
            _helpProvider.AddComanyPhrases();
        }
    }
}