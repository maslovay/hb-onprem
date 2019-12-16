using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using UserOperations.Services;
using UserOperations.Models.Get.AnalyticReportController;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using HBData;
using UserOperations.Utils.AnalyticReportUtils;
using UserOperations.Providers;
using UserOperations.Utils;

namespace UserOperations.Services
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticReportService : Controller
    {
        private readonly IConfiguration _config;
        private readonly LoginService _loginService;
        private readonly RequestFilters _requestFilters;
        private readonly IAnalyticReportProvider _analyticReportProvider;
        private readonly AnalyticReportUtils _analyticReportUtils;
        // private readonly ElasticClient _log;

        public AnalyticReportService(
            IConfiguration config,
            LoginService loginService,
            RequestFilters requestFilters,
            IAnalyticReportProvider analyticReportProvider,
            AnalyticReportUtils analyticReportUtils
            // ElasticClient log
            )
        {
            _config = config;
            _loginService = loginService;
            _requestFilters = requestFilters;
            _analyticReportProvider = analyticReportProvider;
            _analyticReportUtils = analyticReportUtils;
            // _log = log;
        }

        [HttpGet("ActiveEmployee")]
        public IActionResult ReportActiveEmployee([FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                // _log.Info("AnalyticReport/ActiveEmployee started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);
                var sessions = _analyticReportProvider.GetSessions(companyIds, applicationUserIds, workerTypeIds);
                // _log.Info("AnalyticReport/ActiveEmployee finished");
                return Ok(JsonConvert.SerializeObject(sessions));
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }

        [HttpGet("UserPartial")]
        public IActionResult ReportUserPartial([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                // _log.Info("AnalyticReport/UserPartial started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var employeeRole = _analyticReportProvider.GetEmployeeRoleId();
                var sessions = _analyticReportProvider.GetSessions(
                    begTime,
                    endTime,
                    employeeRole,
                    companyIds,
                    applicationUserIds,
                    workerTypeIds);
                var dialogues = _analyticReportProvider.GetDialogues(
                    begTime, 
                    endTime, 
                    companyIds, 
                    applicationUserIds, 
                    workerTypeIds);
                    //---USER WITHOUT SESSIONS---
                var userIds = sessions.Select(x => x.ApplicationUserId).Distinct().ToList();
                var usersToAdd = _analyticReportProvider.GetApplicationUsersToAdd(
                    endTime,
                    companyIds,
                    applicationUserIds,
                    workerTypeIds,
                    userIds,
                    userClaims,
                    employeeRole);
                var result = sessions
                    .GroupBy(p => p.ApplicationUserId)
                    .Select(p => new 
                    {
                        p.First().FullName,
                        ApplicationUserId = p.Key,
                        LoadIndexAverage = _analyticReportUtils.LoadIndex(p, dialogues, begTime, endTime),
                        PeriodInfo = p.GroupBy(q => q.BegTime.Date).Select(q => new ReportPartDayEmployeeInfo
                        {
                            Date = q.Key,
                            WorkingHours = _analyticReportUtils.Min(24, _analyticReportUtils.MaxDouble(_analyticReportUtils.SessionAverageHours(q), _analyticReportUtils.DialogueSumDuration(q, dialogues, p.Key))?? 0),
                            DialogueHours = _analyticReportUtils.Min(24, _analyticReportUtils.DialogueSumDuration(q, dialogues, p.Key)?? 0),
                            LoadIndex = 100 * _analyticReportUtils.LoadIndex(_analyticReportUtils.SessionAverageHours(q), _analyticReportUtils.DialogueSumDuration(q, dialogues, p.Key)),
                            DialogueCount = _analyticReportUtils.DialoguesCount(dialogues, p.Key, q.Key)
                        }).ToList()
                    }).ToList();

                var emptyUsers = usersToAdd.Select(p => new 
                    {
                        p.FullName,
                        ApplicationUserId = p.Id,
                        LoadIndexAverage = (double?)0,
                        PeriodInfo =  new List<ReportPartDayEmployeeInfo>()
                        }).ToList();
                   
                result.AddRange(emptyUsers);
                // _log.Info("AnalyticReport/UserPartial finished");
                return Ok(JsonConvert.SerializeObject(result));

            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }

        [HttpGet("UserFull")]
        public IActionResult ReportUserFull([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                // _log.Info("AnalyticReport/UserFull started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);    
                var employeeRole = _analyticReportProvider.GetEmployeeRoleId();
                System.Console.WriteLine($"employeeRole: {employeeRole}");
                var sessions = _analyticReportProvider.GetSessions(
                    begTime,
                    endTime,
                    employeeRole,
                    companyIds,
                    applicationUserIds,
                    workerTypeIds);
                System.Console.WriteLine($"sessions: {sessions.Count}");
                var dialogues = _analyticReportProvider.GetDialoguesWithWorkerType(
                    begTime,
                    endTime,
                    employeeRole,
                    companyIds,
                    applicationUserIds,
                    workerTypeIds
                );
                System.Console.WriteLine($"dialogues: {dialogues.Count}");
                var result = new List<ReportFullPeriodInfo>();

                foreach (var date in dialogues.Select(p => p.BegTime.Date).Distinct().ToList())
                {
                    foreach (var applicationUserId in dialogues.Select(p => p.ApplicationUserId).Distinct().ToList())
                    {

                        var userInfo = new ReportFullPeriodInfo();
                        var dialoguesUser = dialogues.Where(p => p.ApplicationUserId == applicationUserId && p.BegTime.Date == date).ToList();
                        if (dialoguesUser.Any())
                        {                          
                            var begDate = Convert.ToDateTime(date);
                            var endDate = Convert.ToDateTime(date).AddDays(1);
                            userInfo.ApplicationUserId = applicationUserId;
                            userInfo.Date = Convert.ToDateTime(date);
                            userInfo.FullName = dialoguesUser.FirstOrDefault().FullName;
                            userInfo.WorkerType = dialoguesUser.FirstOrDefault().WorkerType;
                            userInfo.Load = _analyticReportUtils.LoadIndex(sessions, dialoguesUser, applicationUserId, Convert.ToDateTime(date), begDate, endDate);
                            userInfo.DialoguesTime = _analyticReportUtils.DialogueSumDuration(dialoguesUser, begDate, endDate);
                            userInfo.SessionTime = _analyticReportUtils.SessionAverageHours(sessions, applicationUserId, Convert.ToDateTime(date), begDate, endDate);
                            userInfo.PeriodInfo = _analyticReportUtils.TimeTable(sessions, dialoguesUser, applicationUserId, Convert.ToDateTime(date));

                            result.Add(userInfo);
                        }
                    }
                }
                System.Console.WriteLine($"result: {result.Count}");
                // _log.Info("AnalyticReport/UserFull finished");
                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }
    }
}