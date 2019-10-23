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
        private readonly SmtpSettings _smtpSetting;
        private readonly SmtpClient _smtpClient;
        private readonly MailSender _mailSender;
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
            RequestFilters requestFilters,
            MailSender mailSender,
            SmtpSettings smtpSetting,
            SmtpClient smtpClient
            // ElasticClient log
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _sftpClient = sftpClient;
            _requestFilters = requestFilters;
            _mailSender = mailSender;
            // _log = log;
            _containerName = "useravatars";
            activeStatus = 3;
            disabledStatus = 4;
            _smtpSetting = smtpSetting;
            _smtpClient = smtpClient;
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
                var empployeeId = Guid.Parse(userClaims["applicationUserId"]);

                users = _context.ApplicationUsers.Include(p => p.UserRoles).ThenInclude(x => x.Role)
                    .Where(p => p.CompanyId == companyId && (p.StatusId == activeStatus || p.StatusId == disabledStatus)).ToList();     //2 active, 3 - disabled    

                if (role == "Admin")
                    users = _context.ApplicationUsers.Include(p => p.UserRoles).ThenInclude(x => x.Role)
                        .Where(p => p.StatusId == activeStatus || p.StatusId == disabledStatus).ToList();     //2 active, 3 - disabled                  
                if ((role == "Supervisor" || role == "Manager") && corporationId != null)
                {
                    var isManager = role == "Manager";
                    var isSupervisor = role == "Supervisor";

                    if (isManager)
                    {
                        users = _context.ApplicationUsers
                            .Include(p => p.UserRoles).ThenInclude(x => x.Role)
                            .Include(p => p.Company)
                            .Where(p => p.CompanyId == companyId
                                && (p.StatusId == activeStatus || p.StatusId == disabledStatus))
                            //&& p.Id != empployeeId)
                            .ToList();
                    }
                    else if (isSupervisor)
                    {
                        users = _context.ApplicationUsers
                            .Include(p => p.UserRoles).ThenInclude(x => x.Role)
                            .Include(p => p.Company)
                            .Where(p => p.Company.CorporationId.ToString() == corporationId
                                && (p.StatusId == activeStatus || p.StatusId == disabledStatus))
                            // && p.Id != empployeeId)
                            .ToList();
                    }
                    else
                        return BadRequest($"Not allowed new user role for employee role: {role}");
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
                    [FromForm, SwaggerParameter("json user (include password and unique email) in FormData with key 'data' + file avatar (not required)")] IFormCollection formData,
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");

                var companyIdInToken = userClaims["companyId"];
                var corporationIdInToken = userClaims["corporationId"];
                var roleInToken = userClaims["role"];
                var userDataJson = formData.FirstOrDefault(x => x.Key == "data").Value.ToString();

                PostUser message = JsonConvert.DeserializeObject<PostUser>(userDataJson);
                if (_context.ApplicationUsers.Where(x => x.NormalizedEmail == message.Email.ToUpper()).Any())
                    return BadRequest("User email not unique");

                //---for supervisor is allowed to create user with any role. Manager can create only Employee
                message.RoleId = _requestFilters.CheckAndGetAllowedRole(message.RoleId, roleInToken);
                message.CompanyId = message.CompanyId != null? message.CompanyId : userClaims["companyId"];

                List<Guid> allowedEmployeeRoles = _requestFilters.GetAllowedRoles(roleInToken);
                bool isCompanyBelongToUser = _requestFilters.IsCompanyBelongToUser(corporationIdInToken, companyIdInToken, message.CompanyId, roleInToken);

                if (isCompanyBelongToUser == false)
                    return BadRequest($"Not allowed user role");

                if (allowedEmployeeRoles == null || !allowedEmployeeRoles.Where(p => p == Guid.Parse(message.RoleId)).Any())
                    return BadRequest($"Not allowed new user role for employee role: {roleInToken}");
                
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
                    WorkerTypeId = message.WorkerTypeId ?? _context.WorkerTypes.FirstOrDefault(x => x.WorkerTypeName == "Employee")?.WorkerTypeId
                };
                await _context.AddAsync(user);

                //---save avatar---
                string avatarUrl = null;
                if (formData.Files.Count != 0)
                {
                    FileInfo fileInfo = new FileInfo(formData.Files[0].FileName);
                    var fn = user.Id + fileInfo.Extension;
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

                var userForEmail = _context.ApplicationUsers.Include(x => x.Company).FirstOrDefault(x => x.Id == user.Id);
                try
                {
                    await _mailSender.SendUserRegisterEmail(userForEmail, message.Password);
                }
                catch { }
                return Ok(new UserModel(user, avatarUrl));
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
                // _log.Info("User/User PUT started");
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");

                var userDataJson = formData.FirstOrDefault(x => x.Key == "data").Value.ToString();
                ApplicationUser message = JsonConvert.DeserializeObject<ApplicationUser>(userDataJson);
                var user = _context.ApplicationUsers
                    .Include(p => p.UserRoles)
                    .Include(p => p.Company)
                    .Where(p => p.Id == message.Id
                        && (p.StatusId == activeStatus || p.StatusId == disabledStatus))// 2 - active, 3 - disabled
                    .FirstOrDefault();

                var companyId = Guid.Parse(userClaims["companyId"]);
                var corporationId = userClaims["corporationId"];
                var role = userClaims["role"];
                var empployeeId = Guid.Parse(userClaims["applicationUserId"]);

                var isAdmin = role == "Admin";
                var isManager = role == "Manager";
                var isSupervisor = role == "Supervisor";

                if (role == "Manager" || role == "Supervisor")
                {
                    var allRoles = _context.Roles.ToList();
                    List<Guid> allowedEmployeeRoles = _requestFilters.GetAllowedRoles(role);
                    var changedUserCompany = _context.Companys.FirstOrDefault(p => p.CompanyId == user.CompanyId);
                    if (isSupervisor)
                    {
                        if (changedUserCompany == null || (corporationId != null && corporationId != changedUserCompany.CorporationId.ToString()))
                            BadRequest($"Changed user company are not from the same corporation: {corporationId}!");
                    }
                    else if (isManager)
                    {
                        if (changedUserCompany == null || companyId != user.CompanyId)
                            BadRequest($"Changed user are not from the same company: {corporationId}!");
                    }
                    else
                        BadRequest($"Not allowed new user role for employee role: {role}");

                    if (user.UserRoles.Where(p => !allowedEmployeeRoles.Contains(p.Role.Id)).Any())
                        return BadRequest($"Not allowed change user for {role}");
                }

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
                    _context.SaveChanges();
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
                System.Console.WriteLine(0);
                // _log.Info("User/User DELETE started");
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);
                var corporationId = Guid.Parse(userClaims["corporationId"]);
                var role = userClaims["role"];
                var user = _context.ApplicationUsers
                    .Include(x => x.UserRoles)
                    .Include(x => x.Company)
                    .Include(x => x.PasswordHistorys)
                    .First(p => p.Id == applicationUserId);
                var isAdmin = role == "Admin";
                var isSupervisor = role == "Supervisor";

                var allRoles = _context.Roles.ToList();
               List < Guid > allowedEmployeeRoles = _requestFilters.GetAllowedRoles(role);              

                if (allowedEmployeeRoles == null || user.UserRoles.Where(p => !allowedEmployeeRoles.Contains(p.Role.Id)).Any())
                    return BadRequest($"Not allowed remove user with employee {role}");

                if (user != null)
                {
                    //using (var transactionScope = new
                    //     TransactionScope(TransactionScopeOption.Suppress, new TransactionOptions()
                    //                                { IsolationLevel = IsolationLevel.Serializable }))
                    //{
                    try
                    {
                        await Task.Run(() => _sftpClient.DeleteFileIfExistsAsync($"{_containerName}/{user.Id}"));
                        if (user.UserRoles != null && user.UserRoles.Count() != 0)
                            _context.RemoveRange(user.UserRoles);
                        if (user.PasswordHistorys != null && user.PasswordHistorys.Count() != 0)
                            _context.RemoveRange(user.PasswordHistorys);
                        _context.Remove(user);
                        await _context.SaveChangesAsync();
                        // transactionScope.Complete();
                    }
                    catch
                    {
                        user.StatusId = disabledStatus;
                        await _context.SaveChangesAsync();
                        return Ok("Disabled Status");
                    }
                    //}
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
                if (userClaims["role"] == "Admin") // very cool!
                {
                    var companies = _context.Companys.Where(p => p.StatusId == activeStatus || p.StatusId == disabledStatus).ToList();  // 2 active, 3 - disabled
                    return Ok(companies);
                }
                if (userClaims["role"] == "Supervisor" && userClaims["corporationId"] != null) // only for own corporation
                {
                    var corporationId = Guid.Parse(userClaims["corporationId"]);
                    var companies = _context.Companys // 2 active, 3 - disabled
                        .Where(p => p.CorporationId == corporationId && (p.StatusId == activeStatus || p.StatusId == disabledStatus)).ToList();
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

        [HttpPost("Company")]
        [SwaggerOperation(Description = "Create new company for corporation")]
        [SwaggerResponse(200, "Company", typeof(Company))]
        public async Task<IActionResult> CompanysPostAsync(
                    //  [FromBody] PostUser message,  
                    [FromForm, SwaggerParameter("Company name")] IFormCollection formData,
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                    return BadRequest("Token wrong");
                var userDataJson = formData.FirstOrDefault(x => x.Key == "data").Value.ToString();
                var companyModel = JsonConvert.DeserializeObject<CompanyModel>(userDataJson);
                if (companyModel is null)
                    BadRequest("Company Model not deserialized");

                var companyId = Guid.Parse(userClaims["companyId"]);
                var role = userClaims["role"];
                Company newCompany;
                Guid newCompanyGuid = Guid.NewGuid();

                if (role == "Supervisor" || role == "Admin")
                {
                    if (role == "Admin")
                    {
                        if (companyModel.CompanyName is null)
                            return BadRequest("Company name is null");

                        CompanyIndustry companyIndustry = _context.CompanyIndustrys.FirstOrDefault(p => p.CompanyIndustryId == companyModel.CompanyIndustryId);
                        if (companyIndustry is null)
                            return BadRequest("Company industry is null");

                        Language language = _context.Languages.FirstOrDefault(p => p.LanguageId == companyModel.LanguageId);
                        if (language is null)
                            return BadRequest("Company language is null");

                        Country country = _context.Countrys.FirstOrDefault(p => p.CountryId == companyModel.CountryId);
                        if (country is null)
                            return BadRequest("Company country is null");

                        Status status = _context.Statuss.FirstOrDefault(p => p.StatusId == companyModel.StatusId);
                        if (status is null)
                            return BadRequest("Company status is null");

                        Corporation corporation = _context.Corporations.FirstOrDefault(p => p.Id == companyModel.CorporationId);
                        if (companyModel.CorporationId != null && corporation == null)
                            BadRequest("Company corporation is null");

                        newCompany = new Company()
                        {
                            CompanyId = newCompanyGuid,
                            CompanyName = companyModel.CompanyName,
                            CompanyIndustryId = companyModel.CompanyIndustryId,
                            CompanyIndustry = companyIndustry,
                            CreationDate = DateTime.Now,
                            LanguageId = companyModel.LanguageId,
                            Language = language,
                            CountryId = companyModel.CountryId,
                            Country = country,
                            StatusId = companyModel.StatusId,
                            Status = status,
                            Payment = null,
                            CorporationId = companyModel.CorporationId != null ? companyModel.CorporationId : null,
                            Corporation = corporation
                        };
                        _context.Companys.Add(newCompany);
                    }
                    else if (role == "Supervisor")
                    {
                        if (companyModel.CompanyName is null)
                            return BadRequest("Company name is null");

                        var employeeCompany = _context.Companys
                            .Include(p => p.CompanyIndustry)
                            .Include(p => p.Language)
                            .FirstOrDefault(p => p.CompanyId == companyId);

                        newCompany = new Company()
                        {
                            CompanyId = newCompanyGuid,
                            CompanyName = companyModel.CompanyName,
                            CompanyIndustryId = employeeCompany.CompanyIndustryId,
                            CompanyIndustry = employeeCompany.CompanyIndustry,
                            CreationDate = DateTime.Now,
                            LanguageId = employeeCompany.LanguageId,
                            Language = employeeCompany.Language,
                            CountryId = employeeCompany.CountryId,
                            Country = employeeCompany.Country,
                            StatusId = employeeCompany.StatusId,
                            Status = employeeCompany.Status,
                            Payment = employeeCompany.Payment,
                            CorporationId = employeeCompany.CorporationId != null ? employeeCompany.CorporationId : null,
                            Corporation = employeeCompany.Corporation
                        };
                        _context.Companys.Add(newCompany);
                    }
                    _context.SaveChanges();
                    var company = _context.Companys.FirstOrDefault(p => p.CompanyId == newCompanyGuid);
                    return Ok(company);
                }
                return BadRequest("employee role is not accessible for create company");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut("Company")]
        [SwaggerOperation(Summary = "Edit company",
                Description = "Edit company")]
        [SwaggerResponse(200, "Edited user", typeof(Company))]
        public async Task<IActionResult> CompanyPut(
                    [FromForm, SwaggerParameter("json Company with key 'data' in FormData")] IFormCollection formData,
                    [FromHeader, SwaggerParameter("JWT token", Required = true)] string Authorization)
        {
            if (!_loginService.GetDataFromToken(Authorization, out userClaims))
                return BadRequest("Token wrong");
            var companyId = Guid.Parse(userClaims["companyId"]);
            var corporationId = Guid.Parse(userClaims["corporationId"]);
            var role = userClaims["role"];

            var userDataJson = formData.FirstOrDefault(x => x.Key == "data").Value.ToString();
            Company message = JsonConvert.DeserializeObject<Company>(userDataJson);

            if (role == "Admin" || role == "Supervisor")
            {
                var company = _context.Companys.FirstOrDefault(p => p.CompanyId == message.CompanyId);
                if (company is null)
                    return BadRequest($"company with such companyId: {message.CompanyId} not exist");

                if (role == "Supervisor")
                {
                    var supervisorCompany = _context.Companys.FirstOrDefault(p => p.CompanyId == companyId);
                    if (message.CorporationId!= null && supervisorCompany.CorporationId != message.CorporationId)
                        return BadRequest($"changable company corporationId and supervisor corporationId not equal");
                }

                foreach (var p in typeof(Company).GetProperties())
                {
                    if (p.GetValue(message, null) != null && p.GetValue(message, null).ToString() != Guid.Empty.ToString())
                        p.SetValue(company, p.GetValue(message, null), null);
                }
                await _context.SaveChangesAsync();

                return Ok(company);
            }
            else
                return BadRequest($"employee with role: {role} cant change companys");
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
                       !phrasesIncluded.Contains(x.PhraseId) &&
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
                //---search phrase that is in library or that is not belong to any company
                var phrase = _context.Phrases
                        .Include(x => x.PhraseCompany)
                        .Where(x => x.PhraseText.ToLower() == message.PhraseText.ToLower()
                         && (x.IsTemplate == true || x.PhraseCompany.Count() == 0)).FirstOrDefault();

                if (phrase == null)
                {
                    phrase = new Phrase
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
                }
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

                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);
                if (role != "Admin" && role != "Supervisor")
                    return BadRequest("No permission");
                if (role == "Supervisor")
                {
                    var companyForDialogueId = _context.Dialogues
                        .Include(x => x.ApplicationUser)
                        .FirstOrDefault(x => x.DialogueId == message.DialogueId)
                        .ApplicationUser.CompanyId;
                    if (companyForDialogueId != companyId) return BadRequest("No permission");
                }
                var dialogueClientSatisfaction = _context.DialogueClientSatisfactions.FirstOrDefault(x => x.DialogueId == message.DialogueId);
                dialogueClientSatisfaction.MeetingExpectationsByTeacher = message.Satisfaction;
                dialogueClientSatisfaction.BegMoodByTeacher = message.BegMoodTotal;
                dialogueClientSatisfaction.EndMoodByTeacher = message.EndMoodTotal;

                _context.SaveChanges();
                // _log.Info("Function DialogueSatisfactionPut finished");
                return Ok(JsonConvert.SerializeObject(dialogueClientSatisfaction));
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred while executing DialogueSatisfactionPut {e}");
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
                Message message = JsonConvert.DeserializeObject<Message>(userDataJson);  

                var companyIdFromToken = Guid.Parse(userClaims["companyId"]);
                var corporationIdFromToken = Guid.Parse(userClaims["corporationId"]);
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
                else if(roleFromToken == "Manager")
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
    public class CompanyModel
    {
        public Guid CompanyIndustryId;
        public string CompanyName;
        public int LanguageId;
        public Guid CountryId;
        public int StatusId;
        public Guid? CorporationId;
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
        public List<Guid> DialogueIds;//--this done for versions
        public bool InStatistic;
    }

    public class DialogueSatisfactionPut
    {
        public Guid DialogueId;
        public double Satisfaction;
        public double BegMoodTotal;
        public double EndMoodTotal;
    }
    public class Message
    {
        public string Subject;
        public string Body;
    }
}