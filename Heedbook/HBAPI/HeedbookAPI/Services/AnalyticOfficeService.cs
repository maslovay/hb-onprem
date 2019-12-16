using System;
using System.Collections.Generic;
using System.Linq;
using HBData;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Providers;
using UserOperations.Utils;
using UserOperations.Models.Get.AnalyticOfficeController;
using UserOperations.Utils.AnalyticOfficeUtils;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace UserOperations.Services
{
    public class AnalyticOfficeService : Controller
    {  
        private readonly LoginService _loginService;
        private readonly RequestFilters _requestFilters;
        private readonly IAnalyticOfficeProvider _analyticOfficeProvider;
        private readonly AnalyticOfficeUtils _analyticOfficeUtils;
        private readonly IConfiguration _config;
        // private readonly ElasticClient _log;

        public AnalyticOfficeService(
            LoginService loginService,
            RequestFilters requestFilters,
            IAnalyticOfficeProvider analyticOfficeProvider,
            AnalyticOfficeUtils analyticOfficeUtils,
            IConfiguration config
            // ElasticClient log
            )
        {
            _loginService = loginService;
            _requestFilters = requestFilters;
            _analyticOfficeProvider = analyticOfficeProvider;
            _analyticOfficeUtils = analyticOfficeUtils;
            _config = config;
            // _log = log;
        }
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
                // _log.Info("AnalyticOffice/Efficiency started");
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
                    WorkloadValueAvg = _analyticOfficeUtils.LoadIndex(sessionCur, dialoguesCur, begTime, endTime),
                    WorkloadDynamics = -_analyticOfficeUtils.LoadIndex(sessionOld, dialoguesOld, prevBeg, begTime),
                    DialoguesCount = _analyticOfficeUtils.DialoguesCount(dialoguesCur),
                    AvgWorkingTime = _analyticOfficeUtils.SessionAverageHours(sessionCur, begTime, endTime),
                    AvgDurationDialogue = _analyticOfficeUtils.DialogueAverageDuration(dialoguesCur, begTime, endTime),
                    BestEmployee = _analyticOfficeUtils.BestEmployeeLoad(dialoguesCur, sessionCur, begTime, endTime),
                };
                var satisfactionIndex = _analyticOfficeUtils.SatisfactionIndex(dialoguesCur);
                var loadIndex = _analyticOfficeUtils.LoadIndex(sessionCur, dialoguesCur, begTime, endTime.AddDays(1));
                var employeeCount = _analyticOfficeUtils.EmployeeCount(dialoguesCur);
             //   result.CorrelationLoadSatisfaction = satisfactionIndex != 0?  loadIndex / satisfactionIndex : 0;
                result.WorkloadDynamics += result.WorkloadValueAvg;
                result.DialoguesNumberAvgPerEmployee = (dialoguesCur.Count() != 0) ? dialoguesCur.GroupBy(p => p.BegTime.Date).Select(p => p.Count()).Average() / employeeCount : 0;
                result.DialoguesNumberAvgPerDayOffice = (dialoguesCur.Count() != 0) ? dialoguesCur.GroupBy(p => p.BegTime.Date).Select(p => p.Count()).Average() : 0;

                var diagramDialogDurationPause = sessionCur
                .GroupBy(p => p.BegTime.Date)
                .Select(p => new
                {
                    Day = p.Key.ToString(),
                    AvgDialogue = _analyticOfficeUtils
                        .DialogueAverageDuration(
                            dialogues.Where(x => x.BegTime >= p.Min(s => s.BegTime) && x.EndTime < p.Max(s => s.EndTime)).ToList(),
                            p.Min(s => s.BegTime),
                            p.Max(s => s.EndTime)),
                    AvgPause = _analyticOfficeUtils
                        .DialogueAveragePause(
                            p.ToList(), 
                            dialogues.Where(x => x.BegTime >= p.Min(s => s.BegTime) && x.EndTime < p.Max(s => s.EndTime)).ToList(),
                            p.Min(s => s.BegTime),
                            p.Max(s => s.EndTime)),
                    AvgWorkLoad  = _analyticOfficeUtils
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
                    EmployeeCount = _analyticOfficeUtils
                        .EmployeeCount(
                            dialogues.Where(x => x.BegTime >= p.Min(s => s.BegTime) && x.EndTime < p.Max(s => s.EndTime)).ToList()
                           ),
                    LoadIndex = _analyticOfficeUtils
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
                            _analyticOfficeUtils.DialogueAvgPauseListInMinutes(sessionCur, dialoguesCur, begTime, endTime): null;
                     
                var sessTimeMinutes = _analyticOfficeUtils.SessionTotalHours(sessionCur, begTime, endTime)*60;
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
  
                var jsonToReturn = new Dictionary<string, object>();
                jsonToReturn["Workload"] = result;
                jsonToReturn["DiagramDialogDurationPause"] = diagramDialogDurationPause;
                jsonToReturn["DiagramEmployeeWorked"] = diagramEmployeeWorked;
                jsonToReturn["ClientTime"] = clientTime;
                jsonToReturn["ClientDay"] = clientDay;
                jsonToReturn["PausesAmount"] = pausesAmount;
                jsonToReturn["PausesShare"] = pausesShareInSession;
                jsonToReturn["PausesInMinutes"] = pausesInMinutes;
                // _log.Info("AnalyticOffice/Efficiency finished");
                return Ok(JsonConvert.SerializeObject(jsonToReturn));
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                System.Console.WriteLine(e.Message);
                return BadRequest(e);
            }
        }     
        
    }  
}