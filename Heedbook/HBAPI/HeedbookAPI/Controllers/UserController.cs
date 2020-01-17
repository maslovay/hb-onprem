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
using UserOperations.Providers;
using Microsoft.AspNetCore.Authorization;
using UserOperations.Utils.CommonOperations;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]
    public class UserController : Controller
    {
        private readonly IConfiguration _config;
        private readonly LoginService _loginService;
        private readonly RecordsContext _context;
        private readonly RequestFilters _requestFilters;
        private readonly SftpClient _sftpClient;
        private readonly FileRefUtils _fileRef;
        private readonly SmtpSettings _smtpSetting;
        private readonly SmtpClient _smtpClient;
        private readonly MailSender _mailSender;
        private readonly UserProvider _userProvider;
        private readonly PhraseProvider _phraseProvider;
        private Dictionary<string, string> userClaims;
        private readonly string _containerName;
        private readonly int activeStatus;
        //private readonly int disabledStatus;

        public UserController(
            IConfiguration config,
            LoginService loginService,
            RecordsContext context,
            SftpClient sftpClient,
            FileRefUtils fileRef,
            RequestFilters requestFilters,
            SmtpSettings smtpSetting,
            SmtpClient smtpClient,
            MailSender mailSender,
            UserProvider userProvider,
            PhraseProvider phraseProvider
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _sftpClient = sftpClient;
            _fileRef = fileRef;
            _requestFilters = requestFilters;
            _mailSender = mailSender;
            _containerName = "useravatars";
            activeStatus = 3;
            //disabledStatus = 4;
            _smtpSetting = smtpSetting;
            _smtpClient = smtpClient;
            _userProvider = userProvider;
            _phraseProvider = phraseProvider;
        }

        [HttpGet("User")]
        [SwaggerOperation(Summary = "All company users", Description = "Return all active (status 3) users (array) for loggined company with role Id")]
        [SwaggerResponse(200, "Users with role", typeof(List<UserModel>))]
        public async Task<IActionResult> UserGet()
        {
                List<ApplicationUser> users = null;
                var deviceId = _loginService.GetCurrentDeviceId();
                var companyIdInToken = _loginService.GetCurrentCompanyId();

                if (deviceId != null)
                    users = await _userProvider.GetUsersForDeviceAsync(companyIdInToken);

                else
                {
                    var roleInToken = _loginService.GetCurrentRoleName();
                    var corporationIdInToken = _loginService.GetCurrentCorporationId();
                    var userIdInToken = _loginService.GetCurrentUserId();

                    if (roleInToken == "Admin")
                        users = await _userProvider.GetUsersForAdminAsync();

                    if (roleInToken == "Supervisor")
                        users = await _userProvider.GetUsersForSupervisorAsync((Guid)corporationIdInToken, (Guid)userIdInToken);

                    if (roleInToken == "Manager")
                        users = await _userProvider.GetUsersForManagerAsync(companyIdInToken, userIdInToken);
                }


            var result = users?.Select(p => new UserModel(p, p.Avatar != null ? _fileRef.GetFileLink(_containerName, p.Avatar, default) : null));
                return Ok(result);
        }

        [HttpPost("User")]
        [SwaggerOperation(Description = "Create new user with role Employee in loggined company (taked from token) and can save avatar for user / Return new user")]
        [SwaggerResponse(200, "User", typeof(UserModel))]
        public async Task<IActionResult> UserPostAsync(
                    [FromForm, 
                    SwaggerParameter("json user (include password and unique email) in FormData with key 'data' + file avatar (not required)")]
                    IFormCollection formData )
        {
                var roleInToken = _loginService.GetCurrentRoleName();
                var companyIdInToken = _loginService.GetCurrentCompanyId();
                var corporationIdInToken = _loginService.GetCurrentCorporationId();

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
                    avatarUrl = _fileRef.GetFileLink(_containerName, fn, default);
                }
                var userForEmail = await _userProvider.GetUserWithRoleAndCompanyByIdAsync(user.Id);
                try
                {
                    await _mailSender.SendUserRegisterEmail(userForEmail, message.Password);
                }
                catch { }
                return Ok(new UserModel(user, avatarUrl, role));
        }

        [HttpPut("User")]
        [SwaggerOperation(Summary = "Edit user",
                Description = "Edit user (any from loggined company) and return edited. Don't send password and role (can't change). Email must been unique. May contain avatar file")]
        [SwaggerResponse(200, "Edited user", typeof(UserModel))]
        public async Task<IActionResult> UserPut(
                    [FromForm, SwaggerParameter("Avatar file (not required) + json User with key 'data' in FormData")] IFormCollection formData)
        {
                var roleInToken = _loginService.GetCurrentRoleName();
                var companyIdInToken = _loginService.GetCurrentCompanyId();
                var corporationIdInToken = _loginService.GetCurrentCorporationId();
                var userIdInToken = _loginService.GetCurrentUserId();

                var userDataJson = formData.FirstOrDefault(x => x.Key == "data").Value.ToString();
                UserModel message = JsonConvert.DeserializeObject<UserModel>(userDataJson);
                var user = await _userProvider.GetUserWithRoleAndCompanyByIdAsync(message.Id);
                if (user == null) return BadRequest("No such user");

                _requestFilters.IsCompanyBelongToUser(corporationIdInToken, companyIdInToken, user?.CompanyId, roleInToken);

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
                    avatarUrl = _fileRef.GetFileLink(_containerName, user.Avatar, default);
                }
                _context.SaveChanges();
                return Ok(new UserModel(user, avatarUrl, newRole));
        }


        [HttpDelete("User")]
        [SwaggerOperation(Summary = "Delete or make disabled", Description = "Delete user by Id if he hasn't any relations in DB or make status Disabled")]
        public async Task<IActionResult> UserDelete( [FromQuery] Guid applicationUserId)
        {
                var roleInToken = _loginService.GetCurrentRoleName();
                var companyIdInToken = _loginService.GetCurrentCompanyId();
                var corporationIdInToken = _loginService.GetCurrentCorporationId();
                var userIdInToken = _loginService.GetCurrentUserId();

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

        [HttpGet("Companies")]
        [SwaggerOperation(Summary = "All corporations companies", Description = "Return all companies for loggined corporation (only for role Supervisor)")]
        [SwaggerResponse(200, "Companies", typeof(List<Company>))]
        public async Task<IActionResult> CompaniesGet()
        {
                var roleInToken = _loginService.GetCurrentRoleName();
                if (roleInToken != "Admin" && roleInToken != "Supervisor")
                    return BadRequest("Not allowed access(role)");

                IEnumerable<Company> companies = null;
                if (roleInToken == "Admin")
                    companies = await _userProvider.GetCompaniesForAdminAsync();
                if (roleInToken == "Supervisor") // only for corporations
                    companies = await _userProvider.GetCompaniesForSupervisorAsync(userClaims["corporationId"]);

                return Ok(companies?? new List<Company>());
        }

        [HttpPost("Company")]
        [SwaggerOperation(Description = "Create new company for corporation")]
        [SwaggerResponse(200, "Company", typeof(Company))]
        public async Task<IActionResult> CompanysPostAsync( [FromBody] Company model)
        {
                var roleInToken = _loginService.GetCurrentRoleName();
                var companyIdInToken = _loginService.GetCurrentCompanyId();

                if (roleInToken != "Admin" && roleInToken != "Supervisor")
                    return BadRequest("Not allowed access(role)");
              

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

        [HttpPut("Company")]
        [SwaggerOperation(Summary = "Edit company", Description = "Edit company")]
        [SwaggerResponse(200, "Edited company", typeof(Company))]
        public async Task<IActionResult> CompanyPut( [FromBody] Company model)
        {
            var roleInToken = _loginService.GetCurrentRoleName();
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            var corporationIdInToken = _loginService.GetCurrentCorporationId();

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
        public async Task<IActionResult> CorporationsGet()
        {
                var roleInToken = _loginService.GetCurrentRoleName();
                if (roleInToken != "Admin") return BadRequest("Not allowed access(role)");

                var corporations = await _userProvider.GetCorporationsForAdminAsync();
                return Ok(corporations);
        }



        [HttpGet("PhraseLib")]
        [SwaggerOperation(Summary = "Library",
                Description = "Return collections phrases from library (only templates and only with language code = loggined company language code) which company has not yet used")]
        [SwaggerResponse(200, "Library phrase collection", typeof(List<Phrase>))]
        public async Task<IActionResult> PhraseGet()
        {
                var companyIdInToken = _loginService.GetCurrentCompanyId();
                var languageId = _loginService.GetCurrentLanguagueId();

                var phraseIds = await _phraseProvider.GetPhraseIdsByCompanyIdAsync(companyIdInToken, languageId, true);
                var phrases = await _phraseProvider.GetPhrasesNotBelongToCompanyByIdsAsync(phraseIds, languageId, true);
                return Ok(phrases);
        }

        [HttpPost("PhraseLib")]
        [SwaggerOperation(Summary = "Create company phrase (not library!)",
            Description = "Save new phrase to DB and attach it to loggined company (create new PhraseCompany). Assigned that the phrase is not template")]
        [SwaggerResponse(200, "New phrase", typeof(Phrase))]
        public async Task<IActionResult> PhrasePost(
                    [FromBody] PhrasePost message)
        {
            var companyIdInToken = _loginService.GetCurrentCompanyId();
            var languageId = _loginService.GetCurrentLanguagueId();

            var phrase = await _phraseProvider.GetLibraryPhraseByTextAsync(message.PhraseText, true);
            phrase = phrase?? await _phraseProvider.CreateNewPhraseAsync(message, languageId);

            await _phraseProvider.CreateNewPhraseCompanyAsync(companyIdInToken, phrase.PhraseId);
            await _phraseProvider.SaveChangesAsync();
            return Ok(phrase);
        }

        [HttpPut("PhraseLib")]
        [SwaggerOperation(Summary = "Edit company phrase", Description = "Edit phrase. You can edit only your own phrase (not template from library)")]
        public async Task<IActionResult> PhrasePut(
                    [FromBody] Phrase message)
        {
            var companyIdInToken = _loginService.GetCurrentCompanyId();

            var phrase = await _phraseProvider.GetPhraseInCompanyByIdAsync(message.PhraseId, companyIdInToken, false);
            if (phrase != null)
            {
                await _phraseProvider.EditAndSavePhraseAsync(phrase, message);
                return Ok(phrase);
            }
            return BadRequest("No permission for changing phrase");
        }

        [HttpDelete("PhraseLib")]
        [SwaggerOperation(Summary = "Delete company phrase", Description = "Delete phrase (if this phrase are Template it can't be deleted, it only delete connection to company")]
        public async Task<IActionResult> PhraseDelete(
                    [FromQuery(Name = "phraseId"), SwaggerParameter("phraseId Guid", Required = true)] Guid phraseId)
        {
                var companyIdInToken = _loginService.GetCurrentCompanyId();

                var phrase = await _phraseProvider.GetPhraseByIdAsync(phraseId);
                if (phrase == null) return BadRequest("No such phrase");
                var answer = await _phraseProvider.DeleteAndSavePhraseWithPhraseCompanyAsync(phrase, companyIdInToken);
                return Ok(answer);
        }

        [HttpGet("CompanyPhrase")]
        [SwaggerOperation(Summary = "Return attached to company(-ies) phrases", Description = "Return own and template phrases collection for companies sended in params or for loggined company")]
        public async Task<IActionResult> CompanyPhraseGet(
                [FromQuery(Name = "companyId"), SwaggerParameter("list guids, if not passed - takes from token")] List<Guid> companyIds)
        {
                var companyIdInToken = _loginService.GetCurrentCompanyId();
                companyIds = !companyIds.Any() ? new List<Guid> { companyIdInToken } : companyIds;
                var companyPhrase = await _phraseProvider.GetPhrasesInCompanyByIdsAsync(companyIds);
                return Ok(companyPhrase);
        }

        [HttpPost("CompanyPhrase")]
        [SwaggerOperation(Summary = "Attach library(template) phrases to company", Description = "Attach phrases  from library (ids sended in body) to loggined company  (create new PhraseCompany entities)")]
        public async Task<IActionResult> CompanyPhrasePost(
                [FromBody, SwaggerParameter("array ids", Required = true)] List<Guid> phraseIds)
        {
                var companyIdInToken = _loginService.GetCurrentCompanyId();
                foreach (var phraseId in phraseIds)
                {
                    await _phraseProvider.CreateNewPhraseCompanyAsync(companyIdInToken, phraseId);
                }
                await _phraseProvider.SaveChangesAsync();
                return Ok("OK");
        }



        // to do: add dialogue phrase and add make migration 
        // format of datetime is yyyymmddhhmmss
        [HttpGet("Dialogue")]
        [SwaggerOperation(Description = "Return collection of dialogues from dialogue phrases by filters")]
        public IActionResult DialogueGet([FromQuery(Name = "begTime")] string beg,
                                                [FromQuery(Name = "endTime")] string end,
                                                [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds,
                                                [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                                [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds,
                                                [FromQuery(Name = "inStatistic")] bool? inStatistic)
        {
                var roleInToken = _loginService.GetCurrentRoleName();
                var companyIdInToken = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, roleInToken, companyIdInToken);
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
                    (!applicationUserIds.Any() || (p.ApplicationUserId != null && applicationUserIds.Contains(p.ApplicationUserId))) &&
                    (!deviceIds.Any() || (p.DeviceId != null && deviceIds.Contains(p.DeviceId))) &&
                    (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId)) &&
                    (!phraseIds.Any() || p.DialoguePhrase.Any(q => phraseIds.Contains((Guid)q.PhraseId))) &&
                    (!phraseTypeIds.Any() || p.DialoguePhrase.Any(q => phraseTypeIds.Contains((Guid)q.PhraseTypeId)))
                )
                .Select(p => new
                {
                    p.DialogueId,
                    Avatar = (p.DialogueClientProfile.FirstOrDefault() == null) ? null : _fileRef.GetFileUrlFast($"clientavatars/{p.DialogueClientProfile.FirstOrDefault().Avatar}"),
                    ApplicationUserId = p.ApplicationUserId?? null,
                    FullName =  p.ApplicationUser != null? p.ApplicationUser.FullName:null,
                    DialogueHints = p.DialogueHint,
                    p.BegTime,
                    p.EndTime,
                    duration = p.EndTime.Subtract(p.BegTime),
                    p.StatusId,
                    p.InStatistic,
                    p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal
                }).ToList();
                return Ok(dialogues);
        }

        [HttpGet("DialoguePaginated")]
        [SwaggerOperation(Description =
            "Return collection of dialogues from dialogue phrases by filters (one page). limit=10, page=0, orderBy=BegTime/EndTime/FullName/Duration/MeetingExpectationsTotal, orderDirection=desc/asc")]
        public IActionResult DialoguePaginatedGet([FromQuery(Name = "begTime")] string beg,
                                           [FromQuery(Name = "endTime")] string end,
                                           [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                           [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds,
                                           [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                           [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                           [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                           [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds,
                                           [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                           
                                           [FromQuery(Name = "inStatistic")] bool? inStatistic,
                                           [FromQuery(Name = "limit")] int limit = 10,
                                           [FromQuery(Name = "page")] int page = 0,
                                           [FromQuery(Name = "orderBy")] string orderBy = "BegTime",
                                           [FromQuery(Name = "orderDirection")] string orderDirection = "desc")
        {
                var roleInToken = _loginService.GetCurrentRoleName();
                var companyIdInToken = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, roleInToken, companyIdInToken);
                inStatistic = inStatistic ?? true;

                var dialogues = _context.Dialogues
                .Include(p => p.DialogueHint)
                .Where(p =>
                    p.BegTime >= begTime &&
                    p.EndTime <= endTime &&
                    p.StatusId == activeStatus &&
                    p.InStatistic == inStatistic &&
                    (!applicationUserIds.Any() || (p.ApplicationUserId != null && applicationUserIds.Contains(p.ApplicationUserId))) &&
                    (!deviceIds.Any() || (p.DeviceId != null && deviceIds.Contains(p.DeviceId))) &&
                    (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId)) &&
                    (!phraseIds.Any() || p.DialoguePhrase.Any(q => phraseIds.Contains((Guid)q.PhraseId))) &&
                    (!phraseTypeIds.Any() || p.DialoguePhrase.Any(q => phraseTypeIds.Contains((Guid)q.PhraseTypeId)))
                )
                .Select(p => new
                {
                    p.DialogueId,
                    Avatar = (p.DialogueClientProfile.FirstOrDefault() == null) ? null : _fileRef.GetFileUrlFast($"clientavatars/{p.DialogueClientProfile.FirstOrDefault().Avatar}"),
                    ApplicationUserId = p.ApplicationUserId,
                    FullName = p.ApplicationUser != null ? p.ApplicationUser.FullName : null,
                    DialogueHints = p.DialogueHint.Count() != 0 ? "YES" : null,
                    p.BegTime,
                    p.EndTime,
                    Duration = p.EndTime.Subtract(p.BegTime),
                    p.StatusId,
                    p.InStatistic,
                    p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal
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
        }

        [HttpGet("DialogueInclude")]
        [SwaggerOperation(Description = "Return dialogue with relative data by filters")]
        public IActionResult DialogueGetInclude(
                    [FromQuery(Name = "dialogueId")] Guid dialogueId)
        {
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
                    .Include(p => p.Device)
                    .Where(p => p.StatusId == 3 && p.DialogueId == dialogueId)
                    .FirstOrDefault();

                if (dialogue == null) return BadRequest("No such dialogue or user does not have permission for dialogue");

                var begTime = DateTime.UtcNow.AddDays(-30);
                var companyId = dialogue.Device.CompanyId;
                var avgDialogueTime = 0.0;

                avgDialogueTime = _context.Dialogues.Where(p =>
                        p.BegTime >= begTime &&
                        p.StatusId == activeStatus &&
                        p.Device.CompanyId == companyId)
                    .Average(p => p.EndTime.Subtract(p.BegTime).Minutes);

                var jsonDialogue = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(dialogue));

                jsonDialogue["DeviceName"] = dialogue.Device.Name;
                jsonDialogue["FullName"] = dialogue.ApplicationUser?.FullName;
                jsonDialogue["Avatar"] = (dialogue.DialogueClientProfile.FirstOrDefault() == null) ? null : _fileRef.GetFileUrlFast($"clientavatars/{dialogue.DialogueClientProfile.FirstOrDefault().Avatar}");
                jsonDialogue["Video"] = dialogue == null ? null : _fileRef.GetFileUrlFast($"dialoguevideos/{dialogue.DialogueId}.mkv");
                jsonDialogue["DialogueAvgDurationLastMonth"] = avgDialogueTime;

                return Ok(jsonDialogue);
        }

        [HttpPut("Dialogue")]
        [SwaggerOperation(Summary = "Change InStatistic", Description = "Change InStatistic(true/false) of dialogue/dialogues")]
        public IActionResult DialoguePut(
                [FromBody] DialoguePut message)
        {
                var companyIdInToken = _loginService.GetCurrentCompanyId();
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
                return Ok(message.InStatistic);
        }


        [HttpPut("DialogueSatisfaction")]
        [SwaggerOperation(Summary = "Change Satisfaction ", Description = "Change Satisfaction (true/false) of dialogue")]
        public IActionResult DialogueSatisfactionPut(
            [FromBody] DialogueSatisfactionPut message)
        {
                var roleInToken = _loginService.GetCurrentRoleName();
                var companyIdInToken = _loginService.GetCurrentCompanyId();
                var corporationIdInToken = _loginService.GetCurrentCorporationId();
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

        [HttpGet("Alert")]
        [SwaggerOperation(Summary = "all alerts for period", Description = "Return all alerts for period, type, employee, worker type, time")]
        public IActionResult AlertGet([FromQuery(Name = "begTime")] string beg,
                                                            [FromQuery(Name = "endTime")] string end,
                                                            [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                            [FromQuery(Name = "alertTypeId[]")] List<Guid> alertTypeIds,
                                                            [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds)
        {
            var companyIdInToken = _loginService.GetCurrentCompanyId();

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
                        p.DialogueId,
                        p.ApplicationUserId,
                        p.BegTime,
                        p.EndTime,
                    }).ToList();

            var alerts = _context.Alerts
              .Include(p => p.ApplicationUser)
                        .Where(p => p.CreationDate >= begTime
                                && p.CreationDate <= endTime
                                && p.Device.CompanyId == companyIdInToken
                                && (!alertTypeIds.Any() || alertTypeIds.Contains(p.AlertTypeId))
                                && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                                && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId)))
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
        public IActionResult SendVideo(
                    [FromForm, SwaggerParameter("json message with key 'data' in FormData")] IFormCollection formData)
        {
                var roleInToken = _loginService.GetCurrentRoleName();
                var companyIdInToken = _loginService.GetCurrentCompanyId();
                var corporationIdInToken = _loginService.GetCurrentCorporationId();
                var userIdInToken = _loginService.GetCurrentUserId();

                var userDataJson = formData.FirstOrDefault(x => x.Key == "data").Value.ToString();
                VideoMessage message = JsonConvert.DeserializeObject<VideoMessage>(userDataJson);

                var user = _context.ApplicationUsers.First(p => p.Id == userIdInToken);

                List<ApplicationUser> recepients = null;
                if(roleInToken == "Employee")
                {
                    var managerRole = _context.Roles.First(p => p.Name == "Manager");
                    recepients = _context.ApplicationUsers.Where(p => p.CompanyId == companyIdInToken
                            && p.UserRoles.Where(r => r.Role == managerRole).Any())
                        .Distinct()
                        .ToList();
                }
                else if(roleInToken == "Manager" && corporationIdInToken != null)
                {
                    var supervisorRole = _context.Roles.First(p => p.Name == "Supervisor");
                    recepients = _context.ApplicationUsers
                        .Include(p => p.Company)
                        .Where(p => p.Company.CorporationId == corporationIdInToken
                            && p.UserRoles.Where(r => r.Role == supervisorRole).Any())
                        .Distinct()
                        .ToList();
                }
                else
                    return BadRequest($"{roleInToken} not have leader");

                if (formData.Files.Count != 0 && recepients != null && recepients.Count != 0)
                {
                    var mail = new System.Net.Mail.MailMessage
                    {
                        From = new System.Net.Mail.MailAddress(_smtpSetting.FromEmail),
                        Subject = user.FullName + " - " + message.Subject,
                        Body = message.Body,
                        IsBodyHtml = false
                    };

                    foreach (var r in recepients)
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
    }
}