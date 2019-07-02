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

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticOfficeController : Controller
    {
        private readonly IConfiguration _config;        
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly DBOperations _dbOperation;
        private readonly RequestFilters _requestFilters;
        // private readonly ElasticClient _log;

        public AnalyticOfficeController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            DBOperations dbOperation,
            RequestFilters requestFilters
            // ElasticClient log
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _dbOperation = dbOperation;
            _requestFilters = requestFilters;
            // _log = log;
        }

        [HttpGet("Efficiency")]
        public IActionResult Efficiency([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                // _log.Info("AnalyticOffice/Efficiency started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);   
                var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                var sessions = _context.Sessions
                    .Include(p => p.ApplicationUser)
                    .Where(p => p.BegTime >= prevBeg
                            && p.EndTime <= endTime
                            && p.StatusId == 7
                            && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                    .Select(p => new SessionInfo
                    {
                        ApplicationUserId = p.ApplicationUserId,
                        BegTime = p.BegTime,
                        EndTime = p.EndTime
                    }).ToList();

                var sessionCur = sessions.Where(p => p.BegTime.Date >= begTime).ToList();
                var sessionOld = sessions.Where(p => p.BegTime.Date < begTime).ToList();
                var dialogues = _context.Dialogues
                    .Include(p => p.ApplicationUser)
                    .Where(p => p.BegTime >= prevBeg
                            && p.EndTime <= endTime
                            && p.StatusId == 3
                            && p.InStatistic == true
                            && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                    .Select(p => new DialogueInfo
                    {
                        DialogueId = p.DialogueId,
                        ApplicationUserId = p.ApplicationUserId,
                        BegTime = p.BegTime,
                        EndTime = p.EndTime,
                        FullName = p.ApplicationUser.FullName,
                        SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal
                    }).ToList();

                var dialoguesCur = dialogues.Where(p => p.BegTime >= begTime).ToList();
                var dialoguesOld = dialogues.Where(p => p.BegTime < begTime).ToList();

                var result = new EfficiencyDashboardInfoNew
                {
                    WorkloadValueAvg = _dbOperation.LoadIndex(sessionCur, dialoguesCur, begTime, endTime),
                    WorkloadDynamics = -_dbOperation.LoadIndex(sessionOld, dialoguesOld, prevBeg, begTime),
                    DialoguesCount = _dbOperation.DialoguesCount(dialoguesCur),
                    AvgWorkingTime = _dbOperation.SessionAverageHours(sessionCur, begTime, endTime),
                    AvgDurationDialogue = _dbOperation.DialogueAverageDuration(dialoguesCur, begTime, endTime),
                    BestEmployee = _dbOperation.BestEmployeeLoad(dialoguesCur, sessionCur, begTime, endTime),
                };
                var satisfactionIndex = _dbOperation.SatisfactionIndex(dialoguesCur);
                var loadIndex = _dbOperation.LoadIndex(sessionCur, dialoguesCur, begTime, endTime.AddDays(1));
                var employeeCount = _dbOperation.EmployeeCount(dialoguesCur);
                result.CorrelationLoadSatisfaction = satisfactionIndex != 0?  loadIndex / satisfactionIndex : 0;
                result.WorkloadDynamics += result.WorkloadValueAvg;
                result.DialoguesNumberAvgPerEmployee = (dialoguesCur.Count() != 0) ? dialoguesCur.GroupBy(p => p.BegTime.Date).Select(p => p.Count()).Average() / employeeCount : 0;
 
                var diagramDialogDurationPause = sessionCur
                .GroupBy(p => p.BegTime.Date)
                .Select(p => new
                {
                    Day = p.Key.ToString(),
                    AvgDialogue = _dbOperation
                        .DialogueAverageDuration(
                            dialogues.Where(x => x.BegTime >= p.Min(s => s.BegTime) && x.EndTime < p.Max(s => s.EndTime)).ToList(),
                            p.Min(s => s.BegTime),
                            p.Max(s => s.EndTime)),
                    AvgPause = _dbOperation
                        .DialogueAveragePause(
                            p.ToList(), 
                            dialogues.Where(x => x.BegTime >= p.Min(s => s.BegTime) && x.EndTime < p.Max(s => s.EndTime)).ToList(),
                            p.Min(s => s.BegTime),
                            p.Max(s => s.EndTime)),
                    AvgWorkLoad  = _dbOperation
                        .LoadIndex(
                            p.ToList(), 
                            dialogues.Where(x => x.BegTime >= p.Min(s => s.BegTime) && x.EndTime < p.Max(s => s.EndTime)).ToList(),
                            p.Min(s => s.BegTime),
                            p.Max(s => s.EndTime))                      
                }).    
                ToList();

                var optimalLoad = 0.7;
                var employeeWorked = sessionCur
                .GroupBy(p => p.BegTime.Date)
                .Select(p => new
                {
                    Day = p.Key.ToString(),
                    EmployeeCount = _dbOperation
                        .EmployeeCount(
                            dialogues.Where(x => x.BegTime >= p.Min(s => s.BegTime) && x.EndTime < p.Max(s => s.EndTime)).ToList()
                           ),
                    LoadIndex = _dbOperation
                        .LoadIndex(
                            p.ToList(), 
                            dialogues.Where(x => x.BegTime >= p.Min(s => s.BegTime) && x.EndTime < p.Max(s => s.EndTime)).ToList(),
                            p.Min(s => s.BegTime),
                            p.Max(s => s.EndTime))                  
                }).    
                ToList();

                var diagramEmployeeWorked = employeeWorked.Select(
                    p => new {
                        p.Day,
                        p.EmployeeCount,
                        EmployeeOptimalCount = (p.LoadIndex != null & p.LoadIndex != 0) ?
                        (Int32?)(Math.Ceiling((double)(p.EmployeeCount * optimalLoad / p.LoadIndex))) : null
                    }
                );

                 var clientTime = dialogues
                    .GroupBy(p => p.BegTime.Hour)
                    .Select(p => new EfficiencyLoadClientTimeInfo
                    {
                        Time = $"{p.Key}:00",
                        ClientCount = p.GroupBy(q => q.BegTime.Date)
                            .Select(q => new { DialoguesCount = q.Select(r => r.DialogueId).Distinct().Count() })
                            .Average(q => q.DialoguesCount)
                    }).ToList();

                var clientDay = dialogues?
                    .GroupBy(p => p.BegTime.DayOfWeek)
                    .Select(p => new EfficiencyLoadClientDayInfo
                    {
                        Day = p.Key.ToString(),
                        ClientCount = p.GroupBy(q => q.BegTime.Date)
                            .Select(q => new { DialoguesCount = q.Select(r => r.DialogueId).Distinct().Count() })
                            .Average(q => q.DialoguesCount)
                    }).ToList();

                var pauseInMin = (sessionCur.Count() != 0 && dialoguesCur.Count() != 0) ?
                            _dbOperation.DialogueAvgPauseListInMinutes(sessionCur, dialoguesCur, sessionCur.Min(p => p.BegTime), sessionCur.Max(p => p.EndTime)): null;
                     
                var sessTimeMinutes = _dbOperation.SessionTotalHours(sessionCur, begTime, endTime)*60;

                var pausesAmount = new{
                    Less_10 = pauseInMin?.Where(p => p <= 10).Count(),
                    Between_11_20 = pauseInMin?.Where(p => p > 10 && p < 20).Count(),
                    Between_21_60 = pauseInMin?.Where(p => p > 20 && p < 60).Count(),
                    More_60 = pauseInMin?.Where(p => p >= 60).Count()
                };

                    var pausesAvgValue = new{
                    Less_10 = sessTimeMinutes != 0? 100 *  pauseInMin?.Where(p => p <= 10).Sum() / sessTimeMinutes : 0,
                    Between_11_20 = sessTimeMinutes != 0? 100 * pauseInMin?.Where(p => p > 10 && p < 20).Sum() / sessTimeMinutes : 0,
                    Between_21_60 = sessTimeMinutes != 0? 100 * pauseInMin?.Where(p => p > 20 && p < 60).Sum() / sessTimeMinutes : 0,
                    More_60 = sessTimeMinutes != 0? 100 * pauseInMin.Where(p => p >= 60).Sum() / sessTimeMinutes : 0,
                    Sessions = sessTimeMinutes != 0? 100 * (sessTimeMinutes - pauseInMin?.Sum()) / sessTimeMinutes : 0
                };

              
                var jsonToReturn = new Dictionary<string, object>();
                jsonToReturn["Workload"] = result;
                jsonToReturn["DiagramDialogDurationPause"] = diagramDialogDurationPause;
                jsonToReturn["DiagramEmployeeWorked"] = diagramEmployeeWorked;
                jsonToReturn["ClientTime"] = clientTime;
                jsonToReturn["ClientDay"] = clientDay;
                jsonToReturn["Pauses"] = pausesAmount;
                jsonToReturn["PausesShare"] = pausesAvgValue;
                // _log.Info("AnalyticOffice/Efficiency finished");
                return Ok(JsonConvert.SerializeObject(jsonToReturn));
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }     
        
    }  
}