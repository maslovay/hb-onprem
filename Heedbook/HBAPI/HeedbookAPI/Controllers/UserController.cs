using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using HBData;
using HBData.Models;
using HBLib;
using HBLib.Utils;
using UserOperations.Services;
using UserOperations.Utils;
using System.Reflection;
using UserOperations.Models;
using System.Transactions;
using UserOperations.Providers;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly IRequestFilters _requestFilters;
        private readonly SftpClient _sftpClient;
        private readonly SmtpSettings _smtpSetting;
        private readonly SmtpClient _smtpClient;
        private readonly IMailSender _mailSender;
        private readonly IUserProvider _userProvider;
        private readonly IPhraseProvider _phraseProvider;
        private Dictionary<string, string> userClaims;
        private readonly string _containerName;
        private readonly int activeStatus;
        private readonly int disabledStatus;

        public UserController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            SftpClient sftpClient,
            IRequestFilters requestFilters,
            SmtpSettings smtpSetting,
            SmtpClient smtpClient,
            IMailSender mailSender,
            IUserProvider userProvider,
            IPhraseProvider phraseProvider
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _sftpClient = sftpClient;
            _requestFilters = requestFilters;
            _mailSender = mailSender;
            _containerName = "useravatars";
            activeStatus = 3;
            disabledStatus = 4;
            _smtpSetting = smtpSetting;
            _smtpClient = smtpClient;
            _userProvider = userProvider;
            _phraseProvider = phraseProvider;
        }

        [HttpGet("User")]
        [SwaggerOperation(Summary = "All company users", Description = "Return all active (status 3) users (array) for loggined company with role Id")]
        [SwaggerResponse(200, "Users with role", typeof(List<UserModel>))]
        public async Task<IActionResult> UserGet(
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                List<ApplicationUser> users = null;

                Guid.TryParse(userClaims["companyId"], out var companyIdInToken);
                Guid.TryParse(userClaims["corporationId"], out var corporationIdInToken);
                Guid.TryParse(userClaims["applicationUserId"], out var userIdInToken);
                var roleInToken = userClaims["role"];

                if (roleInToken == "Admin")
                    users = await _userProvider.GetUsersForAdminAsync();
              
                if (roleInToken == "Supervisor" )
                    users = await _userProvider.GetUsersForSupervisorAsync(corporationIdInToken, userIdInToken);

                if (roleInToken == "Manager")
                    users = await _userProvider.GetUsersForManagerAsync(companyIdInToken, userIdInToken);

                var result = users?.Select(p => new UserModel(p, p.Avatar != null ? _sftpClient.GetFileLink(_containerName, p.Avatar, default).path : null));
                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("User")]
        [SwaggerOperation(Description = "Create new user with role Employee in loggined company (taked from token) and can save avatar for user / Return new user")]
        [SwaggerResponse(200, "User", typeof(UserModel))]
        public async Task<IActionResult> UserPostAsync(
                    [FromForm, SwaggerParameter("json user (include password and unique email) in FormData with key 'data' + file avatar (not required)")] IFormCollection formData,
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");

                Guid.TryParse( userClaims["companyId"], out var companyIdInToken );
                Guid.TryParse(userClaims["corporationId"], out var corporationIdInToken);

                var roleInToken = userClaims["role"];
                var userDataJson = formData.FirstOrDefault(x => x.Key == "data").Value.ToString();

                PostUser message = JsonConvert.DeserializeObject<PostUser>(userDataJson);
                if (!await _userProvider.CheckUniqueEmailAsync(message.Email))
                    return BadRequest("User email not unique");
               
                message.CompanyId = message.CompanyId ?? companyIdInToken;
                if (_requestFilters.IsCompanyBelongToUser(corporationIdInToken, companyIdInToken, message.CompanyId, roleInToken) == false)
                    return BadRequest($"Not allowed user company");

                if (await _userProvider.CheckAbilityToCreateOrChangeUserAsync(roleInToken, message.RoleId, null) == false)
                    return BadRequest("Not allowed user role");

                ApplicationUser user =  await _userProvider.AddNewUserAsync(message);
                ApplicationRole role =  await _userProvider.AddOrChangeUserRolesAsync (user.Id, message.RoleId, null);

                //---save avatar---
                string avatarUrl = null;
                if (formData.Files.Count != 0)
                {
                    FileInfo fileInfo = new FileInfo(formData.Files[0].FileName);
                    var fn = user.Id + fileInfo.Extension;
                    user.Avatar = fn;
                    avatarUrl = _sftpClient.GetFileLink(_containerName, fn, default).path;
                }
                var userForEmail = await _userProvider.GetUserWithRoleAndCompanyByIdAsync(user.Id);
                try
                {
                    await _mailSender.SendUserRegisterEmail(userForEmail, message.Password);
                }
                catch { }
                return Ok(new UserModel(user, avatarUrl, role));
            }
            catch (Exception e)
            {
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
                if (!_loginService.GetDataFromToken(Authorization, out userClaims)) return BadRequest("Token wrong");

                var userDataJson = formData.FirstOrDefault(x => x.Key == "data").Value.ToString();
                UserModel message = JsonConvert.DeserializeObject<UserModel>(userDataJson);
                var user = await _userProvider.GetUserWithRoleAndCompanyByIdAsync(message.Id);
                if (user == null) return BadRequest("No such user");

                Guid.TryParse(userClaims["companyId"], out var companyIdInToken);
                Guid.TryParse(userClaims["corporationId"], out var corporationIdInToken);
                var roleInToken = userClaims["role"];
                var employeeId = Guid.Parse(userClaims["applicationUserId"]);

                if (_requestFilters.IsCompanyBelongToUser(corporationIdInToken, companyIdInToken, user.CompanyId, roleInToken) == false)
                    return BadRequest($"Not allowed user company");

                if (await _userProvider.CheckAbilityToCreateOrChangeUserAsync(roleInToken, message.RoleId, user.UserRoles.FirstOrDefault().RoleId) == false)
                    return BadRequest("Not allowed user role");

                if (message.Email != null && user.Email != message.Email)
                    return BadRequest("Not allowed change email");

                ApplicationRole newRole = null;
                Type userType = user.GetType();
                foreach (var p in typeof(UserModel).GetProperties())
                {
                    var val = p.GetValue(message, null);
                    if (val != null && val.ToString() != Guid.Empty.ToString() && p.Name != "Role")
                    {
                        if (p.Name == "EmployeeId")//---its a mistake in DB
                            userType.GetProperty("EmpoyeeId").SetValue(user, val);
                        else if (p.Name == "RoleId")
                            newRole =  await _userProvider.AddOrChangeUserRolesAsync(user.Id, message.RoleId, user.UserRoles.FirstOrDefault().RoleId);
                        else
                            userType.GetProperty(p.Name).SetValue(user, val);
                    }
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
                }
                if (user.Avatar != null)
                {
                    avatarUrl = _sftpClient.GetFileLink(_containerName, user.Avatar, default(DateTime)).path;
                }
                _context.SaveChanges();
                return Ok(new UserModel(user, avatarUrl, newRole));
            }
            catch (Exception e)
            {
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
                if (!_loginService.GetDataFromToken(Authorization, out userClaims)) return BadRequest("Token wrong");

                Guid.TryParse(userClaims["companyId"], out var companyIdInToken);
                Guid.TryParse(userClaims["corporationId"], out var corporationIdInToken);
                var roleInToken = userClaims["role"];

                ApplicationUser user = await _userProvider.GetUserWithRoleAndCompanyByIdAsync(applicationUserId);
                if (user == null) return BadRequest("No such user");

                if (! await _userProvider.CheckAbilityToDeleteUserAsync(roleInToken, user.UserRoles.FirstOrDefault().RoleId))
                    return BadRequest("Not allowed user role");
                if (!_requestFilters.IsCompanyBelongToUser(corporationIdInToken, companyIdInToken, user.CompanyId, roleInToken))
                    return BadRequest($"Not allowed user company");

                await _userProvider.SetUserInactiveAsync(user);
                try
                {
                    await _userProvider.DeleteUserWithRolesAsync(user);
                    await _sftpClient.DeleteFileIfExistsAsync($"{_containerName}/{user.Id}");
                    return Ok("Deleted");
                }
                catch
                {                       
                    return Ok("Disabled Status");
                }
            }
            catch (Exception e)
            {
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
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                if (userClaims["role"] != "Admin" && userClaims["role"] != "Supervisor")
                    return BadRequest("Not allowed access(role)");

                IEnumerable<Company> companies = null;
                if (userClaims["role"] == "Admin")
                    companies = await _userProvider.GetCompaniesForAdminAsync();
                if (userClaims["role"] == "Supervisor") // only for corporations
                    companies = await _userProvider.GetCompaniesForSupervisorAsync(userClaims["corporationId"]);

                return Ok(companies);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost("Company")]
        [SwaggerOperation(Description = "Create new company for corporation")]
        [SwaggerResponse(200, "Company", typeof(Company))]
        public async Task<IActionResult> CompanysPostAsync(
                    [FromBody] Company model,
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                Guid.TryParse(userClaims["companyId"], out var companyIdInToken);
                Guid.TryParse(userClaims["corporationId"], out var corporationIdInToken);
                var roleInToken = userClaims["role"];

                if (roleInToken != "Admin" && roleInToken != "Supervisor")
                    return BadRequest("Not allowed access(role)");

                Company newCompany = null;
                if (roleInToken == "Admin")
                    newCompany = await _userProvider.AddNewCompanyAsync(model, model.CompanyName);
                if (roleInToken == "Supervisor")
                {
                    var supervisorCompany = await _userProvider.GetCompanyByIdAsync(companyIdInToken);
                    newCompany = await _userProvider.AddNewCompanyAsync(supervisorCompany, model.CompanyName);
                }
                return Ok(newCompany);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut("Company")]
        [SwaggerOperation(Summary = "Edit company", Description = "Edit company")]
        [SwaggerResponse(200, "Edited company", typeof(Company))]
        public async Task<IActionResult> CompanyPut(
                    [FromBody] Company model,
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                return BadRequest("Token wrong");
            Guid.TryParse(userClaims["companyId"], out var companyIdInToken);
            Guid.TryParse(userClaims["corporationId"], out var corporationIdInToken);
            var roleInToken = userClaims["role"];

            if (roleInToken != "Admin" && roleInToken != "Supervisor")
                return BadRequest("Not allowed access(role)");

            var company = await _userProvider.GetCompanyByIdAsync(model.CompanyId);
            if (company is null)
                return BadRequest($"company with such companyId: {model.CompanyId} not exist");
            if( ! _requestFilters.IsCompanyBelongToUser(corporationIdInToken, companyIdInToken, model.CompanyId, roleInToken))
                return BadRequest($"Not allowed user company");

            company = await _userProvider.UpdateCompanAsync(company, model);
            return Ok(company);
        }

        [HttpGet("Corporations")]
        [SwaggerOperation(Summary = "All corporations", Description = "Return all corporations for loggined admins (only for role Admin)")]
        [SwaggerResponse(200, "Corporations", typeof(List<Company>))]
        public async Task<IActionResult> CorporationsGet(
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                if (userClaims["role"] != "Admin") return BadRequest("Not allowed access(role)");

                var corporations = await _userProvider.GetCorporationsForAdminAsync();
                return Ok(corporations);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }



        [HttpGet("PhraseLib")]
        [SwaggerOperation(Summary = "Library",
                Description = "Return collections phrases from library (only templates and only with language code = loggined company language code) which company has not yet used")]
        [SwaggerResponse(200, "Library phrase collection", typeof(List<Phrase>))]
        public async Task<IActionResult> PhraseGet(
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                Guid.TryParse(userClaims["companyId"], out var companyIdInToken);
                var languageId = userClaims["languageCode"];

                var phraseIds = await _phraseProvider.GetPhraseIdsByCompanyIdAsync(companyIdInToken, languageId, true);
                var phrases = await _phraseProvider.GetPhrasesNotBelongToCompanyByIdsAsync(phraseIds, languageId, true);
                return Ok(phrases);
            }
            catch (Exception e)
            {
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
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);
                var languageId = Int32.Parse(userClaims["languageCode"]);
                var phrase = await _phraseProvider.GetLibraryPhraseByTextAsync(message.PhraseText, true);

                if (phrase == null)
                    phrase = await _phraseProvider.CreateNewPhraseAsync(message, languageId);

                await _phraseProvider.CreateNewPhraseCompanyAsync(companyId, phrase.PhraseId);
                return Ok(phrase);
            }
            catch (Exception e)
            {
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
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);

                var phrase = await _phraseProvider.GetPhraseInCompanyByIdAsync(message.PhraseId, companyId, false);
                if (phrase != null)
                {
                    await _phraseProvider.EditPhraseAsync(phrase, message);
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
        [SwaggerOperation(Summary = "Delete company phrase", Description = "Delete phrase (if this phrase are Template it can't be deleted, it only delete connection to company")]
        public async Task<IActionResult> PhraseDelete(
                    [FromQuery(Name = "phraseId"), SwaggerParameter("phraseId Guid", Required = true)] Guid phraseId,
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);
                var phrase = await _phraseProvider.GetPhraseByIdAsync(phraseId);
                if (phrase == null) return BadRequest("No such phrase");
                var answer = await _phraseProvider.DeletePhraseWithPhraseCompanyAsync(phrase, companyId);
                return Ok(answer);
            }
            catch (Exception e)
            {
                return BadRequest("Deleted from PhraseCompany. Phrase has relations");
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
                                                [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                                [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds,
                                                [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                [FromQuery(Name = "inStatistic")] bool? inStatistic,
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
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);
                inStatistic = inStatistic ?? true;

                var dialogues = _context.Dialogues
                .Include(p => p.DialoguePhrase)
                .Include(p => p.ApplicationUser)
                .Include(p => p.DialogueHint)
                .Include(p => p.DialogueClientProfile)
                .Where(p =>
                    p.BegTime >= begTime &&
                    p.EndTime <= endTime &&
                    p.StatusId == activeStatus &&
                    p.InStatistic == inStatistic &&
                    (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId)) &&
                    (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId)) &&
                    (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)) &&
                    (!phraseIds.Any() || p.DialoguePhrase.Any(q => phraseIds.Contains((Guid)q.PhraseId))) &&
                    (!phraseTypeIds.Any() || p.DialoguePhrase.Any(q => phraseTypeIds.Contains((Guid)q.PhraseTypeId)))
                )
                .Select(p => new
                {
                    p.DialogueId,
                    Avatar = (p.DialogueClientProfile.FirstOrDefault() == null) ? null : _sftpClient.GetFileUrlFast($"clientavatars/{p.DialogueClientProfile.FirstOrDefault().Avatar}"),
                    p.ApplicationUserId,
                    p.ApplicationUser.FullName,
                    DialogueHints = p.DialogueHint,
                    p.BegTime,
                    p.EndTime,
                    duration = p.EndTime.Subtract(p.BegTime),
                    p.StatusId,
                    p.InStatistic,
                    p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal
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

        [HttpGet("DialoguePaginated")]
        [SwaggerOperation(Description =
            "Return collection of dialogues from dialogue phrases by filters (one page). limit=10, page=0, orderBy=begTime/endTime/fullName/duration/meetingExpectationsTotal, orderDirection=desc/asc")]
        public IActionResult DialoguePaginatedGet([FromQuery(Name = "begTime")] string beg,
                                           [FromQuery(Name = "endTime")] string end,
                                           [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                           [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                           [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                           [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                           [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds,
                                           [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,

                                           [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization,
                                           [FromQuery(Name = "inStatistic")] bool? inStatistic,
                                           [FromQuery(Name = "limit")] int limit = 10,
                                           [FromQuery(Name = "page")] int page = 0,
                                           [FromQuery(Name = "orderBy")] string orderBy = "begTime",
                                           [FromQuery(Name = "orderDirection")] string orderDirection = "desc")
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
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);
                inStatistic = inStatistic ?? true;

                var dialogues = _context.Dialogues
                .Include(p => p.DialogueHint)
                .Where(p =>
                    p.BegTime >= begTime &&
                    p.EndTime <= endTime &&
                    p.StatusId == activeStatus &&
                    p.InStatistic == inStatistic &&
                    (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId)) &&
                    (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId)) &&
                    (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)) &&
                    (!phraseIds.Any() || p.DialoguePhrase.Any(q => phraseIds.Contains((Guid)q.PhraseId))) &&
                    (!phraseTypeIds.Any() || p.DialoguePhrase.Any(q => phraseTypeIds.Contains((Guid)q.PhraseTypeId)))
                )
                .Select(p => new
                {
                    dialogueId = p.DialogueId,
                    avatar = (p.DialogueClientProfile.FirstOrDefault() == null) ? null : _sftpClient.GetFileUrlFast($"clientavatars/{p.DialogueClientProfile.FirstOrDefault().Avatar}"),
                    applicationUserId = p.ApplicationUserId,
                    fullName = p.ApplicationUser.FullName,
                    dialogueHints = p.DialogueHint.Count() != 0 ? "YES" : null,
                    begTime = p.BegTime,
                    endTime = p.EndTime,
                    duration = p.EndTime.Subtract(p.BegTime),
                    statusId = p.StatusId,
                    inStatistic = p.InStatistic,
                    meetingExpectationsTotal = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal
                }).ToList();

                if (dialogues.Count() == 0) return Ok(dialogues);

                ////---PAGINATION---
                var pageCount = (int)Math.Ceiling((double)dialogues.Count() / limit);//---round to the bigger 

                Type dialogueType = dialogues.First().GetType();
                PropertyInfo prop = dialogueType.GetProperty(orderBy);
                if (orderDirection == "asc")
                {
                    var dialoguesList = dialogues.OrderBy(p => prop.GetValue(p)).Skip(page * limit).Take(limit).ToList();
                    return Ok(new { dialoguesList, pageCount, orderBy, limit, page });
                }
                else
                {
                    var dialoguesList = dialogues.OrderByDescending(p => prop.GetValue(p)).Skip(page * limit).Take(limit).ToList();
                    return Ok(new { dialoguesList, pageCount, orderBy, limit, page });
                }
                // _log.Info("User/Dialogue GET finished");
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
        [SwaggerOperation(Summary = "Change InStatistic", Description = "Change InStatistic(true/false) of dialogue/dialogues")]
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
                List<Dialogue> dialogues;
                if (message.DialogueIds != null)
                    dialogues = _context.Dialogues.Where(x => message.DialogueIds.Contains(x.DialogueId)).ToList();
                else
                    dialogues = _context.Dialogues.Where(p => p.DialogueId == message.DialogueId).ToList();
                foreach (var dialogue in dialogues)
                {
                    dialogue.InStatistic = message.InStatistic;
                }
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


        [HttpPut("DialogueSatisfaction")]
        [SwaggerOperation(Summary = "Change Satisfaction ", Description = "Change Satisfaction (true/false) of dialogue")]
        public IActionResult DialogueSatisfactionPut(
            [FromBody] DialogueSatisfactionPut message,
            [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                // _log.Info("Function DialogueSatisfactionPut started");
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");

                Guid.TryParse(userClaims["companyId"], out var companyIdInToken);
                Guid.TryParse(userClaims["corporationId"], out var corporationIdInToken);
                var roleInToken = userClaims["role"];
                if (roleInToken != "Admin" && roleInToken != "Supervisor")
                    return BadRequest("No permission");
                if( !_requestFilters.IsCompanyBelongToUser(corporationIdInToken, companyIdInToken, companyIdInToken, roleInToken))
                    return BadRequest($"Not allowed user company");

                var dialogueClientSatisfaction = _context.DialogueClientSatisfactions.FirstOrDefault(x => x.DialogueId == message.DialogueId);
                if(dialogueClientSatisfaction == null) return BadRequest("No such dialogue");
                dialogueClientSatisfaction.MeetingExpectationsByTeacher = message.Satisfaction;
                dialogueClientSatisfaction.BegMoodByTeacher = message.BegMoodTotal;
                dialogueClientSatisfaction.EndMoodByTeacher = message.EndMoodTotal;
                dialogueClientSatisfaction.Age = message.Age;
                dialogueClientSatisfaction.Gender = message.Gender;
                _context.SaveChanges();
                return Ok(JsonConvert.SerializeObject(dialogueClientSatisfaction));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("Alert")]
        [SwaggerOperation(Summary = "all alerts for period", Description = "Return all alerts for period, type, employee, worker type, time")]
        public IActionResult AlertGet([FromQuery(Name = "begTime")] string beg,
                                                            [FromQuery(Name = "endTime")] string end,
                                                            [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                            [FromQuery(Name = "alertTypeId[]")] List<Guid> alertTypeIds,
                                                            [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                            [FromHeader] string Authorization)
        {
            if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                return BadRequest("Token wrong");
            var companyId = Guid.Parse(userClaims["companyId"]);


            var begTime = _requestFilters.GetBegDate(beg);
            var endTime = _requestFilters.GetEndDate(end);

            var dialogues = _context.Dialogues
                    .Include(p => p.ApplicationUser)
                    .Where(p => p.BegTime >= begTime
                            && p.EndTime <= endTime
                            // && p.StatusId == 3
                            // && p.InStatistic == true
                            // && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            // && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
                            )
                    .Select(p => new
                    {
                        DialogueId = p.DialogueId,
                        ApplicationUserId = p.ApplicationUserId,
                        BegTime = p.BegTime,
                        EndTime = p.EndTime,
                    }).ToList();

            var alerts = _context.Alerts
              .Include(p => p.ApplicationUser)
                        .Where(p => p.CreationDate >= begTime
                                && p.CreationDate <= endTime
                                && p.ApplicationUser.CompanyId == companyId
                                && (!alertTypeIds.Any() || alertTypeIds.Contains(p.AlertTypeId))
                                && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                                && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                        .Select(x => new
                        {
                            x.AlertId,
                            x.AlertTypeId,
                            x.ApplicationUserId,
                            x.CreationDate,
                            dialogueId =
                                    (Guid?)dialogues.FirstOrDefault(p => p.ApplicationUserId == x.ApplicationUserId
                                        && p.BegTime <= x.CreationDate
                                        && p.EndTime >= x.CreationDate).DialogueId
                        })
                        .OrderByDescending(x => x.CreationDate)
                        .ToList();
            return Ok(alerts);
        }
        [HttpPost("VideoMessage")]
        [SwaggerOperation(Summary = "SendVideoMessageToManager",
                Description = "Send video messgae to office manager")]
        public async Task<IActionResult> SendVideo(
                    [FromForm, SwaggerParameter("json message with key 'data' in FormData")] IFormCollection formData,
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                return BadRequest("Token wrong");

                var userDataJson = formData.FirstOrDefault(x => x.Key == "data").Value.ToString();
                VideoMessage message = JsonConvert.DeserializeObject<VideoMessage>(userDataJson);  

                var companyIdFromToken = Guid.Parse(userClaims["companyId"]);
                Guid corporationIdFromToken;
                var corporationIdExist = Guid.TryParse(userClaims["corporationId"], out corporationIdFromToken);
                var roleFromToken = userClaims["role"];
                var empployeeIdFromToken = Guid.Parse(userClaims["applicationUserId"]);
                var user = _context.ApplicationUsers.First(p => p.Id == empployeeIdFromToken);

                List<ApplicationUser> recepients = null;
                if(roleFromToken == "Employee")
                {
                    var managerRole = _context.Roles.First(p => p.Name == "Manager");
                    recepients = _context.ApplicationUsers.Where(p => p.CompanyId == companyIdFromToken
                            && p.UserRoles.Where(r => r.Role == managerRole).Any())
                        .Distinct()
                        .ToList();
                }
                else if(roleFromToken == "Manager" && corporationIdExist)
                {
                    var supervisorRole = _context.Roles.First(p => p.Name == "Supervisor");
                    recepients = _context.ApplicationUsers
                        .Include(p => p.Company)
                        .Where(p => p.Company.CorporationId == corporationIdFromToken
                            && p.UserRoles.Where(r => r.Role == supervisorRole).Any())
                        .Distinct()
                        .ToList();
                }
                else
                    return BadRequest($"{roleFromToken} not have leader");

                if (formData.Files.Count != 0 && recepients != null && recepients.Count != 0)
                {
                    var mail = new System.Net.Mail.MailMessage();
                    mail.From = new System.Net.Mail.MailAddress(_smtpSetting.FromEmail);     
                    mail.Subject =user.FullName + " - " + message.Subject;
                    mail.Body = message.Body;
                    mail.IsBodyHtml = false;

                    foreach(var r in recepients)
                    {
                        mail.To.Add(r.Email);
                    }
                        
                    var amountAttachmentsSize = 0f;
                    foreach(var f in formData.Files)
                    {
                        var fn = user.FullName + "_" + formData.Files[0].FileName;                        
                        var memoryStream = f.OpenReadStream();
                        amountAttachmentsSize += (memoryStream.Length / 1024f) / 1024f;
                        
                        memoryStream.Position = 0;
                        var attachment = new System.Net.Mail.Attachment(memoryStream, fn);
                        mail.Attachments.Add(attachment);
                    }
                    if(amountAttachmentsSize > 25)
                    {
                        return BadRequest($"Files size more than 25 MB");
                    }

                    _smtpClient.Send(mail);                            
                }
                else
                {
                    return BadRequest("Video File is null");
                }
                return Ok();
            }
            catch(Exception ex)
            {
                return BadRequest($"{ex.Message}");
            }
        }
    }
}