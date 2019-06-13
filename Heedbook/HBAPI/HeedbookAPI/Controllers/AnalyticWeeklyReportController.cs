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
    public class AnalyticWeeklyReportController : Controller
    {
      private readonly IConfiguration _config;        
        private readonly ILoginService _loginService;
        private readonly RecordsContext _context;
        private readonly DBOperations _dbOperation;
        private readonly RequestFilters _requestFilters;
        private readonly ElasticClient _log;

        public AnalyticWeeklyReportController(
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

        [HttpGet("User")]
        public IActionResult RatingProgress([FromHeader] string Authorization)
        {
            try
            {
                _log.Info("AnalyticWeeklyReport/Progress started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var companyId = Guid.Parse(userClaims["companyId"]);     
                // var begTime = _requestFilters.GetBegDate(beg);
                // var endTime = _requestFilters.GetEndDate(end);
                // _requestFilters.CheckRoles(ref companyIds, corporationIds, role, companyId);       
                // var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                // var sessions = _context.Sessions
                //     .Include(p => p.ApplicationUser)
                //     .Where(p => p.BegTime >= prevBeg
                //             && p.EndTime <= endTime
                //             && p.StatusId == 7
                //             && p.ApplicationUserId ==
                //     .Select(p => new SessionInfo
                //     {
                //         ApplicationUserId = p.ApplicationUserId,
                //         BegTime = p.BegTime,
                //         EndTime = p.EndTime
                //     })
                //     .ToList();

                // var dialogues = _context.Dialogues
                //     .Include(p => p.ApplicationUser)
                //     .Where(p => p.BegTime >= prevBeg
                //             && p.EndTime <= endTime
                //             && p.StatusId == 3
                //             && p.InStatistic == true
                //             && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                //     .Select(p => new DialogueInfo
                //     {
                //         DialogueId = p.DialogueId,
                //         ApplicationUserId = p.ApplicationUserId,
                //         BegTime = p.BegTime,
                //         EndTime = p.EndTime,
                //         SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                //         FullName = p.ApplicationUser.FullName
                //     })
                //     .ToList();

                // var results = dialogues
                //     .GroupBy(p => p.ApplicationUserId)
                //     .Select(p => new RatingProgressInfo
                //     {
                //         FullName = p.First().FullName,
                //         UserResults = p.GroupBy(q => q.BegTime.Date)
                //             .Select(q => new RatingProgressUserInfo
                //             {
                //                 Date = q.Key,
                //                 DialogueCount = q.Count() != 0 ? q.Select(r => r.DialogueId).Distinct().Count() : 0,
                //                 TotalScore = q.Count() != 0 ? q.Average(r => r.SatisfactionScore) : null,
                //                 Load = _dbOperation.LoadIndex(sessions, q, p.Key, q.Key, begTime, endTime),
                //                 LoadHours = _dbOperation.SessionAverageHours(sessions, p.Key, q.Key, begTime, endTime),
                //                 WorkingHours = _dbOperation.DialogueSumDuration(q, begTime, endTime),
                //                 DialogueDuration = _dbOperation.DialogueAverageDuration(q, begTime, endTime)
                //             }).ToList()
                //     }).ToList();

                _log.Info("AnalyticRating/Progress finished");
                //return Ok(JsonConvert.SerializeObject(results));
                return Ok();
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }
       
    }   
}