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
using HBLib.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;

        private readonly SftpClient _sftpClient;
        private Dictionary<string, string> userClaims;



        public UserController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            SftpClient sftpClient
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _sftpClient = sftpClient;
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
                        if (p.GetValue(message, null) != null && p.GetValue(message, null).ToString() != Guid.Empty.ToString())
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
                //string password = GeneratePass(6);
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
                    PasswordHash = _loginService.GeneratePasswordHash(message.Password),
                    StatusId = 3,
                    EmpoyeeId = message.EmployeeId,
                    WorkerTypeId = message.WorkerTypeId
                };
                //string msg = GenerateEmailMsg(password, user);
                //_loginService.SendEmail(message.Email, "Registration on Heedbook", msg);
                await _context.AddAsync(user);

                var userRole = new ApplicationUserRole()
                {
                    UserId = user.Id,
                    RoleId = _context.Roles.FirstOrDefault(x=>x.Name == "Employee").Id //Manager role
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
        public async Task<IActionResult> PhrasePost([FromBody] PhrasePost message, [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);
                var phrase = new Phrase {
                    PhraseId = Guid.NewGuid(),
                    PhraseText = message.PhraseText,
                    PhraseTypeId = message.PhraseTypeId,
                    LanguageId = message.LanguageId,
                    IsClient = message.IsClient,
                    WordsSpace = message.WordsSpace,
                    Accurancy = message.Accurancy,
                    IsTemplate = message.IsTemplate
                };

                await _context.Phrases.AddAsync(phrase);
                var phraseCompany = new PhraseCompany();
                phraseCompany.CompanyId = companyId;
                phraseCompany.PhraseCompanyId = Guid.NewGuid();
                phraseCompany.PhraseId = phrase.PhraseId;
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
        public async Task<IActionResult> PhrasePut([FromBody] PhrasePut message, [FromHeader] string Authorization)
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

                System.Console.WriteLine(companyId);
                System.Console.WriteLine(begTime);
                System.Console.WriteLine(endTime);



                var dialogues = _context.Dialogues
                .Include(p => p.DialoguePhrase)
                .Include(p => p.ApplicationUser)
                .Include(p => p.DialogueHint)
                .Include(p => p.DialogueClientProfile)
                .Where(p => 
                    p.BegTime >= begTime &&
                    p.EndTime <= endTime &&
                    p.ApplicationUser.CompanyId == companyId &&
                    p.StatusId == 3 && p.InStatistic == true &&
                    (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId)) &&
                    (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)) &&
                    (!phraseIds.Any() || p.DialoguePhrase.Any(q => phraseIds.Contains((Guid) q.PhraseId))) &&
                    (!phraseTypeIds.Any() || p.DialoguePhrase.Any(q => phraseTypeIds.Contains((Guid) q.PhraseTypeId)))
                )
                .Select(p => new {
                    DialogueId = p.DialogueId,
                    Avatar = (p.DialogueClientProfile.FirstOrDefault() == null) ? null : _sftpClient.GetFileUrlFast($"clientavatars/{p.DialogueClientProfile.FirstOrDefault().Avatar}"),
                    ApplicationUserId = p.ApplicationUserId,
                    FullName = p.ApplicationUser.FullName,
                    DialogueHints = p.DialogueHint,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    CreationTime = p.CreationTime,
                    Comment = p.Comment,
                    SysVersion = p.SysVersion,
                    StatusId = p.StatusId,
                    InStatistic = p.InStatistic
                })
                .ToList();
                return Ok(dialogues);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("DialogueInclude")]
        [SwaggerOperation(Description = "Return collection of dialogues with relative data by filters")]
        public IActionResult DialogueGetInclude([FromQuery(Name = "dialogueId")] List<Guid> dialogueIds,
                                                [FromHeader] string Authorization)
        {
            try
            {
                var begTime = DateTime.UtcNow.AddDays(-30);
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);
                var avgDialogueTime = _context.Dialogues.Where(p =>
                    p.BegTime >= begTime &&
                    p.StatusId == 3 && p.InStatistic == true &&
                    p.ApplicationUser.CompanyId == companyId)
                .Average(p => p.EndTime.Subtract(p.BegTime).Minutes);
                
                var dialogue = _context.Dialogues
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
                    .Include(p => p.DialogueHint)
                    .Where(p => p.ApplicationUser.CompanyId == companyId 
                        && p.InStatistic == true 
                        && p.StatusId == 3
                        && (!dialogueIds.Any() || dialogueIds.Contains(p.DialogueId)))
                    .FirstOrDefault();   
                
                if (dialogue == null) return BadRequest("No such dialogue or user does not have permission for dialogue");


                var jsonDialogue = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(dialogue));
                jsonDialogue["FullName"] = dialogue.ApplicationUser.FullName;
                jsonDialogue["Avatar"] = (dialogue.DialogueClientProfile.FirstOrDefault() == null) ? null : _sftpClient.GetFileUrlFast($"clientavatars/{dialogue.DialogueClientProfile.FirstOrDefault().Avatar}");
                jsonDialogue["Video"] = dialogue == null ? null :_sftpClient.GetFileUrlFast($"dialoguevideos/{dialogue.DialogueId}.mkv");
                jsonDialogue["DialogueAvgDurationLastMonth"] = avgDialogueTime;
                return Ok(jsonDialogue);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }

    // #region EmailSend
    // public string GeneratePass(int x)
    // {
    //     string pass = "";
    //     var r = new Random();
    //     while (pass.Length < x)
    //     {
    //         Char c = (char)r.Next(33, 125);
    //         if (Char.IsLetterOrDigit(c))
    //             pass += c;
    //     }
    //     return pass;
    // }
    // public string GenerateEmailMsg(string pswd, ApplicationUser user)
    //     {
    //         string msg = "Login:    " + user.Email;
    //         msg += "   Password: " + pswd + ".";
    //         msg += " You were registred in Heedbook";
    //         return msg;
    //     }
    // #endregion

    public class PostUser
    {
        public string FullName;
        public string Email;
        public string EmployeeId;
    //    public string RoleId;
        public string Password;
        public Guid WorkerTypeId;

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

    public class PhrasePost
    {
        public string PhraseText;
        public Guid PhraseTypeId;
        public Int32? LanguageId;
        public bool IsClient;
        public Int32? WordsSpace;
        public double? Accurancy;
        public Boolean IsTemplate;
    }

    public class PhrasePut
    {
        public Guid PhraseId;
        public string PhraseText;
        public Guid PhraseTypeId;
        public Int32? LanguageId;
        public bool IsClient;
        public Int32? WordsSpace;
        public double? Accurancy;
        public Boolean IsTemplate;
    }

}