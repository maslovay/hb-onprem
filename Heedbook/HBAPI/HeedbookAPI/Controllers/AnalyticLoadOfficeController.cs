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
    public class AnalyticLoadOfficeController : Controller
    {
        private readonly IConfiguration _config;        
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly DBOperations _dbOperation;
        private readonly RequestFilters _requestFilters;
        private readonly ElasticClient _log;

        public AnalyticLoadOfficeController(
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

        [HttpGet("EfficiencyDashboardNew")]
        public IActionResult EfficiencyDashboardNew([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end,
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                _log.Info("AnalyticEfficiency/EfficiencyDashboard started");
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
                        FullName = p.ApplicationUser.UserName,
                        SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal
                    }).ToList();

                var dialoguesCur = dialogues.Where(p => p.BegTime >= begTime).ToList();
                var dialoguesOld = dialogues.Where(p => p.BegTime < begTime).ToList();

                var result = new EfficiencyDashboardInfoNew
                {
                    LoadIndex = _dbOperation.LoadIndex(sessionCur, dialoguesCur, begTime, endTime),
                    LoadIndexDelta = -_dbOperation.LoadIndex(sessionOld, dialoguesOld, prevBeg, begTime),
                    DialoguesCount = _dbOperation.DialoguesCount(dialoguesCur),
                    WorkingHours = _dbOperation.SessionAverageHours(sessionCur, begTime, endTime),
                    DialogueAverageDuration = _dbOperation.DialogueAverageDuration(dialoguesCur, begTime, endTime),
                    BestEmployee = _dbOperation.BestEmployee(dialoguesCur)
                };
                var satisfactionIndex = _dbOperation.SatisfactionIndex(dialoguesCur);
                var loadIndex = _dbOperation.LoadIndex(sessionCur, dialoguesCur, begTime, endTime.AddDays(1));
                var employeeCount = _dbOperation.EmployeeCount(dialoguesCur);
                result.CorrelationLoadSatisfaction = satisfactionIndex != 0?  loadIndex / satisfactionIndex : 0;
                result.LoadIndexDelta += result.LoadIndex;
                result.DialoguesPerEmployee = (dialoguesCur.Count() != 0) ? dialoguesCur.GroupBy(p => p.BegTime.Date).Select(p => p.Count()).Average() / employeeCount : 0;

                var diagramDialogDurationPause = sessionCur
                .GroupBy(p => p.BegTime.Date)
                .Select(p => new
                {
                    Day = p.Key.ToString(),
                    avgDialogue = _dbOperation
                        .DialogueAverageDuration(
                            dialogues.Where(x => x.BegTime >= p.Min(s => s.BegTime) && x.EndTime < p.Max(s => s.EndTime)).ToList(),
                            p.Min(s => s.BegTime),
                            p.Max(s => s.EndTime)),
                    avgPause = _dbOperation
                        .DialogueAveragePause(
                            p.ToList(), 
                            dialogues.Where(x => x.BegTime >= p.Min(s => s.BegTime) && x.EndTime < p.Max(s => s.EndTime)).ToList(),
                            p.Min(s => s.BegTime),
                            p.Max(s => s.EndTime))                  
                }).    
                ToList();

                var optimalLoad = 0.6;
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
                var clientDay = dialogues
                    .GroupBy(p => p.BegTime.DayOfWeek)
                    .Select(p => new EfficiencyLoadClientDayInfo
                    {
                        Day = p.Key.ToString(),
                        ClientCount = p.GroupBy(q => q.BegTime.Date)
                            .Select(q => new { DialoguesCount = q.Select(r => r.DialogueId).Distinct().Count() })
                            .Average(q => q.DialoguesCount)
                    }).ToList();
              
                var jsonToReturn = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(result));
                jsonToReturn["DiagramDialogDurationPause"] = diagramDialogDurationPause;
                jsonToReturn["DiagramEmployeeWorked"] = diagramEmployeeWorked;
                jsonToReturn["ClientTime"] = clientTime;
                jsonToReturn["ClientDay"] = clientDay;
                _log.Info("AnalyticEfficiency/EfficiencyDashboard finished");
                return Ok(JsonConvert.SerializeObject(jsonToReturn));
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }     
        
    }

class EfficiencyDashboardInfoNew
    {
        public double? LoadIndex;
        public double? LoadIndexDelta;
        public int? DialoguesCount;
       // public double? EmployeeCount;
        public double? DialoguesPerEmployee;
       // public int? EmployeeOptimalCount;
        public double? WorkingHours;
       // public double? WorkingHoursDelta;
       // public double? DialogueAveragePause;
        public double? DialogueAverageDuration;
        public double? CorrelationLoadSatisfaction;
        public string BestEmployee;
    }    

  
}