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
    public class AnalyticReportController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly DBOperations _dbOperation;
        private readonly RequestFilters _requestFilters;
        private readonly ElasticClient _log;

        public AnalyticReportController(
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

        [HttpGet("ActiveEmployee")]
        public IActionResult ReportActiveEmployee([FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                _log.Info("AnalyticReport/ActiveEmployee started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);
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
                _log.Info("AnalyticReport/ActiveEmployee finished");
                return Ok(JsonConvert.SerializeObject(sessions));
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }

        [HttpGet("UserPartial")]
        public IActionResult ReportUserPartial([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                _log.Info("AnalyticReport/UserPartial started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);

                var sessions = _context.Sessions
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.ApplicationUser.WorkerType)
                    .Where(p => p.BegTime >= begTime
                            && p.EndTime <= endTime
                            && p.StatusId == 7
                            && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                    .Select(p => new SessionInfo
                    {
                        ApplicationUserId = p.ApplicationUserId,
                        BegTime = p.BegTime,
                        EndTime = p.EndTime,
                        FullName = p.ApplicationUser.FullName,
                        // WorkerType = p.ApplicationUser.WorkerType.WorkerTypeName,
                    })
                    .ToList();

                var typeIdCross = _context.PhraseTypes.Where(p => p.PhraseTypeText == "Cross").Select(p => p.PhraseTypeId).First();

                var dialogues = _context.Dialogues
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.ApplicationUser.WorkerType)
                    .Include(p => p.DialogueClientSatisfaction)
                    .Include(p => p.DialoguePhraseCount)
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

                var result = sessions
                    .GroupBy(p => p.ApplicationUserId)
                    .Select(p => new 
                    {
                        FullName = p.First().FullName,
                        ApplicationUserId = p.Key,
                        // WorkerType = p.First().WorkerType,
                        LoadIndexAverage = _dbOperation.LoadIndex(p, dialogues, begTime, endTime),
                        PeriodInfo = p.GroupBy(q => q.BegTime.Date).Select(q => new ReportPartDayEmployeeInfo
                        {
                            Date = q.Key,
                            WorkingHours = _dbOperation.MaxDouble(_dbOperation.SessionAverageHours(q), _dbOperation.DialogueSumDuration(q, dialogues, p.Key)),
                            DialogueHours = _dbOperation.DialogueSumDuration(q, dialogues, p.Key),
                            LoadIndex = _dbOperation.LoadIndex(q, dialogues, p.Key),
                            DialogueCount = _dbOperation.DialoguesCount(dialogues, p.Key, q.Key)
                        }).ToList()
                    }).ToList();
                _log.Info("AnalyticReport/UserPartial finished");
                return Ok(JsonConvert.SerializeObject(result));

            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }

        [HttpGet("UserFull")]
        public IActionResult ReportUserFull([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                _log.Info("AnalyticReport/UserFull started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);       
                Console.WriteLine("-----------------------------1----------------------");
                var sessions = _context.Sessions
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.ApplicationUser.WorkerType)
                    .Where(p => p.BegTime >= begTime
                            && p.EndTime <= endTime
                            && p.StatusId == 7
                            && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                    .Select(p => new SessionInfo
                    {
                        ApplicationUserId = p.ApplicationUserId,
                        BegTime = p.BegTime,
                        EndTime = p.EndTime,
                        FullName = p.ApplicationUser.FullName,
                        // WorkerType = p.ApplicationUser.WorkerType.WorkerTypeName,
                    })
                    .ToList();
                Console.WriteLine("-----------------------------2----------------------");
                var typeIdCross = _context.PhraseTypes.Where(p => p.PhraseTypeText == "Cross").Select(p => p.PhraseTypeId).First();

                var dialogues = _context.Dialogues
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.ApplicationUser.WorkerType)
                    .Include(p => p.DialogueClientSatisfaction)
                    .Include(p => p.DialoguePhraseCount)
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
                        WorkerType = p.ApplicationUser.WorkerType.WorkerTypeName,
                        FullName = p.ApplicationUser.FullName,
                        BegTime = p.BegTime,
                        EndTime = p.EndTime,
                        SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                        // Date = DbFunctions.TruncateTime(p.BegTime)
                    })
                    .ToList();
                Console.WriteLine("-----------------------------3----------------------");
                var result = new List<ReportFullPeriodInfo>();

                foreach (var date in dialogues.Select(p => p.BegTime.Date).Distinct().ToList())
                {
                    Console.WriteLine($"----------------------{date}-----------------------------");
                    foreach (var applicationUserId in dialogues.Select(p => p.ApplicationUserId).Distinct().ToList())
                    {

                        var userInfo = new ReportFullPeriodInfo();
                        var dialoguesUser = dialogues.Where(p => p.ApplicationUserId == applicationUserId && p.BegTime.Date == date).ToList();
                        if (dialoguesUser.Any())
                        {                          
                              //  Console.WriteLine($"----------------------{applicationUserId}-----------------------------");
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
                Console.WriteLine("-----------------------------4----------------------");
                _log.Info("AnalyticReport/UserFull finished");
                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occurred {e}");
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