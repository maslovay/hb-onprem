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

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : Controller
    {
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
//        private readonly ElasticClient _log;
        private Dictionary<string, string> userClaims;
        private readonly IMailSender _mailSender;

        public AccountController(
            ILoginService loginService,
            RecordsContext context,
//            ElasticClient log,      
            IMailSender mailSender
            )
        {
            _loginService = loginService;
            _context = context;
//            _log = log;
            _mailSender = mailSender;
        }

        [HttpPost("Register")]
        [SwaggerOperation(Summary = "Create user, company, trial tariff",
            Description = "Create new active company, new active user, add manager role, create new trial Tariff on 5 days/2 employee and new finished Transaction if no exist")]
        public async Task<IActionResult> UserRegister([FromBody,
                        SwaggerParameter("User and company data", Required = true)]
                        UserRegister message)
        {
//            _log.Info("Account/Register started");
            Guid contentPrototypeId = new Guid("07565966-7db2-49a7-87d4-1345c729a6cb");
            var statusActiveId = _context.Statuss.FirstOrDefault(p => p.StatusName == "Active").StatusId;//---active

            if (_context.Companys.Where(x => x.CompanyName == message.CompanyName).Any() || _context.ApplicationUsers.Where(x => x.NormalizedEmail == message.Email.ToUpper()).Any())
                return BadRequest("Company name or user email not unique");
           // using (var transactionScope = new TransactionScope())
            {
                try
                {
                    //---1---company---
                    var companyId = Guid.NewGuid();
                    var company = new Company
                    {
                        CompanyId = companyId,
                        CompanyIndustryId = message.CompanyIndustryId,
                        CompanyName = message.CompanyName,
                        LanguageId = message.LanguageId,
                        CreationDate = DateTime.UtcNow,
                        CountryId = message.CountryId,
                        CorporationId = message.CorporationId,
                        StatusId = _context.Statuss.FirstOrDefault(p => p.StatusName == "Inactive").StatusId//---inactive
                    };
                    await _context.Companys.AddAsync(company);
//                    _log.Info("Company created");

                    //---2---user---
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
                        StatusId = statusActiveId
                    };
                    await _context.AddAsync(user);
                    _loginService.SavePasswordHistory(user.Id, user.PasswordHash);
                    //                    _log.Info("User created");

                    //---3--user role---
                    message.Role = message.Role ?? "Manager";
                    var userRole = new ApplicationUserRole()
                    {
                        UserId = user.Id,
                        RoleId = _context.Roles.First(p => p.Name == message.Role).Id //Manager or Supervisor role
                    };
                    await _context.ApplicationUserRoles.AddAsync(userRole);

                    //---4---tariff---
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
//                        _log.Info("Tariff created");

                        //---5---transaction---
                        var transaction = new HBData.Models.Transaction
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
                        company.StatusId = statusActiveId;
//                        _log.Info("Transaction created");
                        await _context.Tariffs.AddAsync(tariff);
                        await _context.Transactions.AddAsync(transaction);

                        //---6---ADD WORKER TYPES CATALOGUE CONNECTED TO NEW COMPANY
                        var workerType = new WorkerType
                        {
                            WorkerTypeId = Guid.NewGuid(),
                            CompanyId = companyId,
                            WorkerTypeName = "Employee"
                        };
//                        _log.Info("WorkerTypes created");
                        await _context.WorkerTypes.AddAsync(workerType);

                        //---7---content and campaign clone
                        var content = _context.Contents.FirstOrDefault(x => x.ContentId == contentPrototypeId);
                        if (content != null)
                        {
                            content.ContentId = Guid.NewGuid();
                            content.CompanyId = companyId;
                            content.StatusId = statusActiveId;
                            await _context.Contents.AddAsync(content);

                            Campaign campaign = new Campaign
                            {
                                CampaignId = Guid.NewGuid(),
                                CompanyId = companyId,
                                BegAge = 0,
                                BegDate = DateTime.Now.AddDays(-1),
                                CreationDate = DateTime.Now,
                                EndAge = 100,
                                EndDate = DateTime.Now.AddDays(30),
                                GenderId = 0,
                                IsSplash = true,
                                Name = "PROTOTYPE",
                                StatusId = statusActiveId
                            };
                            await _context.Campaigns.AddAsync(campaign);
                            CampaignContent campaignContent = new CampaignContent
                            {
                                CampaignContentId = Guid.NewGuid(),
                                CampaignId = campaign.CampaignId,
                                ContentId = content.ContentId,
                                SequenceNumber = 1,
                                StatusId = statusActiveId
                            };
                            await _context.CampaignContents.AddAsync(campaignContent);
                        }

                        await _context.SaveChangesAsync();
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
                    user = _context.ApplicationUsers.FirstOrDefault(x => x.Id == userId && x.NormalizedEmail == message.UserName.ToUpper());
                    user.PasswordHash = _loginService.GeneratePasswordHash(message.Password);
                    if (!_loginService.SavePasswordHistory(user.Id, user.PasswordHash))//---check 5 last passwords
                        return BadRequest("password was used");
                }
                //---IF USER NOT LOGGINED HE RECEIVE GENERATED PASSWORD ON EMAIL
                else
                {
                    user = _context.ApplicationUsers.Include(x => x.Company).FirstOrDefault(x => x.NormalizedEmail == message.UserName.ToUpper());
                    if (user == null)
                        return BadRequest("No such user");
                    string password = _loginService.GeneratePass(6);
                    await _mailSender.SendPasswordChangeEmail(user, password);
                    user.PasswordHash = _loginService.GeneratePasswordHash(password);
                }
                await _context.SaveChangesAsync();
//                _log.Info("Account/ change password finished");
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
                var user = _context.ApplicationUsers.FirstOrDefault(x => x.Email.ToUpper() == email.ToUpper());
                if (user == null) return BadRequest("No such user");
                user.PasswordHash = _loginService.GeneratePasswordHash("Test_User12345");                  
                await _context.SaveChangesAsync();
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
                ApplicationUser user = _context.ApplicationUsers.Include(x => x.Company).FirstOrDefault(x => x.NormalizedEmail == email.ToUpper());
                if (_loginService.GetDataFromToken(Authorization, out userClaims))
                {
                    string password = _loginService.GeneratePass(6);
                    string text = string.Format("<table>" +
                     "<tr><td>login:</td><td> {0}</td></tr>" +
                     "<tr><td>password:</td><td> {1}</td></tr>" +
                     "</table>", user.Email, password);
                    _mailSender.SendPasswordChangeEmail(user, text);
                    user.PasswordHash = _loginService.GeneratePasswordHash(password);
                    user.StatusId = _context.Statuss.FirstOrDefault(x => x.StatusName == "Active").StatusId;
                    _loginService.SaveErrorLoginHistory(user.Id, "success");
                }
                await _context.SaveChangesAsync();
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
                    var user = _context.ApplicationUsers.FirstOrDefault(p => p.Email == email);
                    Company company = _context.Companys.FirstOrDefault(x => x.CompanyId == user.CompanyId);
                    var users = _context.ApplicationUsers.Include(x=>x.UserRoles).Where(x => x.CompanyId == company.CompanyId).ToList();
                    var tariff = _context.Tariffs.FirstOrDefault(x => x.CompanyId == company.CompanyId);
                    var transactions = _context.Transactions.Where(x => x.TariffId == tariff.TariffId).ToList();
                    var userRoles = users.SelectMany(x => x.UserRoles).ToList();
                    var workerTypes = _context.WorkerTypes.Where(x => x.CompanyId == company.CompanyId).ToList();
                    var contents = _context.Contents.Where(x => x.CompanyId == company.CompanyId).ToList();
                    var campaigns = _context.Campaigns.Include(x => x.CampaignContents).Where(x => x.CompanyId == company.CompanyId).ToList();
                    var campaignContents = campaigns.SelectMany(x => x.CampaignContents).ToList();
                    var phrases = _context.PhraseCompanys.Where(x => x.CompanyId == company.CompanyId).ToList();
                    var pswdHist = _context.PasswordHistorys.Where(x => users.Select(p=>p.Id).Contains( x.UserId)).ToList();

                    if (pswdHist.Count() != 0)
                        _context.RemoveRange(pswdHist);
                    if (phrases != null && phrases.Count() != 0)
                    _context.RemoveRange(phrases);
                    if (campaignContents.Count() != 0)
                        _context.RemoveRange(campaignContents);
                    if (campaigns.Count() != 0)
                        _context.RemoveRange(campaigns);
                    if (contents.Count() != 0)
                        _context.RemoveRange(contents);
                    _context.RemoveRange(workerTypes);
                    _context.RemoveRange(userRoles);
                    _context.RemoveRange(transactions);
                    _context.RemoveRange(users);
                    _context.Remove(tariff);
                    _context.Remove(company);
                    _context.SaveChanges();
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