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
using UserOperations.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly RequestFilters _requestFilters;
        private readonly SftpClient _sftpClient;
        // private readonly ElasticClient _log;
        private Dictionary<string, string> userClaims;
        private readonly string _containerName;
        private readonly int activeStatus;
        private readonly int disabledStatus;

        public UserController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            SftpClient sftpClient,
            RequestFilters requestFilters
            // ElasticClient log
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _sftpClient = sftpClient;
            _requestFilters = requestFilters;
            // _log = log;
            _containerName = "useravatars";

            // activeStatus = _context.Statuss.FirstOrDefault(p => p.StatusName == "Active").StatusId;
            // disabledStatus = _context.Statuss.FirstOrDefault(p => p.StatusName == "Disabled").StatusId;
            activeStatus = 3;
            disabledStatus = 4;
        }

        [HttpGet("User")]
        [SwaggerOperation(Summary = "All company users", Description = "Return all active (status 3) users (array) for loggined company with role Id")]
        [SwaggerResponse(200, "Users with role", typeof(List<UserModel>))]
        public async Task<IActionResult> UserGet(
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                // _log.Info("User/User GET started"); 
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                List<ApplicationUser> users = null;

                var companyId = Guid.Parse(userClaims["companyId"]);
                var corporationId = userClaims["corporationId"];
                var role = userClaims["role"];
                        users = _context.ApplicationUsers.Include(p => p.UserRoles).ThenInclude(x => x.Role)
                            .Where(p => p.CompanyId == companyId && p.StatusId == activeStatus || p.StatusId == disabledStatus).ToList();     //2 active, 3 - disabled    
                if ( role == "Admin")
                        users = _context.ApplicationUsers.Include(p => p.UserRoles).ThenInclude(x => x.Role)
                            .Where(p => p.StatusId == activeStatus || p.StatusId == disabledStatus).ToList();     //2 active, 3 - disabled  
                if (role == "Supervisor" && corporationId != null)
                {
                        users = _context.ApplicationUsers
                            .Include(p => p.UserRoles)
                            .ThenInclude(x => x.Role)
                            .Include(x => x.Company)
                            .Where(p => (p.StatusId == activeStatus || p.StatusId == disabledStatus)
                            && p.Company.CorporationId.ToString() == corporationId).ToList();     //2 active, 3 - disabled  
                }
                var result = users.Select(p => new UserModel(p, p.Avatar != null ? _sftpClient.GetFileLink(_containerName, p.Avatar, default(DateTime)).path : null));
                // _log.Info("User/User GET finished");
                return Ok(result);
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }

        [HttpPost("User")]
        [SwaggerOperation(Description = "Create new user with role Employee in loggined company (taked from token) and can save avatar for user / Return new user")]
        [SwaggerResponse(200, "User", typeof(UserModel))]
        public async Task<IActionResult> UserPostAsync(
                    //  [FromBody] PostUser message,  
                    [FromForm, SwaggerParameter("json user (include password and unique email) in FormData with key 'data' + file avatar (not required)")] IFormCollection formData,
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                // _log.Info("User/User POST started");
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var userDataJson = formData.FirstOrDefault(x => x.Key == "data").Value.ToString();
                PostUser message = JsonConvert.DeserializeObject<PostUser>(userDataJson);
                if (_context.ApplicationUsers.Where(x => x.NormalizedEmail == message.Email.ToUpper()).Any())
                    return BadRequest("User email not unique");

                var isAdmin = userClaims["role"] == "Admin";
                message.RoleId = message.RoleId != null && isAdmin ? message.RoleId : _context.Roles.FirstOrDefault(x => x.Name == "Employee").Id.ToString(); //Manager role
                message.CompanyId = message.CompanyId != null && isAdmin ? message.CompanyId : userClaims["companyId"];

                //string password = GeneratePass(6);
                var user = new ApplicationUser
                {
                    UserName = message.Email,
                    NormalizedUserName = message.Email.ToUpper(),
                    Email = message.Email,
                    NormalizedEmail = message.Email.ToUpper(),
                    Id = Guid.NewGuid(),
                    CompanyId = Guid.Parse(message.CompanyId),
                    CreationDate = DateTime.UtcNow,
                    FullName = message.FullName,
                    PasswordHash = _loginService.GeneratePasswordHash(message.Password),
                    StatusId = activeStatus,//3
                    EmpoyeeId = message.EmployeeId,
                    WorkerTypeId = message.WorkerTypeId
                };
                await _context.AddAsync(user);
                _loginService.SavePasswordHistory(user.Id, user.PasswordHash);//---save password
                string avatarUrl = null;
                //---save avatar---
                if (formData.Files.Count != 0)
                {
                    FileInfo fileInfo = new FileInfo(formData.Files[0].FileName);
                    var fn = user.Id + fileInfo.Extension;
                    var memoryStream = formData.Files[0].OpenReadStream();
                    await _sftpClient.UploadAsMemoryStreamAsync(memoryStream, $"{_containerName}/", fn, true);
                    user.Avatar = fn;
                    avatarUrl = _sftpClient.GetFileLink(_containerName, fn, default(DateTime)).path;
                }

                var userRole = new ApplicationUserRole()
                {
                    UserId = user.Id,
                    RoleId = Guid.Parse(message.RoleId)
                };
                await _context.ApplicationUserRoles.AddAsync(userRole);
                await _context.SaveChangesAsync();
                //    return Ok(JsonConvert.SerializeObject(new UserModel(user, avatarUrl)));
                // _log.Info("User/User POST finished");
                return Ok(new UserModel(user, avatarUrl));
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }

        [HttpPut("User")]
        [SwaggerOperation(Summary = "Edit user",
                Description = "Edit user (any from loggined company) and return edited. Don't send password and role (can't change). Email must been unique. May contain avatar file")]
        [SwaggerResponse(200, "Edited user", typeof(UserModel))]
        public async Task<IActionResult> UserPut(
                    [FromForm, SwaggerParameter("Avatar file (not required) + json User with key 'data' in FormData")] IFormCollection formData,
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                // _log.Info("User/User PUT started");
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");

                var userDataJson = formData.FirstOrDefault(x => x.Key == "data").Value.ToString();
                ApplicationUser message = JsonConvert.DeserializeObject<ApplicationUser>(userDataJson);
                var companyId = Guid.Parse(userClaims["companyId"]);
                var user = _context.ApplicationUsers.Include(p => p.UserRoles)// 2 - active, 3 - disabled
                    .Where(p => p.Id == message.Id && p.CompanyId.ToString() == userClaims["companyId"]
                            && (p.StatusId == activeStatus || p.StatusId == disabledStatus))
                    .FirstOrDefault();
                // if (user.Email != message.Email && _context.ApplicationUsers.Where(x => x.NormalizedEmail == message.Email.ToUpper()).Any())
                //     return BadRequest("User email not unique");
                if (user != null)
                {
                    foreach (var p in typeof(ApplicationUser).GetProperties())
                    {
                        if (p.GetValue(message, null) != null && p.GetValue(message, null).ToString() != Guid.Empty.ToString())
                            p.SetValue(user, p.GetValue(message, null), null);
                    }
                    string avatarUrl = null;
                    if (formData.Files.Count != 0)
                    {
                        await Task.Run(() => _sftpClient.DeleteFileIfExistsAsync($"{_containerName}/{user.Id}"));
                        FileInfo fileInfo = new FileInfo(formData.Files[0].FileName);
                        var fn = user.Id + fileInfo.Extension;
                        var memoryStream = formData.Files[0].OpenReadStream();
                        await _sftpClient.UploadAsMemoryStreamAsync(memoryStream, $"{_containerName}/", fn, true);
                        user.Avatar = fn;
                        avatarUrl = _sftpClient.GetFileLink(_containerName, fn, default(DateTime)).path;
                    }
                    await _context.SaveChangesAsync();
                    // _log.Info("User/User PUT finished");
                    return Ok(new UserModel(user, avatarUrl));
                }
                else
                {
                    return BadRequest("No such user");
                }
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }


        [HttpDelete("User")]
        [SwaggerOperation(Summary = "Delete or make disabled", Description = "Delete user by Id if he hasn't any relations in DB or make status Disabled")]
        public async Task<IActionResult> UserDelete(
                    [FromQuery] Guid applicationUserId,
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                // _log.Info("User/User DELETE started");
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);
                var user = _context.ApplicationUsers
                    .Include(x => x.UserRoles)
                    .Include(x => x.PasswordHistorys)
                    .Where(p => p.Id == applicationUserId && p.CompanyId == companyId).FirstOrDefault();

                if (user != null)
                {
                    try
                    {
                        await Task.Run(() => _sftpClient.DeleteFileIfExistsAsync($"{_containerName}/{user.Id}"));
                        _context.RemoveRange(user.UserRoles);
                        _context.RemoveRange(user.PasswordHistorys);
                        _context.Remove(user);
                        await _context.SaveChangesAsync();
                    }
                    catch
                    {
                        user.StatusId = disabledStatus;
                        await _context.SaveChangesAsync();
                    }
                    // _log.Info("User/User DELETE finished");
                    return Ok("Deleted");
                }
                return BadRequest("No such user");
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }

        [HttpGet("Companies")]
        [SwaggerOperation(Summary = "All corporations companies", Description = "Return all companies for loggined corporation (only for role Supervisor)")]
        [SwaggerResponse(200, "Companies", typeof(List<Company>))]
        public async Task<IActionResult> CompaniesGet(
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                // _log.Info("User/Companies GET started");
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var corporationId = Guid.Parse(userClaims["corporationId"]);
                if (userClaims["role"] == "Supervisor") // only for own corporation
                {
                    var companies = _context.Companys // 2 active, 3 - disabled
                        .Where(p => p.CorporationId == corporationId && (p.StatusId == activeStatus || p.StatusId == disabledStatus)).ToList();
                    return Ok(companies);
                }
                if (userClaims["role"] == "Admin") // very cool!
                {
                    var companies = _context.Companys.Where(p => p.StatusId == activeStatus || p.StatusId == disabledStatus).ToList();  // 2 active, 3 - disabled
                    return Ok(companies);
                }
                return BadRequest("Not allowed access(role)");
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }

        [HttpGet("Corporations")]
        [SwaggerOperation(Summary = "All corporations", Description = "Return all corporations for loggined admins (only for role Admin)")]
        [SwaggerResponse(200, "Corporations", typeof(List<Company>))]
        public async Task<IActionResult> CorporationsGet(
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                // _log.Info("User/Corporations GET started");
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                if (userClaims["role"] != "Admin") return BadRequest("Not allowed access(role)"); ;
                var corporations = _context.Corporations.ToList();
                //  _log.Info("User/Corporations GET finished");
                return Ok(corporations);
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }


        [HttpGet("PhraseLib")]
        [SwaggerOperation(Summary = "Library",
                Description = "Return collections phrases from library (only templates and only with language code = loggined company language code) which company has not yet used")]
        [SwaggerResponse(200, "Library phrase collection", typeof(List<Phrase>))]
        public IActionResult PhraseGet(
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                // _log.Info("User/PhraseLib GET started");
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyIdUser = Guid.Parse(userClaims["companyId"]);
                var languageId = userClaims["languageCode"];

                var phrasesIncluded = _context.PhraseCompanys
                    .Include(x => x.Phrase)
                    .Where(x =>
                        x.CompanyId == companyIdUser &&
                        x.Phrase.IsTemplate == true &&
                        x.Phrase.PhraseText != null &&
                        x.Phrase.LanguageId.ToString() == languageId)
                    .Select(x => x.Phrase.PhraseId).ToList();

                 var phrases = _context.Phrases
                    .Where(x =>
                        x.IsTemplate == true &&
                        x.PhraseText != null &&
                        ! phrasesIncluded.Contains(x.PhraseId) &&
                        x.LanguageId.ToString() == languageId).ToList();

                // _log.Info("User/PhraseLib GET finished");
                return Ok(phrases);
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }

        [HttpPost("PhraseLib")]
        [SwaggerOperation(Summary = "Create company phrase (not library!)",
            Description = "Save new phrase to DB and attach it to loggined company (create new PhraseCompany). Assigned that the phrase is not template")]
        [SwaggerResponse(200, "New phrase", typeof(Phrase))]
        public async Task<IActionResult> PhrasePost(
                    [FromBody] PhrasePost message,
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                // _log.Info("User/PhraseLib POST started");
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);
                var languageId = Int32.Parse(userClaims["languageCode"]);
                var phrase = new Phrase
                {
                    PhraseId = Guid.NewGuid(),
                    PhraseText = message.PhraseText,
                    PhraseTypeId = message.PhraseTypeId,
                    LanguageId = languageId,
                    IsClient = message.IsClient,
                    WordsSpace = message.WordsSpace,
                    Accurancy = message.Accurancy,
                    IsTemplate = false
                };

                await _context.Phrases.AddAsync(phrase);
                var phraseCompany = new PhraseCompany();
                phraseCompany.CompanyId = companyId;
                phraseCompany.PhraseCompanyId = Guid.NewGuid();
                phraseCompany.PhraseId = phrase.PhraseId;
                await _context.PhraseCompanys.AddAsync(phraseCompany);
                await _context.SaveChangesAsync();
                // _log.Info("User/PhraseLib POST finished");
                return Ok(phrase);
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }

        [HttpPut("PhraseLib")]
        [SwaggerOperation(Summary = "Edit company phrase", Description = "Edit phrase. You can edit only your own phrase (not template from library)")]
        public async Task<IActionResult> PhrasePut(
                    [FromBody] Phrase message,
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                // _log.Info("User/PhraseLib PUT started");
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);

                var phrase = _context.PhraseCompanys
                    .Include(p => p.Phrase)
                    .Where(p => p.Phrase.PhraseId == message.PhraseId && p.CompanyId == companyId && p.Phrase.IsTemplate == false)
                    .Select(p => p.Phrase)
                    .FirstOrDefault();

                if (phrase != null)
                {
                    foreach (var p in typeof(Phrase).GetProperties())
                    {
                        if (p.GetValue(message, null) != null && p.GetValue(message, null).ToString() != Guid.Empty.ToString())
                            p.SetValue(phrase, p.GetValue(message, null), null);
                    }
                    await _context.SaveChangesAsync();
                    // _log.Info("User/PhraseLib PUT finished");
                    return Ok(phrase);
                }
                else
                {
                    return BadRequest("No permission for changing phrase");
                }
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }

        }

        [HttpDelete("PhraseLib")]
        [SwaggerOperation(Summary = "Delete company phrases", Description = "Delete phrases (if this phrases are Templates they can't be deleted, it only delete connection to company")]
        public async Task<IActionResult> PhraseDelete(
                    [FromQuery(Name = "phraseId"), SwaggerParameter("array ids to delete: id&id", Required = true)] List<Guid> phraseIds,
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                // _log.Info("User/PhraseLib DELETE started");
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);
                var phrasesCompany = _context.PhraseCompanys
                    .Include(p => p.Phrase)
                    .Where(p => phraseIds.Contains(p.Phrase.PhraseId) && p.CompanyId == companyId && p.Phrase.IsTemplate == false);
                var phrases = phrasesCompany.Select(p => p.Phrase);
                var phrasesCompanyTemplate = _context.PhraseCompanys
                    .Include(p => p.Phrase)
                    .Where(p => phraseIds.Contains(p.Phrase.PhraseId) && p.CompanyId == companyId && p.Phrase.IsTemplate == true);
                _context.RemoveRange(phrasesCompanyTemplate);//--remove connections to template phrases in library
                _context.RemoveRange(phrasesCompany);//--remove connections to own phrases in library
                _context.RemoveRange(phrases);//--remove own phrases
                await _context.SaveChangesAsync();
                // _log.Info("User/PhraseLib DELETE finished");
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("CompanyPhrase")]
        [SwaggerOperation(Summary = "Return attached to company(-ies) phrases", Description = "Return own and template phrases collection for companies sended in params or for loggined company")]
        public IActionResult CompanyPhraseGet(
                [FromQuery(Name = "companyId"), SwaggerParameter("list guids, if not passed - takes from token")] List<Guid> companyIds,
                [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                // _log.Info("User/CompanyPhrase GET started");
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                companyIds = !companyIds.Any() ? new List<Guid> { Guid.Parse(userClaims["companyId"]) } : companyIds;

                var companyPhrase = _context.PhraseCompanys.Include(p => p.Phrase).Where(p => companyIds.Contains((Guid)p.CompanyId));
                // _log.Info("User/CompanyPhrase GET finished");
                return Ok(companyPhrase.Select(p => p.Phrase).ToList());
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }

        [HttpPost("CompanyPhrase")]
        [SwaggerOperation(Summary = "Attach library(template) phrases to company", Description = "Attach phrases  from library (ids sended in body) to loggined company  (create new PhraseCompany entities)")]
        public async Task<IActionResult> CompanyPhrasePost(
                [FromBody, SwaggerParameter("array ids", Required = true)] List<Guid> phraseIds,
                [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                // _log.Info("User/CompanyPhrase POST started");
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);
                foreach (var phraseId in phraseIds)
                {
                    var phraseCompany = new PhraseCompany
                    {
                        PhraseCompanyId = Guid.NewGuid(),
                        CompanyId = companyId,
                        PhraseId = phraseId
                    };
                    await _context.AddAsync(phraseCompany);
                }
                await _context.SaveChangesAsync();
                // _log.Info("User/CompanyPhrase POST finished");
                return Ok("OK");
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }

        // to do: add dialogue phrase and add make migration 
        // format of datetime is yyyymmddhhmmss
        [HttpGet("Dialogue")]
        [SwaggerOperation(Description = "Return collection of dialogues from dialogue phrases by filters")]
        public IActionResult DialogueGet([FromQuery(Name = "begTime")] string beg,
                                                [FromQuery(Name = "endTime")] string end,
                                                [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                                [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                                [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds,
                                                [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                // _log.Info("User/Dialogue GET started");
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);

                var dialogues = _context.Dialogues
                .Include(p => p.DialoguePhrase)
                .Include(p => p.ApplicationUser)
                .Include(p => p.DialogueHint)
                .Include(p => p.DialogueClientProfile)
                .Where(p =>
                    p.BegTime >= begTime &&
                    p.EndTime <= endTime &&
                    p.StatusId == activeStatus &&
                    (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId)) &&
                    (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId)) &&
                    (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)) &&
                    (!phraseIds.Any() || p.DialoguePhrase.Any(q => phraseIds.Contains((Guid)q.PhraseId))) &&
                    (!phraseTypeIds.Any() || p.DialoguePhrase.Any(q => phraseTypeIds.Contains((Guid)q.PhraseTypeId)))
                )
                .Select(p => new
                {
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
                    InStatistic = p.InStatistic,
                    MeetingExpectationsTotal = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal
                }).ToList();
                // _log.Info("User/Dialogue GET finished");
                return Ok(dialogues);
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e.Message);
            }
        }

        [HttpGet("DialogueInclude")]
        [SwaggerOperation(Description = "Return dialogue with relative data by filters")]
        public IActionResult DialogueGetInclude(
                    [FromQuery(Name = "dialogueId")] Guid dialogueId,
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                // _log.Info("Function DialogueInclude started");
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");

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
                    .Where(p => p.StatusId == 3 && p.DialogueId == dialogueId)
                    .FirstOrDefault();

                if (dialogue == null) return BadRequest("No such dialogue or user does not have permission for dialogue");

                var begTime = DateTime.UtcNow.AddDays(-30);
                var companyId = dialogue.ApplicationUser.CompanyId;
                var avgDialogueTime = 0.0;

                avgDialogueTime = _context.Dialogues.Where(p =>
                        p.BegTime >= begTime &&
                        p.StatusId == activeStatus &&
                        p.ApplicationUser.CompanyId == companyId)
                    .Average(p => p.EndTime.Subtract(p.BegTime).Minutes);

                var jsonDialogue = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(dialogue));

                jsonDialogue["FullName"] = dialogue.ApplicationUser.FullName;
                jsonDialogue["Avatar"] = (dialogue.DialogueClientProfile.FirstOrDefault() == null) ? null : _sftpClient.GetFileUrlFast($"clientavatars/{dialogue.DialogueClientProfile.FirstOrDefault().Avatar}");
                jsonDialogue["Video"] = dialogue == null ? null : _sftpClient.GetFileUrlFast($"dialoguevideos/{dialogue.DialogueId}.mkv");
                jsonDialogue["DialogueAvgDurationLastMonth"] = avgDialogueTime;

                // _log.Info("Function DialogueInclude finished");
                return Ok(jsonDialogue);
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred while executing DialogueInclude {e}");
                return BadRequest(e.Message);
            }
        }

        [HttpPut("Dialogue")]
        [SwaggerOperation(Summary = "Change InStatistic", Description = "Change InStatistic(true/false) of dialogue")]
        public IActionResult DialoguePut(
                [FromBody] DialoguePut message,
                [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                // _log.Info("Function DialoguePut started");
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);
                var dialogue = _context.Dialogues.FirstOrDefault(p => p.DialogueId == message.DialogueId);
                dialogue.InStatistic = message.InStatistic;
                _context.SaveChanges();
                // _log.Info("Function DialoguePut finished");
                return Ok(message.InStatistic);
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred while executing DialoguePut {e}");
                return BadRequest(e.Message);
            }
        }
    }

    public class PostUser
    {
        public string FullName;
        public string Email;
        public string EmployeeId;
        public string RoleId;
        public string Password;
        public Guid? WorkerTypeId;
        public string CompanyId;
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
        public ApplicationRole Role;
        public UserModel(ApplicationUser user, string avatar = null)
        {
            Id = user.Id;
            FullName = user.FullName;
            Email = user.Email;
            Avatar = avatar;
            EmployeeId = user.EmpoyeeId;
            CreationDate = user.CreationDate.ToLongDateString();
            CompanyId = user.CompanyId.ToString();
            StatusId = user.StatusId;
            OneSignalId = user.OneSignalId;
            WorkerTypeId = user.WorkerTypeId;
            Role = user.UserRoles.FirstOrDefault()?.Role;
        }
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

    public class DialoguePut
    {
        public Guid DialogueId;
        public bool InStatistic;
    }
}