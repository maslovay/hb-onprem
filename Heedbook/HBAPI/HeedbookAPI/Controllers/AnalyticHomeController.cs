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

                // to do: add authorization

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

                var dialoguesCur = dialogues.Where(p => p.BegTime >= begTime).ToList();
                var dialoguesOld = dialogues.Where(p => p.BegTime < begTime).ToList();


                var companyIndusrtys = _context.Companys
                        .Include(p => p.CompanyIndustry)
                        .Where(p => (!companyIds.Any() || companyIds.Contains(p.CompanyId)))
                        .Select(p => new
                        {
                            LoadIndex = p.CompanyIndustry.LoadIndex,
                            SatisfactionIndex = p.CompanyIndustry.SatisfactionIndex,
                            CrossIndex = p.CompanyIndustry.CrossSalesIndex
                        })
                        .FirstOrDefault();

                var result =  new DashboardInfo()
                {
                    EfficiencyIndex = _dbOperation.EfficiencyIndex(sessionCur, dialoguesCur, begTime, endTime.AddDays(1)),
                    EfficiencyIndexDelta = -_dbOperation.EfficiencyIndex(sessionOld, dialoguesOld, prevBeg, begTime),
                    EfficiencyIndexPeak = 0,
                    SatisfactionIndex = _dbOperation.SatisfactionIndex(dialoguesCur),
                    SatisfactionIndexDelta = - _dbOperation.SatisfactionIndex(dialoguesOld),
                    SatisfactionIndexDeltaBranch = 0,
                    LoadIndex = _dbOperation.LoadIndex(sessionCur, dialoguesCur, begTime, endTime.AddDays(1)),
                    LoadIndexDelta = - _dbOperation.LoadIndex(sessionOld, dialoguesOld, prevBeg, endTime),
                    LoadIndexDeltaBranch = 0,
                    CrossIndex = _dbOperation.CrossIndex(dialoguesCur),
                    CrossIndexDelta = - _dbOperation.CrossIndex(dialoguesOld),
                    CrossIndexDeltaBranch = 0,
                    DialoguesCount = _dbOperation.DialoguesCount(dialoguesCur),
                    DialoguesCountDelta = -_dbOperation.DialoguesCount(dialoguesOld),
                    EmployeeCount = _dbOperation.EmployeeCount(dialoguesCur),
                    EmployeeCountDelta = - _dbOperation.EmployeeCount(dialoguesOld),
                    BestEmployee = _dbOperation.BestEmployee(dialoguesCur, sessionCur, begTime, endTime.AddDays(1)),
                    BestEmployeeEfficiency = _dbOperation.BestEmployeeEfficiency(dialoguesCur, sessionCur, begTime, endTime.AddDays(1)),
                    BestProgressiveEmployee = _dbOperation.BestProgressiveEmployee(dialogues, begTime),
                    BestProgressiveEmployeeDelta = _dbOperation.BestProgressiveEmployeeDelta(dialogues, begTime),
                    SatisfactionDialogueDelta = _dbOperation.SatisfactionDialogueDelta(dialogues)
                };

                result.EmployeeCountDelta += result.EmployeeCount;
                result.CrossIndexDelta += result.CrossIndex;
                result.LoadIndexDelta += result.LoadIndex;
                result.EfficiencyIndexDelta += result.EfficiencyIndex;
                result.SatisfactionIndexDelta += result.SatisfactionIndex;
                result.DialoguesCountDelta += result.DialoguesCount;
                result.LoadIndexDeltaBranch = (result.LoadIndex != 0 & result.LoadIndex != null) ?
                    result.LoadIndex - companyIndusrtys.LoadIndex: result.LoadIndex;
                result.SatisfactionIndexDeltaBranch = (result.SatisfactionIndex != 0 & result.SatisfactionIndex != null) ?
                    result.SatisfactionIndex - companyIndusrtys.SatisfactionIndex : 0;
                result.CrossIndexDeltaBranch = (result.CrossIndex != 0 & result.CrossIndex != null) ?
                    result.CrossIndex - companyIndusrtys.CrossIndex: 0;
                result.CrossIndexBranch = companyIndusrtys.CrossIndex;
                result.LoadIndexBranch = companyIndusrtys.LoadIndex;
                result.SatisfactionIndexBranch = companyIndusrtys.SatisfactionIndex;


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
            // to do: add authorization

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