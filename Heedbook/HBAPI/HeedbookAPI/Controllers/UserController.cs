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
using System.Net.Http.Headers;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private Dictionary<string, string> userClaims;



        public UserController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
        }

        [HttpGet("User")]
        [SwaggerOperation(Description = "Return all users for loggined company with role Ids")]
        public IActionResult UserGet([FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);
                var users = _context.ApplicationUsers.Include(p => p.UserRoles).Where(p => p.CompanyId == companyId && p.StatusId == 3).ToList();
                var result = users.Select(p => new UserModel(p));
                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut("User")]
        [SwaggerOperation(Description = "Edit user and return edited")]
        public async Task<IActionResult> UserPut([FromBody] ApplicationUser message, [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);
                var user = _context.ApplicationUsers.Include(p => p.UserRoles)
                    .Where(p => p.Id == message.Id && p.CompanyId.ToString() == userClaims["companyId"] && p.StatusId == 3)
                    .FirstOrDefault();
                if (user.Email != message.Email && _context.ApplicationUsers.Where(x => x.NormalizedEmail == message.Email.ToUpper()).Any())
                    return BadRequest("User email not unique");
                if (user != null)
                {
                    foreach (var p in typeof(ApplicationUser).GetProperties())
                    {
                        if (p.GetValue(message, null) != null)
                            p.SetValue(user, p.GetValue(message, null), null);
                    }
                    await _context.SaveChangesAsync();
                    return Ok(new UserModel(user));
                }
                else
                {
                    return BadRequest("No such user");
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("User")]
        [SwaggerOperation(Description = "Create new user with role Manager in loggined company (taked from token)/ Return new user")]
        public async Task<IActionResult> UserPostAsync([FromBody] PostUser message, [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                if (_context.ApplicationUsers.Where(x => x.NormalizedEmail == message.Email.ToUpper()).Any())
                    return BadRequest("User email not unique");
                string password = GeneratePass(6);
                var user = new ApplicationUser
                {
                    UserName = message.Email,
                    NormalizedUserName = message.Email.ToUpper(),
                    Email = message.Email,
                    NormalizedEmail = message.Email.ToUpper(),
                    Id = Guid.NewGuid(),
                    CompanyId = Guid.Parse(userClaims["companyId"]),
                    CreationDate = DateTime.UtcNow,
                    FullName = message.FullName,
                    PasswordHash = _loginService.GeneratePasswordHash(password),
                    StatusId = 3,
                    EmpoyeeId = message.EmployeeId
                };
                string msg = GenerateEmailMsg(password, user);
                _loginService.SendEmail(message.Email, "Registration on Heedbook", msg);
                await _context.AddAsync(user);

                var userRole = new ApplicationUserRole()
                {
                    UserId = user.Id,
                    RoleId = Guid.Parse(message.RoleId) //Manager role
                };
                await _context.ApplicationUserRoles.AddAsync(userRole);
                await _context.SaveChangesAsync();
                return Ok(new UserModel(user));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("User")]
        [SwaggerOperation(Description = "Delete user by Id if he hasn't any relations in DB or make status Disabled")]
        public async Task<IActionResult> UserDelete([FromQuery] Guid applicationUserId, [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);
                var user = _context.ApplicationUsers.Where(p => p.Id == applicationUserId && p.CompanyId == companyId).FirstOrDefault();

                if (user != null)
                {
                    try
                    {
                        _context.Remove(user);
                        await _context.SaveChangesAsync();
                    }
                    catch
                    {
                        user.StatusId = _context.Statuss.Where(p => p.StatusName == "Disabled").FirstOrDefault().StatusId;
                        await _context.SaveChangesAsync();
                    }
                    return Ok("Deleted");
                }
                return BadRequest("No such user");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        [HttpGet("PhraseLib")]
        [SwaggerOperation(Description = "Return collections phrases for loggined company")]
        public IActionResult PhraseGet([FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyIdUser = Guid.Parse(userClaims["companyId"]);
                return Ok(_context.PhraseCompanys
                        .Include(p => p.Phrase)
                        .Where(p => p.CompanyId == companyIdUser && p.Phrase.PhraseText != null).Select(p => p.Phrase).ToList());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("PhraseLib")]
        [SwaggerOperation(Description = "Save new phrase to DB and attach it to loggined company (create new PhraseCompany)")]
        public async Task<IActionResult> PhrasePost([FromBody] Phrase message, [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);

                await _context.Phrases.AddAsync(message);
                var phraseCompany = new PhraseCompany();
                phraseCompany.CompanyId = companyId;
                phraseCompany.PhraseCompanyId = Guid.NewGuid();
                phraseCompany.PhraseId = message.PhraseId;
                await _context.PhraseCompanys.AddAsync(phraseCompany);
                await _context.SaveChangesAsync();
                return Ok(message);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut("PhraseLib")]
        [SwaggerOperation(Description = "Edit phrase")]
        public async Task<IActionResult> PhrasePut([FromBody] Phrase message, [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);

                var phrase = _context.PhraseCompanys
                    .Include(p => p.Phrase)
                    .Where(p => p.Phrase.PhraseId == message.PhraseId && p.CompanyId == companyId)
                    .Select(p => p.Phrase)
                    .FirstOrDefault();

                if (phrase != null)
                {
                    foreach (var p in typeof(ApplicationUser).GetProperties())
                    {
                        if (p.GetValue(message, null) != null)
                            p.SetValue(phrase, p.GetValue(message, null), null);
                    }
                    await _context.SaveChangesAsync();
                    return Ok(phrase);
                }
                else
                {
                    return BadRequest("No permission for changing phrase");
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

        }

        [HttpDelete("PhraseLib")]
        [SwaggerOperation(Description = "Delete phrase (if this phrase used in any company return Bad request")]
        public async Task<IActionResult> PhraseDelete([FromQuery] Guid phraseId, [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);
                var phrase = _context.PhraseCompanys
                    .Include(p => p.Phrase)
                    .Where(p => p.Phrase.PhraseId == phraseId && p.CompanyId == companyId).FirstOrDefault();
                _context.Remove(phrase);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("CompanyPhrase")]
        [SwaggerOperation(Description = "Return phrase library ids collection for companies sended in params")]
        public IActionResult CompanyPhraseGet([FromQuery(Name = "companyId")] List<Guid> companyIds, [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyPhrase = _context.PhraseCompanys.Where(p => companyIds.Contains((Guid)p.CompanyId));
                return Ok(companyPhrase.Select(p => (Guid)p.PhraseId).ToList());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("CompanyPhrase")]
        [SwaggerOperation(Description = "Attach phrase to all companies sended in body (create new PhraseCompany entities)")]
        public async Task<IActionResult> CompanyPhrasePost([FromBody] CompanyPhrasePostModel message, [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                foreach (var companyId in message.companyIds)
                {
                    var phraseCompany = new PhraseCompany
                    {
                        PhraseCompanyId = Guid.NewGuid(),
                        CompanyId = companyId,
                        PhraseId = message.phraseId
                    };
                    await _context.AddAsync(phraseCompany);
                }
                await _context.SaveChangesAsync();
                return Ok("OK");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("CompanyPhrase")]
        [SwaggerOperation(Description = "Delete PhraseCompany from loggined company by Phrase Id")]
        public async Task<IActionResult> CompanyPhraseDelete([FromQuery] Guid phraseId, [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);

                var phrase = _context.PhraseCompanys.Where(p => p.CompanyId == companyId && p.PhraseId == phraseId).FirstOrDefault();
                if (phrase != null)
                {
                    _context.Remove(phrase);
                    await _context.SaveChangesAsync();
                }
                return Ok("OK");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        // to do: add dialogue phrase and add make migration 
        // format of datetime is yyyymmddhhmmss
        [HttpGet("Dialogue")]
        [SwaggerOperation(Description = "Return collection of dialogues from dialogue phrases by filters")]
        public IActionResult DialogueGet([FromQuery(Name = "begTime")] string beg,
                                                [FromQuery(Name = "endTime")] string end,
                                                [FromQuery(Name = "applicationUserId")] List<Guid> applicationUserIds,
                                                [FromQuery(Name = "phraseId")] List<Guid> phraseIds,
                                                [FromQuery(Name = "phraseTypeId")] List<Guid> phraseTypeIds,
                                                [FromQuery(Name = "workerTypeId")] List<Guid> workerTypeIds,
                                                [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);
                var formatString = "yyyyMMdd";
                var begTime = !String.IsNullOrEmpty(beg) ? DateTime.ParseExact(beg, formatString, CultureInfo.InvariantCulture) : DateTime.Now.AddDays(-6);
                var endTime = !String.IsNullOrEmpty(end) ? DateTime.ParseExact(end, formatString, CultureInfo.InvariantCulture) : DateTime.Now;


                begTime = begTime.Date;
                endTime = endTime.Date.AddDays(1);

                var dialogues = _context.DialoguePhrases
                .Include(p => p.Dialogue)
                .Include(p => p.Dialogue.ApplicationUser)
                .Where(p =>
                    p.Dialogue.BegTime >= begTime &&
                    p.Dialogue.EndTime <= endTime &&
                    p.Dialogue.ApplicationUser.CompanyId == companyId &&
                    (!applicationUserIds.Any() || applicationUserIds.Contains(p.Dialogue.ApplicationUserId)) &&
                    (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.Dialogue.ApplicationUser.WorkerTypeId)) &&
                    (!phraseIds.Any() || phraseIds.Contains((Guid)p.PhraseId)) &&
                    (!phraseTypeIds.Any() || phraseTypeIds.Contains((Guid)p.PhraseTypeId))
                    ).Select(p => p.Dialogue).ToList();
                return Ok(dialogues);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("DialogueInclude")]
        [SwaggerOperation(Description = "Return collection of dialogues with relative data by filters")]
        public IActionResult DialogueGetInclude([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "phraseId")] List<Guid> phraseIds,
                                                        [FromQuery(Name = "phraseTypeId")] List<Guid> phraseTypeIds,
                                                        [FromQuery(Name = "workerTypeId")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization
                                                        )
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);
                var formatString = "yyyyMMdd";
                var begTime = !String.IsNullOrEmpty(beg) ? DateTime.ParseExact(beg, formatString, CultureInfo.InvariantCulture) : DateTime.Now.AddDays(-6);
                var endTime = !String.IsNullOrEmpty(end) ? DateTime.ParseExact(end, formatString, CultureInfo.InvariantCulture) : DateTime.Now;


                begTime = begTime.Date;
                endTime = endTime.Date.AddDays(1);

                var dialogues = _context.Dialogues
                .Include(p => p.DialogueAudio)
                .Include(p => p.DialogueClientProfile)
                .Include(p => p.DialogueClientSatisfaction)
                .Include(p => p.DialogueFrame)
                .Include(p => p.DialogueInterval)
                .Include(p => p.DialoguePhrase)
                .Include(p => p.DialoguePhraseCount)
                .Include(p => p.DialogueSpeech)
                .Include(p => p.DialogueVisual)
                .Include(p => p.DialogueWord)
                .Include(p => p.ApplicationUser)

                .Where(p =>
                    p.BegTime >= begTime &&
                    p.EndTime <= endTime &&
                    (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId)) &&
                    (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)) &&
                    (!phraseIds.Any() || p.DialoguePhrase.Where(q => phraseIds.Contains((Guid)q.PhraseId)).Any()) &&
                    (!phraseTypeIds.Any() || p.DialoguePhrase.Where(q => phraseTypeIds.Contains((Guid)q.PhraseTypeId)).Any())
                    ).ToList();

                return Ok(dialogues);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        #region EmailSend
        public string GeneratePass(int x)
        {
            string pass = "";
            var r = new Random();
            while (pass.Length < x)
            {
                Char c = (char)r.Next(33, 125);
                if (Char.IsLetterOrDigit(c))
                    pass += c;
            }
            return pass;
        }
        public string GenerateEmailMsg(string pswd, ApplicationUser user)
            {
                string msg = "Login:    " + user.Email;
                msg += "   Password: " + pswd + ".";
                msg += " You were registred in Heedbook";
                return msg;
            }
        #endregion
    }

    public class PostUser
    {
        public string FullName;
        public string Email;
        public string EmployeeId;
        public string RoleId;
    }

    public class UserModel
    {
        public Guid Id;
        public string Email;
        public string FullName;
        public string Avatar;
        public string EmployeeId;
        public string CreationDate;
        public string CompanyId;
        public Int32? StatusId;
        public string OneSignalId;
        public Guid? WorkerTypeId;
        public string RoleId;
        public UserModel(ApplicationUser user)
        {
            Id = user.Id;
            FullName = user.FullName;
            Email = user.Email;
            Avatar = user.Avatar;
            EmployeeId = user.EmpoyeeId;
            CreationDate = user.CreationDate.ToLongDateString();
            CompanyId = user.CompanyId.ToString();
            StatusId = user.StatusId;
            OneSignalId = user.OneSignalId;
            WorkerTypeId = user.WorkerTypeId;
            RoleId = user.UserRoles.FirstOrDefault().RoleId.ToString();
        }
    }

    public class CompanyPhrasePostModel
    {
        public List<Guid> companyIds { get; set; }   
        public Guid phraseId { get; set; }       
    }

}