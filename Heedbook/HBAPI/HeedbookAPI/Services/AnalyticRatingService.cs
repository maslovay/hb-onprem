using System;
using System.Collections.Generic;
using System.Linq;
using HBData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using UserOperations.Utils;
using UserOperations.Models.Get.AnalyticRatingController;
using UserOperations.Utils.AnalyticRatingUtils;
using UserOperations.Providers;

namespace UserOperations.Services
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticRatingService : Controller
    {    
        private readonly LoginService _loginService;
        private readonly RecordsContext _context;
        private readonly RequestFilters _requestFilters;
        private readonly AnalyticRatingUtils _analyticRatingUtils;
        private readonly IAnalyticRatingProvider _analyticRatingProvider;

        public AnalyticRatingService(
            LoginService loginService,
            RecordsContext context,
            RequestFilters requestFilters,
            AnalyticRatingUtils analyticRatingUtils,
            IAnalyticRatingProvider analyticRatingProvider
            )
        {
            _loginService = loginService;
            _context = context;
            _requestFilters = requestFilters;
            _analyticRatingUtils = analyticRatingUtils;
            _analyticRatingProvider = analyticRatingProvider;
        }

        [HttpGet("Progress")]
        public IActionResult RatingProgress([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                // _log.Info("AnalyticRating/Progress started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);       
              //  var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);
                var typeIdCross = _analyticRatingProvider.GetCrossPhraseTypeId();

                var sessions = _analyticRatingProvider.GetSessions(
                    begTime,
                    endTime,
                    companyIds,
                    applicationUserIds,
                    workerTypeIds);

                var dialogues = _analyticRatingProvider.GetDialogues(
                    begTime,
                    endTime,
                    companyIds,
                    applicationUserIds,
                    workerTypeIds,
                    typeIdCross);

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
                                Load = _analyticRatingUtils.LoadIndex(sessions, q, p.Key, q.Key),
                                LoadHours = _analyticRatingUtils.SessionAverageHours(sessions, p.Key, q.Key),
                                WorkingHours = _analyticRatingUtils.DialogueSumDuration(q),
                                DialogueDuration = _analyticRatingUtils.DialogueAverageDuration(q),
                                CrossInProcents = _analyticRatingUtils.CrossIndex(p)
                            }).ToList()
                    }).ToList();

                // _log.Info("AnalyticRating/Progress finished");
                return Ok(JsonConvert.SerializeObject(results));
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }


        [HttpGet("RatingUsers")]
        public IActionResult RatingUsers([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                // _log.Info("AnalyticRating/RatingUsers started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);       
               // var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                var sessions = _analyticRatingProvider.GetSessions(
                    begTime,
                    endTime,
                    companyIds,
                    applicationUserIds,
                    workerTypeIds);

                var typeIdCross = _analyticRatingProvider.GetCrossPhraseTypeId();

                var dialogues = _analyticRatingProvider.GetDialogues(
                    begTime,
                    endTime,
                    companyIds,
                    applicationUserIds,
                    workerTypeIds,
                    typeIdCross
                );

                var result = dialogues
                    .GroupBy(p => p.ApplicationUserId)
                    .Select(p => new RatingUserInfo
                    {
                        FullName = p.First().FullName,
                        SatisfactionIndex = _analyticRatingUtils.SatisfactionIndex(p),
                        LoadIndex = _analyticRatingUtils.LoadIndex(sessions, p, begTime, endTime),
                        CrossIndex = _analyticRatingUtils.CrossIndex(p),
                        DialoguesCount = p.Select(q => q.DialogueId).Distinct().Count(),
                        CompanyId = p.First().CompanyId.ToString()
                    }).ToList();

                var emptyUsers = sessions.GroupBy(p => p.ApplicationUserId)
                    .Where(p => !result.Select(x=>x.FullName).Contains(p.First().FullName))
                    .Select(p => new RatingUserInfo
                    {
                        FullName = p.First().FullName,
                        SatisfactionIndex = 0,
                        LoadIndex = 0,
                        CrossIndex = 0,
                        DialoguesCount = 0,
                        CompanyId = p.First().CompanyId.ToString()
                    }).ToList();

                result = result.Union(emptyUsers).OrderByDescending(p => p.SatisfactionIndex).ToList();
                // _log.Info("AnalyticRating/RatingUsers finished");
                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occurred {e}");
                return BadRequest(e);
            }
        }  


        [HttpGet("RatingOffices")]
        public IActionResult RatingOffices([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
                                                        [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
                                                        [FromHeader] string Authorization)
        {
            try
            {
                // _log.Info("AnalyticRating/RatingOffices started");
                if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
                    return BadRequest("Token wrong");
                var role = userClaims["role"];
                var companyId = Guid.Parse(userClaims["companyId"]);     
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);       
                //var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                var sessions = _analyticRatingProvider.GetSessionInfoCompanys(
                    begTime,
                    endTime,
                    companyIds,
                    workerTypeIds
                );

                var typeIdCross = _analyticRatingProvider.GetCrossPhraseTypeId();

                var dialogues = _analyticRatingProvider.GetDialogueInfoCompanys(
                    begTime,
                    endTime,
                    companyIds,
                    workerTypeIds,
                    typeIdCross
                );

                var result = dialogues
                    .GroupBy(p => p.CompanyId)
                    .Select(p => new RatingOfficeInfo
                    {
                        CompanyId = p.Key.ToString(),
                        FullName = p.First().FullName,
                        SatisfactionIndex = _analyticRatingUtils.SatisfactionIndex(p),
                        LoadIndex = _analyticRatingUtils.LoadIndex(sessions, p, begTime, endTime),
                        CrossIndex = _analyticRatingUtils.CrossIndex(p),
                        Recommendation = "",
                        DialoguesCount = p.Select(q => q.DialogueId).Distinct().Count(),
                        DaysCount = p.Select(q => q.BegTime.Date).Distinct().Count(),
                        WorkingHoursDaily = _analyticRatingUtils.DialogueAverageDuration(p, begTime, endTime),
                        DialogueAverageDuration = _analyticRatingUtils.DialogueAverageDuration(p, begTime, endTime),
                        DialogueAveragePause = _analyticRatingUtils.DialogueAveragePause(sessions, p, begTime, endTime)
                    }).ToList();
                result = result.OrderBy(p => p.EfficiencyIndex).ToList();
                // _log.Info("AnalyticRating/RatingOffices finished");
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