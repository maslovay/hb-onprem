using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using UserOperations.Services;
using UserOperations.Models.AnalyticModels;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using HBData;
using UserOperations.Utils;
using UserOperations.Providers;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticOfficeController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly IDBOperations _dbOperation;
        private readonly IRequestFilters _requestFilters;
        private readonly IAnalyticOfficeProvider _analyticOfficeProvider;

        public AnalyticOfficeController(
            IConfiguration config,
            ILoginService loginService,
            IDBOperations dbOperation,
            IRequestFilters requestFilters,
            IAnalyticOfficeProvider analyticOfficeProvider
            )
        {
            _config = config;
            _loginService = loginService;
            _dbOperation = dbOperation;
            _requestFilters = requestFilters;
            _analyticOfficeProvider = analyticOfficeProvider;
            // _log = log;
        }

        [HttpGet("Efficiency")]
        public IActionResult Efficiency([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);   
                var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                var sessions = _analyticOfficeProvider.GetSessionsInfo(prevBeg, endTime, companyIds, applicationUserIds, workerTypeIds);

                var sessionCur = sessions.Where(p => p.BegTime.Date >= begTime).ToList();
                var sessionOld = sessions.Where(p => p.BegTime.Date < begTime).ToList();
                var dialogues = _analyticOfficeProvider.GetDialoguesInfo(prevBeg, endTime, companyIds, applicationUserIds, workerTypeIds);                
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
             //   result.CorrelationLoadSatisfaction = satisfactionIndex != 0?  loadIndex / satisfactionIndex : 0;
                result.WorkloadDynamics += result.WorkloadValueAvg;
                result.DialoguesNumberAvgPerEmployee = (dialoguesCur.Count() != 0) ? dialoguesCur.GroupBy(p => p.BegTime.Date).Select(p => p.Count()).Average() / employeeCount : 0;
                result.DialoguesNumberAvgPerDayOffice = (dialoguesCur.Count() != 0) ? dialoguesCur.GroupBy(p => p.BegTime.Date).Select(p => p.Count()).Average() : 0;

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
                            _dbOperation.DialogueAvgPauseListInMinutes(sessionCur, dialoguesCur, begTime, endTime): null;
                     
                var sessTimeMinutes = _dbOperation.SessionTotalHours(sessionCur, begTime, endTime)*60;
                var pausesAmount = new{
                    Less_10 = pauseInMin?.Where(p => p <= 10).Count(),
                    Between_11_20 = pauseInMin?.Where(p => p > 10 && p <= 20).Count(),
                    Between_21_60 = pauseInMin?.Where(p => p > 20 && p <= 60).Count(),
                    More_60 = pauseInMin?.Where(p => p > 60).Count()
                };

                var pausesShareInSession = new{
                    Less_10 = sessTimeMinutes != 0? 100 *  pauseInMin?.Where(p => p <= 10).Sum() / sessTimeMinutes : 0,
                    Between_11_20 = sessTimeMinutes != 0? 100 * pauseInMin?.Where(p => p > 10 && p <= 20).Sum() / sessTimeMinutes : 0,
                    Between_21_60 = sessTimeMinutes != 0? 100 * pauseInMin?.Where(p => p > 20 && p <= 60).Sum() / sessTimeMinutes : 0,
                    More_60 = sessTimeMinutes != 0? 100 * pauseInMin?.Where(p => p > 60).Sum() / sessTimeMinutes : 0,
                    Load = sessTimeMinutes != 0? 100 * (sessTimeMinutes - pauseInMin?.Sum()) / sessTimeMinutes : 0
                };
                 var pausesInMinutes = new{
                    Less_10 = pauseInMin?.Where(p => p <= 10).Sum(),
                    Between_11_20 = pauseInMin?.Where(p => p > 10 && p <= 20).Sum(),
                    Between_21_60 = pauseInMin?.Where(p => p > 20 && p <= 60).Sum() ,
                    More_60 = pauseInMin?.Where(p => p > 60).Sum(),
                    Load = sessTimeMinutes - pauseInMin?.Sum()
                };

                var jsonToReturn = new Dictionary<string, object>
                {
                    ["Workload"] = result,
                    ["DiagramDialogDurationPause"] = diagramDialogDurationPause,
                    ["DiagramEmployeeWorked"] = diagramEmployeeWorked,
                    ["ClientTime"] = clientTime,
                    ["ClientDay"] = clientDay,
                    ["PausesAmount"] = pausesAmount,
                    ["PausesShare"] = pausesShareInSession,
                    ["PausesInMinutes"] = pausesInMinutes
                };
                return Ok(JsonConvert.SerializeObject(jsonToReturn));
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }     
        
    }  
}