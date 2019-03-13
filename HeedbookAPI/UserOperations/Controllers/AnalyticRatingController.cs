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
    public class AnalyticRatingController : Controller
    {
        private readonly RecordsContext _context;
        private readonly IConfiguration _config;
        private readonly DBOperations _dbOperation;

        public AnalyticRatingController(
            RecordsContext context,
            IConfiguration config,
            DBOperations dbOperation
            )
        {
            _context = context;
            _config = config;
            _dbOperation = dbOperation;
        }

        [HttpGet("Progress")]
        public IActionResult RatingProgress([FromQuery(Name = "begTime")] string beg,
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
                        EndTime = p.EndTime,
                        SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                        FullName = p.ApplicationUser.FullName
                    })
                    .ToList();

                var results = dialogues
                    .GroupBy(p => p.ApplicationUserId)
                    .Select(p => new RatingProgressInfo
                    {
                        FullName = p.First().FullName,
                        UserResults = p.GroupBy(q => q.BegTime.Date)
                            .Select(q => new RatingProgressUserInfo
                            {
                                Date = q.Key,
                                DialogueCount = q.Count() != 0 ? q.Select(r => r.DialogueId).Distinct().Count() : 0,
                                TotalScore = q.Count() != 0 ? q.Average(r => r.SatisfactionScore) : null,
                                Load = _dbOperation.LoadIndex(sessions, q, p.Key, q.Key, begTime, endTime),
                                LoadHours = _dbOperation.SessionAverageHours(sessions, p.Key, q.Key, begTime, endTime),
                                WorkingHours = _dbOperation.DialogueAverageHoursDaily(q, begTime, endTime),
                                DialogueDuration = _dbOperation.DialogueAverageDuration(q, begTime, endTime)
                            }).ToList()
                    }).ToList();


                return Ok(JsonConvert.SerializeObject(results));
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }


        [HttpGet("RatingUsers")]
        public IActionResult RatingUsers([FromQuery(Name = "begTime")] string beg,
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

                var typeIdCross = _context.PhraseTypes.Where(p => p.PhraseTypeText == "Cross").Select(p => p.PhraseTypeId).FirstOrDefault();


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
                        BegTime = p.BegTime,
                        EndTime = p.EndTime,
                        SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                        FullName = p.ApplicationUser.FullName,
                        CrossCout = p.DialoguePhraseCount.Where(q => q.PhraseTypeId == typeIdCross).Count()
                    })
                    .ToList();

                var result = dialogues
                    .GroupBy(p => p.ApplicationUserId)
                    .Select(p => new RatingUserInfo
                    {
                        FullName = p.First().FullName,
                        SatisfactionIndex = _dbOperation.SatisfactionIndex(p),
                        LoadIndex = _dbOperation.LoadIndex(sessions, p, begTime, endTime),
                        CrossIndex = _dbOperation.CrossIndex(p),
                        EfficiencyIndex = _dbOperation.EfficiencyIndex(sessions, p, begTime, endTime),
                        Recommendation = "",
                        DialoguesCount = p.Select(q => q.DialogueId).Distinct().Count(),
                        DaysCount = p.Select(q => q.BegTime.Date).Distinct().Count(),
                        WorkingHoursDaily = _dbOperation.DialogueAverageDuration(p, begTime, endTime),
                        DialogueAverageDuration = _dbOperation.DialogueAverageDuration(p, begTime, endTime),
                        DialogueAveragePause = _dbOperation.DialogueAveragePause(sessions, p, begTime, endTime),
                        ClientsWorkingHoursDaily = _dbOperation.DialogueAverageDurationDaily(p, begTime, endTime)
                    }).ToList();
                result = result.OrderBy(p => p.EfficiencyIndex).ToList();
                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }       
    }
     public class RatingProgressInfo
    {
        public string FullName;
        public List<RatingProgressUserInfo> UserResults;
    }

    public class RatingProgressUserInfo
    {
        public DateTime? Date;
        public int DialogueCount;
        public double? TotalScore;
        public double? Load;
        public double? LoadHours;
        public double? WorkingHours;
        public double? DialogueDuration;
    }

    public class RatingUserInfo
    {
        public string FullName;
        public double? EfficiencyIndex;
        public double? SatisfactionIndex;
        public double? LoadIndex;
        public double? CrossIndex;
        public string Recommendation;
        public int DialoguesCount;
        public int DaysCount;
        public double? WorkingHoursDaily;
        public double? DialogueAverageDuration;
        public double? DialogueAveragePause;
        public double? ClientsWorkingHoursDaily;
    }
}