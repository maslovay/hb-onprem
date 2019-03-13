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
using UserOperations.Repository;
using UserOperations.Models;
using UserOperations.Models.AccountViewModels;
using UserOperations.Services;
using UserOperations.Models.AnalyticModels;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using UserOperations.Data;
using static UserOperations.Utils.DBOperations;
using UserOperations.Utils;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticReportController : Controller
    {
        private readonly RecordsContext _context;
        private readonly IConfiguration _config;
        private readonly DBOperations _dbOperation;

        public AnalyticReportController(
            RecordsContext context,
            IConfiguration config,
            DBOperations dbOperation
            )
        {
            _context = context;
            _config = config;
            _dbOperation = dbOperation;
        }

        [HttpGet("ActiveEmployee")]
        public IActionResult ReportActiveEmployee([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId")] List<Guid> companyIds,
                                                        [FromQuery(Name = "workerTypeId")] List<Guid> workerTypeIds)
        {
            try
            {
                var stringFormat = "yyyyMMdd";
                var begTime = !String.IsNullOrEmpty(beg) ? DateTime.ParseExact(beg, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now;
                var endTime = !String.IsNullOrEmpty(end) ? DateTime.ParseExact(end, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now.AddDays(-6);
                begTime = begTime.Date;
                endTime = endTime.Date.AddDays(1);


                var sessions = _context.Sessions
                    .Include(p => p.ApplicationUser)
                    .Where(p => p.BegTime >= begTime
                            && p.StatusId == 6
                            && (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                    .Select(p => new SessionInfo
                    {
                        ApplicationUserId = p.ApplicationUserId,
                        FullName = p.ApplicationUser.FullName
                    })
                    .ToList().Distinct().ToList();

                return Ok(JsonConvert.SerializeObject(sessions));
            }
            catch (Exception e)
            {
               return BadRequest(e); 
            }

        }

        [HttpGet("UserPartial")]
        public IActionResult ReportUserPartial([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId")] List<Guid> companyIds,
                                                        [FromQuery(Name = "workerTypeId")] List<Guid> workerTypeIds)
        {
            try
            {
                var stringFormat = "yyyyMMdd";
                var begTime = !String.IsNullOrEmpty(beg) ? DateTime.ParseExact(beg, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now;
                var endTime = !String.IsNullOrEmpty(end) ? DateTime.ParseExact(end, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now.AddDays(-6);
                begTime = begTime.Date;
                endTime = endTime.Date.AddDays(1);

                var sessions = _context.Sessions
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.ApplicationUser.WorkerType)
                    .Where(p => p.BegTime >= begTime
                            && p.EndTime <= endTime
                            && p.StatusId == 7
                            && (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
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
                            && (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
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
                    .Select(p => new ReportPartPeriodEmployeeInfo
                    {
                        FullName = p.First().FullName,
                        ApplicationUserId = p.Key,
                        // WorkerType = p.First().WorkerType,
                        LoadIndexAverage = _dbOperation.LoadIndex(p, dialogues, begTime, endTime),
                        PeriodInfo = p.GroupBy(q => q.BegTime.Date).Select(q => new ReportPartDayEmployeeInfo {
                            Date = q.Key,
                            WorkingHours = _dbOperation.MaxDouble(_dbOperation.SessionAverageHours(q),_dbOperation.DialogueSumDuration(q, dialogues, p.Key)),
                            DialogueHours = _dbOperation.DialogueSumDuration(q, dialogues, p.Key),
                            LoadIndex = _dbOperation.LoadIndex(q, dialogues, p.Key),
                            DialogueCount = _dbOperation.DialoguesCount(dialogues, p.Key, q.Key)
                        }).ToList()
                    }).ToList();
                return Ok(JsonConvert.SerializeObject(result));

            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpGet("UserFull")]
        public IActionResult ReportUserFull([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId")] List<Guid> companyIds,
                                                        [FromQuery(Name = "workerTypeId")] List<Guid> workerTypeIds)
        {
            try
            {
                var stringFormat = "yyyyMMdd";
                var begTime = !String.IsNullOrEmpty(beg) ? DateTime.ParseExact(beg, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now;
                var endTime = !String.IsNullOrEmpty(end) ? DateTime.ParseExact(end, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now.AddDays(-6);
                begTime = begTime.Date;
                endTime = endTime.Date.AddDays(1);

                var sessions = _context.Sessions
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.ApplicationUser.WorkerType)
                    .Where(p => p.BegTime >= begTime
                            && p.EndTime <= endTime
                            && p.StatusId == 7
                            && (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
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
                            && (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                            && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
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
                            userInfo.SessionTime = _dbOperation. SessionAverageHours(sessions, applicationUserId, Convert.ToDateTime(date), begDate, endDate);
                            userInfo.PeriodInfo = TimeTable(sessions, dialoguesUser, applicationUserId, Convert.ToDateTime(date));
                            
                            result.Add(userInfo);
                        }
                    }
                }

                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        public List<ReportFullDayInfo> Sum(List<ReportFullDayInfo> curRes, ReportFullDayInfo newInterval)
        {
            var intervals = curRes;
            newInterval.End = _dbOperation.MinTime(newInterval.End, intervals.Max(p => p.End));
            newInterval.Beg = _dbOperation.MaxTime(newInterval.Beg, intervals.Min(p => p.Beg));

            foreach (var interval in intervals.Where(p => p.Beg >= newInterval.Beg && p.End <= newInterval.End))
            {
                interval.ActivityType += 1;
            }

            // case inside
            var begInterval = intervals.Where(p => p.Beg < newInterval.Beg && p.End > newInterval.End);

            if (begInterval.Count() == 1)
            {

                var end = begInterval.First().End;
                var dialogueId = begInterval.First().DialogueId;
                var type = begInterval.First().ActivityType;

                begInterval.First().End = newInterval.Beg;

                newInterval.ActivityType = type + newInterval.ActivityType;
                newInterval.DialogueId = Guid.Parse(dialogueId.ToString() + newInterval.DialogueId.ToString());
                intervals.Add(newInterval);

                intervals.Add(new ReportFullDayInfo
                {
                    Beg = newInterval.End,
                    End = end,
                    DialogueId = dialogueId,
                    ActivityType = type
                });
            }
            else
            {
                begInterval = intervals.Where(p => p.Beg < newInterval.Beg && p.End > newInterval.Beg);
                if (begInterval.Count() == 1)
                {
                    var end = begInterval.First().End;
                    var dialogueId = begInterval.First().DialogueId;
                    var type = begInterval.First().ActivityType;

                    begInterval.First().End = newInterval.Beg;

                    intervals.Add(new ReportFullDayInfo
                    {
                        Beg = newInterval.Beg,
                        End = end,
                        DialogueId = Guid.Parse(dialogueId.ToString() + newInterval.DialogueId.ToString()),
                        ActivityType = type + newInterval.ActivityType
                    });
                }

                var endInterval = intervals.Where(p => p.Beg < newInterval.End && p.End > newInterval.End);

                if (endInterval.Count() == 1)
                {
                    var end = endInterval.First().End;
                    var dialogueId = endInterval.First().DialogueId;
                    var type = endInterval.First().ActivityType;

                    var endIntervalNew = endInterval.First();
                    endIntervalNew.End = newInterval.End;
                    endIntervalNew.DialogueId =Guid.Parse(endIntervalNew.DialogueId.ToString() + newInterval.DialogueId.ToString());
                    endIntervalNew.ActivityType += newInterval.ActivityType;

                    intervals.Add(new ReportFullDayInfo
                    {
                        Beg = newInterval.End,
                        End = end,
                        DialogueId = dialogueId,
                        ActivityType = type
                    });
                }
            }


            return intervals;
        }

        public List<ReportFullDayInfo> TimeTable(List<SessionInfo> sessions, List<DialogueInfo> dialogues, Guid applicationUserId, DateTime date)
        {
            var result = new List<ReportFullDayInfo>();
            result.Add(new ReportFullDayInfo
            {
                Beg = date.Date,
                End = date.Date.AddDays(1),
                ActivityType = 0,
                DialogueId = null
            });
            if (sessions.Count() != 0)
            {
                foreach (var session in sessions.Where(p => p.BegTime.Date == date && p.ApplicationUserId == applicationUserId))
                {
                    result = Sum(result, new ReportFullDayInfo
                    {
                        Beg = session.BegTime,
                        End = session.EndTime,
                        DialogueId = null,
                        ActivityType = 1
                    });
                }
            }

            if (dialogues.Count() != 0)
            {
                foreach (var dialogue in dialogues)
                {
                    result = Sum(result, new ReportFullDayInfo
                    {
                        Beg = dialogue.BegTime,
                        End = dialogue.EndTime,
                        DialogueId = dialogue.DialogueId,
                        ActivityType = 1
                    });
                }
            }
            result = result.OrderBy(p => p.Beg).ToList();
            foreach (var element in result.Where(p => p.ActivityType != 0 && p.ActivityType != 1 && p.ActivityType != 2 ))
            {
                element.ActivityType = 0;
            }
            foreach (var element in result.Where(p =>p.DialogueId != null))
            {
                element.ActivityType = 2;
            }
            return result;
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

    public class ReportFullDayInfo
    {
        public int ActivityType;
        public DateTime Beg;
        public DateTime End;
        public Guid? DialogueId;
    }

    public class ReportFullPeriodInfo
    {
        public string FullName;
        public Guid ApplicationUserId;
        public string WorkerType;
        public DateTime Date;
        public double? SessionTime;
        public double? DialoguesTime;
        public double? Load;
        public List<ReportFullDayInfo> PeriodInfo;
    }
}