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

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticReportController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly IDBOperations _dbOperation;
        private readonly IRequestFilters _requestFilters;
        // private readonly ElasticClient _log;

        public AnalyticReportController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            IDBOperations dbOperation,
            IRequestFilters requestFilters
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
                var sessions = _context.Sessions
                    .Include(p => p.ApplicationUser)
                    .Where(p => p.StatusId == 6
                            && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                    .Select(p => new SessionInfo
                    {
                        ApplicationUserId = p.ApplicationUserId,
                        FullName = p.ApplicationUser.FullName
                    })
                    .ToList().Distinct().ToList();
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

                var employeeRole = _context.Roles.FirstOrDefault(x =>x.Name == "Employee").Id;
                var sessions = _context.Sessions
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.ApplicationUser.UserRoles)
                    .Include(p => p.ApplicationUser.WorkerType)
                    .Where(p => p.BegTime >= begTime
                            && p.EndTime <= endTime
                            && p.StatusId == 7
                            && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
                            && (p.ApplicationUser.UserRoles.Any(x => x.RoleId == employeeRole)))
                    .Select(p => new SessionInfo
                    {
                        ApplicationUserId = p.ApplicationUserId,
                        BegTime = p.BegTime,
                        EndTime = p.EndTime,
                        FullName = p.ApplicationUser.FullName,
                        // WorkerType = p.ApplicationUser.WorkerType.WorkerTypeName,
                    })
                    .ToList();

          //      var typeIdCross = _context.PhraseTypes.Where(p => p.PhraseTypeText == "Cross").Select(p => p.PhraseTypeId).First();

                var dialogues = _context.Dialogues
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.ApplicationUser.WorkerType)
                    .Include(p => p.DialogueClientSatisfaction)
                    .Include(p => p.DialoguePhrase)
                    .Where(p => p.BegTime >= begTime
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
                        // WorkerType = p.ApplicationUser.WorkerType.WorkerTypeName,
                        FullName = p.ApplicationUser.FullName,
                        BegTime = p.BegTime,
                        EndTime = p.EndTime,
                        SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal
                    })
                    .ToList();
                    //---USER WITHOUT SESSIONS---
                var userIds = sessions.Select(x => x.ApplicationUserId).Distinct().ToList();
                
                var usersToAdd = _context.ApplicationUsers
                    .Include(x =>x.UserRoles)
                    .Where(p => 
                        p.CreationDate <= endTime
                        && p.StatusId == 3
                        &&(!companyIds.Any() || companyIds.Contains((Guid)p.CompanyId))
                        && (!applicationUserIds.Any() || applicationUserIds.Contains(p.Id))
                        && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.WorkerTypeId))
                        && !userIds.Contains(p.Id)
                        && p.Id != Guid.Parse(userClaims["applicationUserId"])
                        && (p.UserRoles.Any(x => x.RoleId == employeeRole))
                    ).ToList();

                var result = sessions
                    .GroupBy(p => p.ApplicationUserId)
                    .Select(p => new 
                    {
                        p.First().FullName,
                        ApplicationUserId = p.Key,
                        LoadIndexAverage = _dbOperation.LoadIndex(p, dialogues, begTime, endTime),
                        PeriodInfo = p.GroupBy(q => q.BegTime.Date).Select(q => new ReportPartDayEmployeeInfo
                        {
                            Date = q.Key,
                            WorkingHours = _dbOperation.Min(24, _dbOperation.MaxDouble(_dbOperation.SessionAverageHours(q), _dbOperation.DialogueSumDuration(q, dialogues, p.Key))?? 0),
                            DialogueHours = _dbOperation.Min(24, _dbOperation.DialogueSumDuration(q, dialogues, p.Key)?? 0),
                            LoadIndex = 100 * _dbOperation.LoadIndex(_dbOperation.SessionAverageHours(q), _dbOperation.DialogueSumDuration(q, dialogues, p.Key)),
                            DialogueCount = _dbOperation.DialoguesCount(dialogues, p.Key, q.Key)
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
                var employeeRole = _context.Roles.FirstOrDefault(x =>x.Name == "Employee").Id;

                var sessions = _context.Sessions
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.ApplicationUser.UserRoles)
                    .Include(p => p.ApplicationUser.WorkerType)
                    .Where(p => p.BegTime >= begTime
                            && p.EndTime <= endTime
                            && p.StatusId == 7
                            && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
                            && (p.ApplicationUser.UserRoles.Any(x => x.RoleId == employeeRole)))
                    .Select(p => new SessionInfo
                    {
                        ApplicationUserId = p.ApplicationUserId,
                        BegTime = p.BegTime,
                        EndTime = p.EndTime,
                        FullName = p.ApplicationUser.FullName,
                        // WorkerType = p.ApplicationUser.WorkerType.WorkerTypeName,
                    })
                    .ToList();
        //        var typeIdCross = _context.PhraseTypes.Where(p => p.PhraseTypeText == "Cross").Select(p => p.PhraseTypeId).First();

                var dialogues = _context.Dialogues
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.ApplicationUser.UserRoles)
                    .Include(p => p.ApplicationUser.WorkerType)
                    .Include(p => p.DialogueClientSatisfaction)
                    .Include(p => p.DialoguePhrase)
                    .Where(p => p.BegTime >= begTime
                            && p.EndTime <= endTime
                            && p.StatusId == 3
                            && p.InStatistic == true
                            && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
                            && (p.ApplicationUser.UserRoles.Any(x => x.RoleId == employeeRole)))
                    .Select(p => new DialogueInfo
                    {
                        DialogueId = p.DialogueId,
                        ApplicationUserId = p.ApplicationUserId,
                        WorkerType = p.ApplicationUser.WorkerType.WorkerTypeName,
                        FullName = p.ApplicationUser.FullName,
                        BegTime = p.BegTime,
                        EndTime = p.EndTime,
                        SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                        // Date = DbFunctions.TruncateTime(p.BegTime)
                    })
                    .ToList();
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
                                userInfo.Load = _dbOperation.LoadIndex(sessions, dialoguesUser, applicationUserId, Convert.ToDateTime(date), begDate, endDate);
                                userInfo.DialoguesTime = _dbOperation.DialogueSumDuration(dialoguesUser, begDate, endDate);
                                userInfo.SessionTime = _dbOperation.SessionAverageHours(sessions, applicationUserId, Convert.ToDateTime(date), begDate, endDate);
                                userInfo.PeriodInfo = _dbOperation.TimeTable(sessions, dialoguesUser, applicationUserId, Convert.ToDateTime(date));

                                result.Add(userInfo);
                        }
                    }
                }
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

    public class ReportPartDayEmployeeInfo
    {
        public DateTime Date;
        public double? WorkingHours;
        public double? DialogueHours;
        public double? LoadIndex;
        public int? DialogueCount;
    }

    public class ReportPartPeriodEmployeeInfo
    {
        public string FullName;
        public Guid ApplicationUserId;
        // public string WorkerType;
        public double? LoadIndexAverage;
        public List<ReportPartDayEmployeeInfo> PeriodInfo;
    }
}