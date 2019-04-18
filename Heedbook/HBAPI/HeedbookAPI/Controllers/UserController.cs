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

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;


        public UserController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _loginService = loginService;
            _context = context;
        }        
        
        [HttpGet("User")]
        public IEnumerable<ApplicationUser> UserGet([FromHeader] string Authorization)
        {
            var userClaims = _loginService.GetDataFromToken(Authorization);
            var companyId = Guid.Parse(userClaims["companyId"]);
            var users = _context.ApplicationUsers.Where(p => p.CompanyId == companyId && p.StatusId == 3).ToList();
            return users;
        }

        [HttpPut("User")]
        public IActionResult UserPut([FromBody] ApplicationUser message, [FromHeader] string Authorization)
        {
            try
            {
                var userClaims = _loginService.GetDataFromToken(Authorization);
                var companyId = Guid.Parse(userClaims["companyId"]);
                var user = _context.ApplicationUsers
                    .Where(p => p.Id == message.Id && p.CompanyId.ToString() == userClaims["companyId"] && p.StatusId == 3)
                    .FirstOrDefault();
                if (user != null)
                {
                    foreach(var p in typeof(ApplicationUser).GetProperties()) 
                    {
                        if (p.GetValue(message, null) != null)
                            p.SetValue(user, p.GetValue(message, null), null);
                    }
                    _context.SaveChanges();
                    return Ok(user);
                }
                else
                {
                    return Ok();
                }
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpPost("User")]
        public async Task<IActionResult> UserPostAsync([FromBody] PostUser message, [FromHeader] string Authorization)
        {
            try
            {
                var userClaims = _loginService.GetDataFromToken(Authorization);
                var user = new ApplicationUser { 
                    UserName = message.Email,
                    Email = message.Email,
                    Id = Guid.NewGuid(),
                    CompanyId = Guid.Parse(userClaims["companyId"]),
                    CreationDate = DateTime.UtcNow,
                    FullName = message.FullName,
                    StatusId = 3,
                    EmpoyeeId = message.EmployeeId
                };
                var result = await _userManager.CreateAsync(user, message.Password);
                await _userManager.AddToRoleAsync(user, "Employee");
                return Ok(user);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpDelete("User")]
        public IActionResult UserDelete([FromQuery] Guid applicationUserId, [FromHeader] string Authorization)
        {
            try
            {
                var userClaims = _loginService.GetDataFromToken(Authorization);
                var companyId = Guid.Parse(userClaims["companyId"]);
                var user = _context.ApplicationUsers.Where(p => p.Id == applicationUserId && p.CompanyId == companyId).FirstOrDefault();
                 
                if (user != null)
                {
                    try
                    {
                        _context.Remove(user);
                        _context.SaveChanges();
                    }
                    catch
                    {
                        user.StatusId = 4;
                        _context.SaveChanges();
                    }
                }
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }


        [HttpGet("PhraseLib")]
        public IEnumerable<Phrase> PhraseGet([FromQuery] Guid companyId)
        {
            if (companyId == null)
            {
                return _context.Phrases.Where(p => p.PhraseText != null);
            }
            else
            {
                return _context.PhraseCompanys
                    .Include(p => p.Phrase)
                    .Where(p => p.Phrase.PhraseText != null && p.PhraseCompanyId == companyId)
                    .Select(p => p.Phrase);
            }
        }

        [HttpPost("PhraseLib")]
        public Phrase PhrasePost([FromBody] Phrase message)
        {
            _context.Add(message);
            _context.SaveChanges();
            return message;
        }

        [HttpPut("PhraseLib")]
        public Phrase PhrasePut([FromBody] Phrase message)
        {
            var phrase = _context.Phrases.Where(p => p.PhraseId == message.PhraseId).FirstOrDefault();
            if (phrase == null)
            {
                _context.Add(message);
                _context.SaveChanges();
            }
            else
            {
                foreach(var p in typeof(ApplicationUser).GetProperties()) 
                {
                    if (p.GetValue(message, null) != null)
                        p.SetValue(phrase, p.GetValue(message, null), null);
                }
                _context.SaveChanges();
            }
            return phrase;
        }

        [HttpDelete("PhraseLib")]
        public IActionResult PhraseDelete([FromQuery] Guid phraseId)
        {
           var phrase =  _context.Phrases.Where(p => p.PhraseId == phraseId).FirstOrDefault();
           _context.Remove(phrase);
           _context.SaveChanges();
           
            return Ok();
        }

        [HttpGet("CompanyPhrase")]
        public List<Guid> CompanyPhraseGet([FromQuery(Name = "companyId")] List<Guid> companyIds)
        {
            var companyPhrase = _context.PhraseCompanys.Where(p => companyIds.Contains((Guid) p.CompanyId));
            return companyPhrase.Select(p => (Guid) p.PhraseId).ToList();
        }  

        [HttpPost("CompanyPhrase")]
        public IActionResult CompanyPhraseGet([FromQuery(Name = "companyId")] List<Guid> companyIds, [FromQuery] Guid phraseId)
        {
            try
            {
                foreach (var companyId in companyIds)
                {
                    var phraseCompany = new PhraseCompany {
                        PhraseCompanyId = Guid.NewGuid(),
                        CompanyId = companyId,
                        PhraseId = phraseId
                    };
                    _context.Add(phraseCompany);
                }
                _context.SaveChanges();
                return Ok("OK");
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }  

        [HttpDelete("CompanyPhrase")]
        public IActionResult CompanyPhraseDelete([FromQuery] Guid companyId, [FromQuery] Guid phraseId)
        {
            try
            {
                var phrase = _context.PhraseCompanys.Where(p => p.CompanyId == companyId && p.PhraseId == phraseId);
                if (phrase != null)
                {
                    _context.Remove(phrase);
                    _context.SaveChanges();
                }
                return Ok("OK");
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }


        // to do: add dialogue phrase and add make migration 
        // format of datetime is yyyymmddhhmmss
        [HttpGet("Dialogue")]
        public IEnumerable<Dialogue> DialogueGet([FromQuery(Name = "begTime")] string beg,
                                                [FromQuery(Name = "endTime")] string end, 
                                                [FromQuery(Name = "applicationUserId")] List<Guid> applicationUserIds,
                                                [FromQuery(Name = "phraseId")] List<Guid> phraseIds,
                                                [FromQuery(Name = "phraseTypeId")] List<Guid> phraseTypeIds,
                                                [FromQuery(Name = "workerTypeId")] List<Guid> workerTypeIds,
                                                [FromHeader] string Authorization)
        {
            var userClaims = _loginService.GetDataFromToken(Authorization);
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
                (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.Dialogue.ApplicationUser.WorkerTypeId)) &&
                (!phraseIds.Any() || phraseIds.Contains((Guid) p.PhraseId)) && 
                (!phraseTypeIds.Any() || phraseTypeIds.Contains((Guid) p.PhraseTypeId))
                ).Select(p => p.Dialogue).ToList();

            return dialogues;
        }

        [HttpGet("DialogueInclude")]
        public IEnumerable<Dialogue> DialogueGetInclude([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "phraseId")] List<Guid> phraseIds,
                                                        [FromQuery(Name = "phraseTypeId")] List<Guid> phraseTypeIds,
                                                        [FromQuery(Name = "workerTypeId")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization
                                                        )
        {
            var userClaims = _loginService.GetDataFromToken(Authorization);
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
                (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)) &&
                (!phraseIds.Any() || p.DialoguePhrase.Where(q => phraseIds.Contains((Guid) q.PhraseId)).Any()) && 
                (!phraseTypeIds.Any() || p.DialoguePhrase.Where(q => phraseTypeIds.Contains((Guid) q.PhraseTypeId)).Any())
                ).ToList();

            return dialogues;
        }
    }

    public class PostUser
    {
        public string FullName;
        public string Email;
        public string Password;
        public string EmployeeId;
    }

    public class PutUser
    {
        public Guid Id;
        public string FullName;
        public string Avatar;
        public string EmployeeId;
        public string CreationDate;
        public string CompanyId;
        public Int32 StatusId;
        public string OneSignalId;
        public Guid? WorkerTypeId;
    }
    
}