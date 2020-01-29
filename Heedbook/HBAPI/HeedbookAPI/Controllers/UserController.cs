using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Annotations;
using HBData.Models;
using UserOperations.Services;
using UserOperations.Models;
using Microsoft.AspNetCore.Authorization;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ControllerExceptionFilter]
    public class UserController : Controller
    {
        private readonly LoginService _loginService;
        private readonly CompanyService _companyService;
        private readonly UserService _userService;
        private readonly PhraseService _phraseService;
        private readonly DialogueService _dialogueService;

        public UserController(
            LoginService loginService,
            CompanyService companyService,
            UserService userService,
            PhraseService phraseService,
            DialogueService dialogueService
            )
        {
            _loginService = loginService;
            _companyService = companyService;
            _userService = userService;
            _phraseService = phraseService;
            _dialogueService = dialogueService;
        }

        //---USER---

        [HttpGet("User")]
        [SwaggerOperation(Summary = "All company users", Description = "Return all active (status 3) users (array) for loggined company with role Id")]
        [SwaggerResponse(200, "Users with role", typeof(List<UserModel>))]
        public async Task<IEnumerable<UserModel>> UserGet()
        {
                if (_loginService.GetCurrentDeviceId() != null)
                    return  await _userService.GetUsersForDeviceAsync();
                return  await _userService.GetUsers();

                
        }


        [HttpPost("User")]
        [SwaggerOperation(Description = "Create new user with role Employee in loggined company (taked from token) and can save avatar for user / Return new user")]
        [SwaggerResponse(200, "User", typeof(UserModel))]
        public async Task<UserModel> UserPostAsync(
                    [FromForm,
                    SwaggerParameter("json user (include password and unique email) in FormData with key 'data' + file avatar (not required)")]
                    IFormCollection formData) =>
                await _userService.CreateUserWithAvatarAsync(formData);


        [HttpPut("User")]
        [SwaggerOperation(Summary = "Edit user",
                Description = "Edit user (any from loggined company) and return edited. Don't send password and role (can't change). Email must been unique. May contain avatar file")]
        [SwaggerResponse(200, "Edited user", typeof(UserModel))]
        public async Task<UserModel> UserPut(
                    [FromForm, SwaggerParameter("Avatar file (not required) + json User with key 'data' in FormData")]
                    IFormCollection formData) =>
                await _userService.EditUserWithAvatarAsync(formData);


        [HttpDelete("User")]
        [SwaggerOperation(Summary = "Delete or make disabled", Description = "Delete user by Id if he hasn't any relations in DB or make status Disabled")]
        public async Task<string> UserDelete( [FromQuery] Guid applicationUserId) =>
                await _userService.DeleteUserWithAvatarAsync(applicationUserId);


        //---COMPANY---

        [HttpGet("Companies")]
        [SwaggerOperation(Summary = "All corporations companies", Description = "Return all companies for loggined corporation (only for role Supervisor)")]
        [SwaggerResponse(200, "Companies", typeof(List<Company>))]
        public async Task<IEnumerable<Company>> CompaniesGet()
        {
                var roleInToken = _loginService.GetCurrentRoleName();
                if (roleInToken == "Admin")
                    return await _companyService.GetCompaniesForAdminAsync();
                if (roleInToken == "Supervisor") // only for corporations
                    return await _companyService.GetCompaniesForSupervisorAsync(_loginService.GetCurrentCorporationId());
                throw new AccessException("Not allowed access(role)");
        }

        [HttpPost("Company")]
        [SwaggerOperation(Description = "Create new company for corporation")]
        [SwaggerResponse(200, "Company", typeof(Company))]
        public async Task<Company> CompanysPostAsync( [FromBody] Company model)
        {
                var roleInToken = _loginService.GetCurrentRoleName();

                if (roleInToken == "Admin")
                    return await _companyService.AddNewCompanyAsync(model, model.CorporationId);
                if (roleInToken == "Supervisor")
                {
                    var corporatioIdInToken = _loginService.GetCurrentCorporationId();
                    return await _companyService.AddNewCompanyAsync(model, corporatioIdInToken);
                }
                throw new AccessException("Not allowed access(role)");
        }

        [HttpPut("Company")]
        [SwaggerOperation(Summary = "Edit company", Description = "Edit company")]
        [SwaggerResponse(200, "Edited company", typeof(Company))]
        public async Task<Company> CompanyPut([FromBody] Company model)
        {
            var roleInToken = _loginService.GetCurrentRoleName();
            if (roleInToken != "Admin" && roleInToken != "Supervisor")
                throw new AccessException("Not allowed access(role)");
            return await _companyService.UpdateCompanAsync(model);
        }

        [HttpGet("Corporations")]
        [SwaggerOperation(Summary = "All corporations", Description = "Return all corporations for loggined admins (only for role Admin)")]
        [SwaggerResponse(200, "Corporations", typeof(List<Company>))]
        public async Task<IEnumerable<Corporation>> CorporationsGet()
        {
            var roleInToken = _loginService.GetCurrentRoleName();
            if (roleInToken != "Admin") throw new AccessException("Not allowed access(role)");
            return await _companyService.GetCorporationAsync();
        }


        //---PHRASE---

        [HttpGet("PhraseLib")]
        [SwaggerOperation(Summary = "Library",
                Description = "Return collections phrases from library (only templates and only with language code = loggined company language code) which company has not yet used")]
        [SwaggerResponse(200, "Library phrase collection", typeof(List<Phrase>))]
        public async Task<IActionResult> PhraseGet()
        {
                var phraseIds = await _phraseService.GetPhraseIdsByCompanyIdAsync(true);
                var phrases = await _phraseService.GetPhrasesNotBelongToCompanyByIdsAsync(phraseIds, true);
                return Ok(phrases);
        }

        [HttpPost("PhraseLib")]
        [SwaggerOperation(Summary = "Create company phrase (not library!)",
            Description = "Save new phrase to DB and attach it to loggined company (create new PhraseCompany). Assigned that the phrase is not template")]
        [SwaggerResponse(200, "New phrase", typeof(Phrase))]
        public async Task<Phrase> PhrasePost(
                    [FromBody] PhrasePost message) =>
            await _phraseService.CreateNewPhraseAndAddToCompanyAsync(message);

        [HttpPut("PhraseLib")]
        [SwaggerOperation(Summary = "Edit company phrase", Description = "Edit phrase. You can edit only your own phrase (not template from library)")]
        public async Task<Phrase> PhrasePut(
                    [FromBody] Phrase message)
        {
            var phrase = await _phraseService.GetPhraseInCompanyByIdAsync(message.PhraseId, false);
            return await _phraseService.EditAndSavePhraseAsync(phrase, message);
        }

        [HttpDelete("PhraseLib")]
        [SwaggerOperation(Summary = "Delete company phrase", Description = "Delete phrase (if this phrase are Template it can't be deleted, it only delete connection to company")]
        public async Task<string> PhraseDelete(
                    [FromQuery(Name = "phraseId"), SwaggerParameter("phraseId Guid", Required = true)] Guid phraseId)
        {
                var phrase = await _phraseService.GetPhraseByIdAsync(phraseId);
                return await _phraseService.DeleteAndSavePhraseWithPhraseCompanyAsync(phrase);
        }

        [HttpGet("CompanyPhrase")]
        [SwaggerOperation(Summary = "Return attached to company(-ies) phrases", Description = "Return own and template phrases collection for companies sended in params or for loggined company")]
        public async Task<List<Phrase>> CompanyPhraseGet(
                [FromQuery(Name = "companyId"), SwaggerParameter("list guids, if not passed - takes from token")] List<Guid> companyIds) =>
                await _phraseService.GetPhrasesInCompanyByIdsAsync(companyIds);

        [HttpPost("CompanyPhrase")]
        [SwaggerOperation(Summary = "Attach library(template) phrases to company", Description = "Attach phrases  from library (ids sended in body) to loggined company  (create new PhraseCompany entities)")]
        public async Task CompanyPhrasePost(
                [FromBody, SwaggerParameter("array ids", Required = true)] List<Guid> phraseIds) =>
                await _phraseService.CreateNewPhrasesCompanyAsync(phraseIds);

        //---DIALOGUES---

        // to do: add dialogue phrase and add make migration 
        // format of datetime is yyyymmddhhmmss
        [HttpGet("Dialogue")]
        [SwaggerOperation(Description = "Return collection of dialogues from dialogue phrases by filters")]
        public async Task<List<DialogueGetModel>> DialogueGet([FromQuery(Name = "begTime")] string beg,
                                                [FromQuery(Name = "endTime")] string end,
                                                [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds,
                                                [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                                [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds,
                                                [FromQuery(Name = "inStatistic")] bool? inStatistic) =>
             await _dialogueService.GetAllDialogues(beg, end, applicationUserIds, deviceIds, companyIds, 
                                                        corporationIds, phraseIds, phraseTypeIds, inStatistic);
        

        [HttpGet("DialoguePaginated")]
        [SwaggerOperation(Description =
            "Return collection of dialogues from dialogue phrases by filters (one page). limit=10, page=0, orderBy=BegTime/EndTime/FullName/Duration/MeetingExpectationsTotal, orderDirection=desc/asc")]
        public async Task<string> DialoguePaginatedGet([FromQuery(Name = "begTime")] string beg,
                                           [FromQuery(Name = "endTime")] string end,
                                           [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                           [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds,
                                           [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                           [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                           [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                           [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds,
                                           
                                           [FromQuery(Name = "inStatistic")] bool? inStatistic,
                                           [FromQuery(Name = "limit")] int limit = 10,
                                           [FromQuery(Name = "page")] int page = 0,
                                           [FromQuery(Name = "orderBy")] string orderBy = "BegTime",
                                           [FromQuery(Name = "orderDirection")] string orderDirection = "desc") =>
            await _dialogueService.GetAllDialoguesPaginated(beg, end, applicationUserIds, deviceIds, companyIds,
                                                        corporationIds, phraseIds, phraseTypeIds, inStatistic, 
                                                        limit, page, orderBy, orderDirection);
    

        [HttpGet("DialogueInclude")]
        [SwaggerOperation(Description = "Return dialogue with relative data by filters")]
        public async Task<Dictionary<string, object>> DialogueGetInclude(
                    [FromQuery(Name = "dialogueId")] Guid dialogueId) =>
           await _dialogueService.DialogueGet(dialogueId);


        [HttpPut("Dialogue")]
        [SwaggerOperation(Summary = "Change InStatistic", Description = "Change InStatistic(true/false) of dialogue/dialogues")]
        public async Task<bool> DialoguePut(
                [FromBody] DialoguePut message) =>
           await _dialogueService.ChangeInStatistic(message);


        [HttpPut("DialogueSatisfaction")]
        [SwaggerOperation(Summary = "Change Satisfaction ", Description = "Change Satisfaction (true/false) of dialogue")]
        public async Task<string> DialogueSatisfactionPut(
            [FromBody] DialogueSatisfactionPut message) =>
           await _dialogueService.SatisfactionChangeByTeacher(message);

        [HttpGet("Alert")]
        [SwaggerOperation(Summary = "all alerts for period", Description = "Return all alerts for period, type, employee, worker type, time")]
        public async Task<object> AlertGet([FromQuery(Name = "begTime")] string beg,
                                                            [FromQuery(Name = "endTime")] string end,
                                                            [FromQuery(Name = "applicationUserId[]")] List<Guid?> applicationUserIds,
                                                            [FromQuery(Name = "alertTypeId[]")] List<Guid> alertTypeIds,
                                                            [FromQuery(Name = "deviceId[]")] List<Guid> deviceIds) =>
           await _dialogueService.GetAlerts(beg, end, applicationUserIds, alertTypeIds, deviceIds);


        [HttpPost("VideoMessage")]
        [SwaggerOperation(Summary = "SendVideoMessageToManager",
                Description = "Send video messgae to office manager")]
        public async Task SendVideo(
                    [FromForm, SwaggerParameter("json message with key 'data' in FormData")] IFormCollection formData)
              => await _userService.SendVideoMessageToManager(formData);
        
    }
}