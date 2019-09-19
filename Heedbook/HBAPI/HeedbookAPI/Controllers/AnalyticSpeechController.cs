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
using UserOperations.Models.AnalyticModels;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using HBData;
using UserOperations.Utils;
using HBLib.Utils;
using Swashbuckle.AspNetCore.Annotations;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticSpeechController : Controller
    {
        private readonly IConfiguration _config;        
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly DBOperations _dbOperation;
        private readonly RequestFilters _requestFilters;
//        private readonly ElasticClient _log;


        public AnalyticSpeechController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            DBOperations dbOperation,
            RequestFilters requestFilters
//            ElasticClient log
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _dbOperation = dbOperation;
            _requestFilters = requestFilters;
//            _log = log;
        }
  

        [HttpGet("EmployeeRating")]
        public IActionResult SpeechEmployeeRating([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        // [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                                        // [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
//                _log.Info("AnalyticSpeech/EmployeeRating started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     

                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId); 
                var typeIdCross = _context.PhraseTypes
                                    .Where(p => p.PhraseTypeText == "Cross")
                                    .Select(p => p.PhraseTypeId).First();
                var typeIdAlert = _context.PhraseTypes
                                    .Where(p => p.PhraseTypeText == "Alert")
                                    .Select(p => p.PhraseTypeId).First();

                    var dialogues = _context.Dialogues
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.DialoguePhrase)
                    .Where(p => p.BegTime >= begTime
                            && p.EndTime <= endTime
                            && p.StatusId == 3
                            && p.InStatistic == true
                            && (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
                    .Select(p => new DialogueInfo
                    {
                        DialogueId = p.DialogueId,
                        ApplicationUserId = p.ApplicationUserId,
                        BegTime = p.BegTime,
                        EndTime = p.EndTime,
                        SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                        FullName = p.ApplicationUser.FullName,
                        CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count(),
                        AlertCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdAlert).Count(),
                    })
                    .ToList();
              
                var result = dialogues
                    .GroupBy(p => p.ApplicationUserId)
                    .Select(p => new
                    {
                        FullName = p.First().FullName,
                        ApplicationUserId = p.Key,
                        CrossFreq = _dbOperation.CrossIndex(p),
                        AlertFreq = _dbOperation.AlertIndex(p)
                    });
//                _log.Info("AnalyticSpeech/EmployeeRating finished");
                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpGet("PhraseTable")]
        public IActionResult SpeechPhraseTable([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                                        [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
//                _log.Info("AnalyticSpeech/PhraseTable started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);                  

                var companysPhrases = _context.PhraseCompanys
                    .Where(p => (!companyIds.Any() || companyIds.Contains((Guid) p.CompanyId)))
                    .Select(p => p.PhraseId)
                    .ToList();
                
                var dialogueIds = _context.Dialogues
                    .Where(p => p.EndTime >= begTime
                        && p.EndTime <= endTime
                        && p.StatusId == 3
                        && p.InStatistic == true)
                     .Where(p => (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                        && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                        && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
                    .Select(p => p.DialogueId).ToList();

                var dialoguesTotal = dialogueIds.Count();               
               
                // GET ALL PHRASES INFORMATION
                var phrasesInfo = _context.DialoguePhrases
                    .Include(p => p.Phrase)
                    .Where(p => p.DialogueId.HasValue && dialogueIds.Contains(p.DialogueId.Value)
                         && (!phraseIds.Any() || phraseIds.Contains((Guid) p.PhraseId))
                         && (!phraseTypeIds.Any() || phraseTypeIds.Contains((Guid) p.Phrase.PhraseTypeId))
                         //&& (companysPhrases.Contains(p.PhraseId))
                         )
                    .Select(p => new {
                        IsClient = p.IsClient,
                        FullName = p.Dialogue.ApplicationUser.FullName,
                        ApplicationUserId = p.Dialogue.ApplicationUserId,
                        DialogueId = p.DialogueId,
                        PhraseId = p.PhraseId,
                        PhraseText = p.Phrase.PhraseText,
                        PhraseTypeText = p.Phrase.PhraseType.PhraseTypeText
                    })
                    .ToList();

                var result = phrasesInfo
                    .GroupBy(p => p.PhraseText.ToLower())
                    .Select(p => new {
                        Phrase = p.Key,
                        PhraseId = p.First().PhraseId,
                        PopularName = p.GroupBy(q => q.FullName)
                            .OrderByDescending(q => q.Count())
                            .Select(g => g.Key)
                            .First(),
                        PhraseType = p.First().PhraseTypeText,
                        Percent = dialogueIds.Any() ? Math.Round(100 * Convert.ToDouble(p.Select(q => q.DialogueId).Distinct().Count()) / Convert.ToDouble(dialoguesTotal), 1) : 0,
                        Freq = Convert.ToDouble(p.Select(q => q.DialogueId).Distinct().Count()) != 0 ?
                            Math.Round(Convert.ToDouble(p.GroupBy(q => q.ApplicationUserId).Max(q => q.Count())) / Convert.ToDouble(p.Select(q => q.DialogueId).Distinct().Count()), 2) :
                            0
                    });
//                _log.Info("AnalyticSpeech/PhraseTable finished");
                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpGet("PhraseTypeCount")]
        [SwaggerOperation(Summary = "% phrases in dialogues", Description = "Return type, procent and colour of phrase type in dialogues (for employees, clients and total)")]
        public IActionResult SpeechPhraseTypeCount([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                                        [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
//                _log.Info("AnalyticSpeech/PhraseTypeCount started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);       

                var dialogueIds = _context.Dialogues
                    .Where(p => p.EndTime >= begTime
                        && p.EndTime <= endTime
                        && p.StatusId == 3
                        && p.InStatistic == true)
                     .Where(p => (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId)
                        && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                        && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId))))
                    .Select(p => p.DialogueId).ToList();

                // CREATE PARAMETERS                
                var totalInfo = new SpeechPhraseTotalInfo();

                var requestPhrase = _context.DialoguePhrases
                    .Include(p => p.Phrase)
                    .Include(p => p.Phrase.PhraseType)
                    .Where(p => p.DialogueId.HasValue && dialogueIds.Contains(p.DialogueId.Value)
                         && (!phraseIds.Any() || phraseIds.Contains((Guid) p.PhraseId))
                         && (!phraseTypeIds.Any() || phraseTypeIds.Contains((Guid) p.Phrase.PhraseTypeId)))
                    .Select(p => new {
                        IsClient = p.IsClient,
                        PhraseType = p.Phrase.PhraseType.PhraseTypeText,
                        Colour = p.Phrase.PhraseType.Colour,
                        DialogueId = p.DialogueId
                    }).ToList();               

                var employee = requestPhrase.Where(p => p.IsClient == false)
                    .GroupBy(p => p.PhraseType)
                    .Select(p => new SpeechPhrasesInfo
                    {
                        Type = p.Key,
                        Count = (requestPhrase.Where(q => q.IsClient == false).Select(q => q.DialogueId).Distinct().Count() != 0) ?
                            Math.Round(100 * Convert.ToDouble(p.Select(q => q.DialogueId).Distinct().Count()) / Convert.ToDouble(requestPhrase.Where(q => q.IsClient == false).Select(q => q.DialogueId).Distinct().Count())) : 0,
                        Colour = p.First().Colour
                    }).ToList();          

                var client = requestPhrase.Where(p => p.IsClient == true & (p.PhraseType == "Loyalty" | p.PhraseType == "Alert"))
                    .GroupBy(p => p.PhraseType)
                    .Select(p => new SpeechPhrasesInfo
                    {
                        Type = p.Key,
                        Count = (requestPhrase.Where(q=> q.IsClient == true).Select(q => q.DialogueId).Distinct().Count() != 0) ? 
                        Math.Round(100 * Convert.ToDouble(p.Select(q => q.DialogueId).Distinct().Count()) / Convert.ToDouble(requestPhrase.Where(q => q.IsClient == true).Select(q => q.DialogueId).Distinct().Count())): 0,
                        Colour = p.First().Colour
                    }).ToList();
                   
                var total = requestPhrase
                    .GroupBy(p => p.PhraseType)
                    .Select(p => new SpeechPhrasesInfo
                    {
                        Type = p.Key,
                        Count = (dialogueIds.Count() != 0) ? Math.Round(100 * Convert.ToDouble(p.Select(q => q.DialogueId).Distinct().Count()) / Convert.ToDouble(dialogueIds.Count())) : 0,
                        Colour = p.First().Colour
                    }).ToList();

                var types = _context.PhraseTypes.ToList();
               // var employeeType = employee.GetType();
                foreach (var type in types)
                {
                    if (employee.Where(p => p.Type == type.PhraseTypeText).Count() == 0)
                        employee.Add(new SpeechPhrasesInfo
                        {
                            Type = type.PhraseTypeText,
                            Count = 0,
                            Colour = type.Colour
                        });

                    if (client.Where(p => p.Type == type.PhraseTypeText).Any() && (type.PhraseTypeText == "Loyalty" | type.PhraseTypeText == "Alert"))
                        client.Add(new SpeechPhrasesInfo
                        {
                            Type = type.PhraseTypeText,
                            Count = 0,
                            Colour = type.Colour
                        });

                    if (total.Where(p => p.Type == type.PhraseTypeText).Count() == 0)
                        total.Add(new SpeechPhrasesInfo
                        {
                            Type = type.PhraseTypeText,
                            Count = 0,
                            Colour = type.Colour
                        });
                }
                totalInfo.Client = client;
                totalInfo.Employee = employee;
                totalInfo.Total = total;

//                _log.Info("AnalyticSpeech/PhraseTypeCount finished");
                return Ok(totalInfo);
            }
            catch (Exception e)
            {
//                _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);            
            }
        }

        [HttpGet("WordCloud")]
        public IActionResult SpeechWordCloud([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromQuery(Name = "phraseId[]")] List<Guid> phraseIds,
                                                        [FromQuery(Name = "phraseTypeId[]")] List<Guid> phraseTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
//                _log.Info("AnalyticSpeech/WordCloud started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);       

                var dialogueIds = _context.Dialogues
                    .Where(p => p.EndTime >= begTime
                        && p.EndTime <= endTime
                        && p.StatusId == 3
                        && p.InStatistic == true)
                     .Where(p => (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                        && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                        && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
                    .Select(p => p.DialogueId).ToList();

                var phrases = _context.DialoguePhrases
                    .Include(p => p.Phrase)
                    .Include(p => p.Phrase.PhraseType)
                    .Where(p => p.DialogueId.HasValue && dialogueIds.Contains(p.DialogueId.Value)
                         && (!phraseIds.Any() || phraseIds.Contains((Guid) p.PhraseId))
                         && (!phraseTypeIds.Any() || phraseTypeIds.Contains((Guid) p.Phrase.PhraseTypeId)))
                    .Select(p =>new {
                        PhraseText = p.Phrase.PhraseText,
                        PhraseColor = p.Phrase.PhraseType.Colour
                    }).ToList();

                var result = phrases.GroupBy(p => p.PhraseText)
                    .Select(p => new {
                        Text = p.First().PhraseText,
                        Weight = 2 * p.Count(),
                        Colour = p.First().PhraseColor});

//                _log.Info("AnalyticSpeech/WordCloud finished");
                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
//                _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }
    }
     public class SpeechPhrasesInfo
    {
        public string Type { get; set; }
        public double Count { get; set; }
        public string Colour { get; set; }
    }

    public class SpeechPhraseTotalInfo
    {
        public List<SpeechPhrasesInfo> Client { get; set; }
        public List<SpeechPhrasesInfo> Employee { get; set; }
        public List<SpeechPhrasesInfo> Total{ get; set; }
    }
}
