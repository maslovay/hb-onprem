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
using HBData.Models;
using HBData.Models.AccountViewModels;
using HBData;
using UserOperations.Services;
using UserOperations.Models.AnalyticModels;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using UserOperations.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticEfficiencyController : Controller
    {
        private readonly IConfiguration _config;        
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly DBOperations _dbOperation;
        private readonly RequestFilters _requestFilters;

        public AnalyticEfficiencyController(
            IConfiguration config,
            ILoginService loginService,
            RecordsContext context,
            DBOperations dbOperation,
            RequestFilters requestFilters
            )
        {
            _config = config;
            _loginService = loginService;
            _context = context;
            _dbOperation = dbOperation;
            _requestFilters = requestFilters;
        }

        [HttpGet("EfficiencyDashboard")]
        public IActionResult EfficiencyDashboard([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
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
                var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);                
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);
                

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

                var dialogues = _context.Dialogues
                    .Include(p => p.ApplicationUser)
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
                        BegTime = p.BegTime,
                        EndTime = p.EndTime
                    })
                    .ToList();

                var dialoguesCur = dialogues.Where(p => p.BegTime >= begTime).ToList();
                var dialoguesOld = dialogues.Where(p => p.BegTime < begTime).ToList();

                var result = new EfficiencyDashboardInfo
                {
                    LoadIndex = _dbOperation.LoadIndex(sessionCur, dialoguesCur, begTime, endTime),
                    LoadIndexDelta = - _dbOperation.LoadIndex(sessionOld, dialoguesOld, prevBeg, begTime),
                    DialoguesCount = _dbOperation.DialoguesCount(dialoguesCur),
                    EmployeeCount = _dbOperation.EmployeeCount(dialoguesCur),
                    WorkingHours = _dbOperation.SessionAverageHours(sessionCur, begTime, endTime),
                    WorkingHoursDelta = - _dbOperation.SessionAverageHours(sessionOld, prevBeg, begTime),
                    DialogueAveragePause = _dbOperation.DialogueAveragePause(sessionCur, dialoguesCur, begTime, endTime),
                    DialogueAverageDuration = _dbOperation.DialogueAverageDuration(dialoguesCur, begTime, endTime)
                };

                var optimalLoad = 0.6;
                result.WorkingHoursDelta += result.WorkingHours;
                result.LoadIndexDelta += result.LoadIndex;
                //result.DialoguesPerEmployee = (result.EmployeeCount != null & result.EmployeeCount != 0) ? result.DialoguesCount / result.EmployeeCount: 0;
                result.DialoguesPerEmployee = (dialoguesCur.Count() != 0) ? dialoguesCur.GroupBy(p => p.BegTime.Date).Select(p => p.Count()).Average() / result.EmployeeCount : 0;
                result.EmployeeOptimalCount = (result.LoadIndex != null & result.LoadIndex != 0) ?
                    (Int32?)(Math.Ceiling( (double) (result.EmployeeCount * optimalLoad / result.LoadIndex))) : null;

                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }

        }

        [HttpGet("EfficiencyRating")]
        public IActionResult EfficiencyRating([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
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
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);
                var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

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

                var typeIdCross = _context.PhraseTypes.Where(p => p.PhraseTypeText == "Cross").Select(p => p.PhraseTypeId).First();

                var dialogues = _context.Dialogues
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.DialogueClientSatisfaction)
                    .Include(p => p.DialoguePhraseCount)
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
                        FullName = p.ApplicationUser.FullName,
                        BegTime = p.BegTime,
                        EndTime = p.EndTime,
                        CrossCout = p.DialoguePhraseCount.Where(q => q.PhraseTypeId == typeIdCross).Count(),
                        SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal
                    })
                    .ToList();

                var result = dialogues
                    .GroupBy(p => p.ApplicationUserId)
                    .Select(p => new EfficiencyRatingInfo
                    {
                        FullName = p.First().FullName,
                        LoadIndex = _dbOperation.LoadIndex(sessions, p, begTime, endTime),
                        DialoguesCount = p.Select(q => q.DialogueId).Distinct().Count(),
                        WorkingHoursDaily = _dbOperation.DialogueAverageDuration(p, begTime, endTime),
                        DialogueAverageDuration = _dbOperation.DialogueAverageDuration(p, begTime, endTime),
                        DialogueAveragePause = _dbOperation.DialogueAveragePause(sessions, p, begTime, endTime),
                    //---TODO -- not working in DB dateEnd less then dateBeg
                    // ClientsWorkingHoursDaily = _dbOperation.DialogueAverageDurationDaily(p, begTime, endTime),
                        WorkingDaysCount = _dbOperation.WorkingDaysCount(p)
                    }).ToList();
                result = result.OrderBy(p => p.LoadIndex).ToList();

                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpGet("EfficiencyOptimization")]
        public IActionResult EfficiencyOptimization([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
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
                var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);

                var maxLoad = 0.8;
                var maxPercent = 0.3;            
                

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

                var dialogues = _context.Dialogues
                    .Include(p => p.ApplicationUser)
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
                        BegTime = p.BegTime,
                        EndTime = p.EndTime,
                    })
                    .ToList();

                var dailyInfo = dialogues
                    .GroupBy(p => p.BegTime.Date)
                    .Select(p => new EfficiencyOptimizationDayInfo
                    {
                        Date = Convert.ToDateTime(p.Key),
                        DayLoads = _dbOperation.LoadDaily(Convert.ToDateTime(p.Key), dialogues, sessions)
                    });
                var result = new List<EfficiencyOptimizationEmployeeInfo>();
                for (int i = 0; i < 24; i++)
                {
                    result.Add(new EfficiencyOptimizationEmployeeInfo
                    {
                        OptimalEmployeeCount = _dbOperation.EmployeeCount(dailyInfo.Select(p => p.DayLoads[i]).ToList(), maxLoad, maxPercent),
                        Time = DateTime.MinValue.AddHours(i).TimeOfDay,
                        RealEmployeeCount = (dailyInfo.Count() > 0) ? dailyInfo.Average(p => p.DayLoads[i].UsersCount) : 0
                    });
                }

                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }

        }

        [HttpGet("EfficiencyLoad")]
        public IActionResult EfficiencyLoad([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationIds[]")] List<Guid> corporationIds,
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
                var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);
                _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);

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

                var dialogues = _context.Dialogues
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.DialogueClientSatisfaction)
                    .Include(p => p.ApplicationUser.Session)
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
                        BegTime = p.BegTime,
                        EndTime = p.EndTime,
                        SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                        SessionBegTime = (p.ApplicationUser.Session.FirstOrDefault().BegTime != null) ? p.ApplicationUser.Session.FirstOrDefault().BegTime : p.BegTime,
                        SessionEndTime = (p.ApplicationUser.Session.FirstOrDefault().EndTime != null) ? p.ApplicationUser.Session.FirstOrDefault().EndTime : p.EndTime

                    })
                    .ToList();

                var employeeTime = _dbOperation.EmployeeTimeCalculation(dialogues, sessions);
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

                var result = new EfficiencyLoadClientsCountInfo
                {
                    ClientTimeInfo = clientTime,
                    ClientDayInfo = clientDay,
                    EmployeeTimeInfo = employeeTime
                };
                Console.WriteLine("result : \n" + JsonConvert.SerializeObject(result));
                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
    }
}