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
using static UserOperations.Utils.DBOperations;
using UserOperations.Utils;
using HBLib.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticHomeController : Controller
    {
      private readonly IConfiguration _config;        
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly DBOperations _dbOperation;
        private readonly RequestFilters _requestFilters;
        private readonly ElasticClient _log;
        public AnalyticHomeController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            DBOperations dbOperation,
            RequestFilters requestFilters,
            ElasticClient log
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _dbOperation = dbOperation;
            _requestFilters = requestFilters;
            _log = log;
        }

        [HttpGet("Dashboard")]
        public IActionResult GetDashboard([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                _log.Info("AnalyticHome/Dashboard started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);       
                var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                Console.WriteLine("---------------1------------------");

                var sessions = _context.Sessions
                        .Include(p => p.ApplicationUser)
                        .Where(p => p.BegTime >= prevBeg
                                && p.EndTime <= endTime
                                && p.StatusId == 7
                                && (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                                && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                                && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
                        .Select(p => new SessionInfo
                        {
                            ApplicationUserId = p.ApplicationUserId,
                            BegTime = p.BegTime,
                            EndTime = p.EndTime
                        })
                        .ToList();

                var sessionCur = sessions.Where(p => p.BegTime.Date >= begTime).ToList();
                var sessionOld = sessions.Where(p => p.BegTime.Date < begTime).ToList();

                var typeIdCross = _context.PhraseTypes
                    .Where(p => p.PhraseTypeText == "Cross")
                    .Select(p => p.PhraseTypeId)
                    .First();

                var dialogues = _context.Dialogues
                        .Include(p => p.ApplicationUser)
                        .Include(p => p.DialogueClientSatisfaction)
                        .Include(p => p.DialoguePhraseCount)
                        .Where(p => p.BegTime >= prevBeg
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
                            FullName = p.ApplicationUser.FullName,
                            BegTime = p.BegTime,
                            EndTime = p.EndTime,
                            CrossCout = p.DialoguePhraseCount.Where(q => q.PhraseTypeId == typeIdCross).Count(),
                            SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                            SatisfactionScoreBeg = p.DialogueClientSatisfaction.FirstOrDefault().BegMoodByNN,
                            SatisfactionScoreEnd = p.DialogueClientSatisfaction.FirstOrDefault().EndMoodByNN
                        })
                        .ToList(); 

//-----------------FOR BRANCH---------------------------------------------------------------
                var companyIdsInIndustryExceptSelected = _requestFilters.CompanyIdsInIndustryExceptSelected(companyIds);  
                var companyIdsInIndustry = _requestFilters.CompanyIdsInIndustry(companyIds);  
                var companyIdsInHeedbookExceptSelected = _requestFilters.CompanyIdsInHeedbookExceptSelected(companyIds);  
//-----------------ALL-----------------------
                //---for selected period in industries except selected companies
                var indexesInIndustryExeptSelectedComp = _context.VIndexesByCompanysDays
                    .Where(p => companyIdsInIndustryExceptSelected.Contains (p.CompanyId)
                    && p.Day > begTime && p.Day < endTime
                    ).ToList();
                //---for all period in industries
                var indexesInIndustryAllForBenchmark = _context.VIndexesByCompanysDays
                    .Where(p => companyIdsInIndustry.Contains (p.CompanyId)
                    ).ToList();
                 //---for selected period in industries except selected companies
                var indexesInHeedbook = _context.VIndexesByCompanysDays
                    .Where(p => companyIdsInHeedbookExceptSelected.Contains (p.CompanyId)
                    && p.Day > begTime && p.Day < endTime
                     ).ToList();

//-----------------DIALOGUES INDEXES-----------------------
                //---for selected period in industries except selected companies
                var indexesByDialogueExeptSelectedComp = _context.VIndexesByDialoguesDays
                    .Where(p => companyIdsInIndustryExceptSelected.Contains (p.CompanyId)
                    && p.Day > begTime && p.Day < endTime
                    ).ToList();
                //---for all period in industries
                var indexesByDialogueAllForBenchmark = _context.VIndexesByDialoguesDays
                    .Where(p => companyIdsInIndustry.Contains (p.CompanyId)
                    ).ToList();
                 //---for selected period in industries except selected companies
                var indexesByDialogueHeedbook = _context.VIndexesByDialoguesDays
                    .Where(p => companyIdsInHeedbookExceptSelected.Contains (p.CompanyId)
                    && p.Day > begTime && p.Day < endTime
                     ).ToList();

                // Console.WriteLine($"--ind---{satisfactionByCompanysDaysInIndustry.Sum(p => p.SatisfactionIndex)}----"); 
                // Console.WriteLine($"--begTime---{begTime}----");   
                // Console.WriteLine($"--compId---{companyIds[0]}----");   

                var dialoguesCur = dialogues.Where(p => p.BegTime >= begTime).ToList();
                var dialoguesOld = dialogues.Where(p => p.BegTime < begTime).ToList();             

                var result =  new DashboardInfo()
                {                
                    DialoguesCount = _dbOperation.DialoguesCount(dialoguesCur),
                    DialoguesCountDelta = -_dbOperation.DialoguesCount(dialoguesOld),
                    EmployeeCount = _dbOperation.EmployeeCount(dialoguesCur),
                    EmployeeCountDelta = - _dbOperation.EmployeeCount(dialoguesOld),
                    BestEmployee = _dbOperation.BestEmployee(dialoguesCur, sessionCur, begTime, endTime.AddDays(1)),
                    BestEmployeeEfficiency = _dbOperation.BestEmployeeEfficiency(dialoguesCur, sessionCur, begTime, endTime.AddDays(1)),
                    BestProgressiveEmployee = _dbOperation.BestProgressiveEmployee(dialogues, begTime),
                    BestProgressiveEmployeeDelta = _dbOperation.BestProgressiveEmployeeDelta(dialogues, begTime),

                    SatisfactionDialogueDelta = _dbOperation.SatisfactionDialogueDelta(dialogues),                  
                    SatisfactionIndex = _dbOperation.SatisfactionIndex(dialoguesCur),
                    SatisfactionIndexDelta = - _dbOperation.SatisfactionIndex(dialoguesOld),
                    SatisfactionIndexIndustryAverage = indexesInIndustryExeptSelectedComp.Count() != 0? indexesInIndustryExeptSelectedComp.Sum(p => p.SatisfactionIndex)
                                    / indexesInIndustryExeptSelectedComp.Count() : 0,
                    SatisfactionIndexIndustryBenchmark = indexesInIndustryAllForBenchmark.Max(p => p.SatisfactionIndex),
                    SatisfactionIndexTotalAverage = indexesInHeedbook.Sum(p => p.SatisfactionIndex) 
                                    / indexesInHeedbook.Count(),

                    LoadIndex = _dbOperation.LoadIndex(sessionCur, dialoguesCur, begTime, endTime.AddDays(1)),
                    LoadIndexDelta = - _dbOperation.LoadIndex(sessionOld, dialoguesOld, prevBeg, endTime),
                    LoadIndexIndustryAverage = indexesInIndustryExeptSelectedComp.Count() !=0 ? 100 * indexesInIndustryExeptSelectedComp
                                    .Where(p => p.SessionHours != 0).Sum(p => p.DialoguesHours / p.SessionHours)
                                    / indexesInIndustryExeptSelectedComp.Count() : 0,
                    LoadIndexIndustryBenchmark = 100 * indexesInIndustryAllForBenchmark.Max(p => p.DialoguesHours/ p.SessionHours),
                    LoadIndexTotalAverage = 100 * indexesInHeedbook.Sum(p => p.DialoguesHours / p.SessionHours)/ indexesInHeedbook.Count(),
                    
                    CrossIndex = _dbOperation.CrossIndex(dialoguesCur),
                    CrossIndexDelta = - _dbOperation.CrossIndex(dialoguesOld),
                    CrossIndexIndustryAverage = indexesByDialogueExeptSelectedComp.GroupBy(x => x.CompanyId).Count() != 0?
                         (double)indexesByDialogueExeptSelectedComp.GroupBy(x => x.CompanyId)
                        .Sum(x => (double)x.Count(p => p.CrossCount != 0) /(double)x.Count())
                        / (double)indexesByDialogueExeptSelectedComp.GroupBy(x => x.CompanyId).Count() : 0,
                    CrossIndexIndustryBenchmark = (double)indexesByDialogueAllForBenchmark.GroupBy(x => x.CompanyId)
                        .Max(x => (double)x.Count(p => p.CrossCount != 0) /(double)x.Count()), 
                    CrossIndexTotalAverage = (double)indexesByDialogueHeedbook.GroupBy(x => x.CompanyId)
                        .Sum(x => (double)x.Count(p => p.CrossCount != 0) /(double)x.Count())
                        / (double)indexesByDialogueHeedbook.GroupBy(x => x.CompanyId).Count(),

                    AvgWorkingTimeEmployees = _dbOperation.LoadPeriod(begTime, endTime, dialoguesCur, sessionCur),
                    AvgWorkingTimeEmployeesDelta = _dbOperation.LoadPeriod(prevBeg, endTime, dialoguesOld, sessionOld)
                };

              
                Console.WriteLine($"--comp---{indexesByDialogueExeptSelectedComp.Count(x => x.CrossCount != 0)}----");
                Console.WriteLine($"--comp---{indexesByDialogueExeptSelectedComp.Count()}----");
                result.NumberOfDialoguesPerEmployees = result.DialoguesCount / result.EmployeeCount;   
                result.NumberOfDialoguesPerEmployeesDelta =  - result.DialoguesCountDelta 
                    / (result.EmployeeCountDelta != 0? result.EmployeeCountDelta : 1) + result.NumberOfDialoguesPerEmployees; 
                
                result.AvgWorkingTimeEmployeesDelta +=result.AvgWorkingTimeEmployees;
                result.EmployeeCountDelta += result.EmployeeCount;
                result.CrossIndexDelta += result.CrossIndex;
                result.LoadIndexDelta += result.LoadIndex;
               // result.EfficiencyIndexDelta += result.EfficiencyIndex;
                result.SatisfactionIndexDelta += result.SatisfactionIndex;
                result.DialoguesCountDelta += result.DialoguesCount;

                             


                var jsonToReturn = JsonConvert.SerializeObject(result);  
                _log.Info("AnalyticHome/Dashboard finished");          
                return Ok(jsonToReturn);
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }

        [HttpGet("Recomendation")]
        public IActionResult GetRecomendation([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)

        {
            _log.Info("AnalyticHome/Recomendation started");
            if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
            var role = userClaims["role"];
            var companyId = Guid.Parse(userClaims["companyId"]);     
            var begTime = _requestFilters.GetBegDate(beg);
            var endTime = _requestFilters.GetEndDate(end);
            _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);       
            var hintCount = !String.IsNullOrEmpty(_config["hint : hintCount"]) ? Convert.ToInt32(_config["hint : hintCount"]): 3;
          

            var hints =_context.DialogueHints
                    .Include(p => p.Dialogue)
                    .Include(p => p.Dialogue.ApplicationUser)
                    .Where(p => p.Dialogue.BegTime >= begTime && p.Dialogue.EndTime <= endTime
                            && p.Dialogue.InStatistic == true
                            && (!companyIds.Any() || companyIds.Contains((Guid) p.Dialogue.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.Dialogue.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.Dialogue.ApplicationUser.WorkerTypeId)))
                    .Select(p => new
                    {
                        HintText = p.HintText,
                        IsAutomatic = p.IsAutomatic,
                        IsPositive = p.IsPositive,
                        Type = p.Type
                    }).ToList();

            var positiveTopHints = hints.Where(p => p.IsPositive == true)
                .GroupBy(p => p.HintText)
                .Select(p => new
                {
                    HintText = p.Key,
                    HintCount = p.Count()
                })
                .OrderByDescending(p => p.HintCount).Take(hintCount).Select(p => p.HintText).ToList();

            var negativeTopHints = hints.Where(p => p.IsPositive == false)
                .GroupBy(p => p.HintText)
                .Select(p => new
                {
                    HintText = p.Key,
                    HintCount = p.Count()
                })
                .OrderByDescending(p => p.HintCount).Take(hintCount).Select(p => p.HintText).ToList();
            var topHints = new List<TopHintInfo>();
            topHints.Add(new TopHintInfo
            {
                IsPositive = true,
                Hints = positiveTopHints
            });

            topHints.Add(new TopHintInfo
            {
                IsPositive = false,
                Hints = negativeTopHints
            });
            _log.Info("AnalyticHome/Recomendation finished");
            return Ok(JsonConvert.SerializeObject(topHints));
        }                                                
    }
}