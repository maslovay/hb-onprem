using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using UserOperations.Services;
using Newtonsoft.Json;
using UserOperations.Utils;
using UserOperations.Providers;
using System.Threading.Tasks;
using UserOperations.Models.Get.HomeController;
using UserOperations.Utils.AnalyticHomeUtils;

namespace UserOperations.Services
{
    public class AnalyticHomeService : Controller
    {
        private readonly IAnalyticHomeProvider _analyticHomeProvider;
        private readonly IConfiguration _config;
        private readonly ILoginService _loginService;
        private readonly IRequestFilters _requestFilters;
        private readonly AnalyticHomeUtils _utils;

        public AnalyticHomeService(
            IAnalyticHomeProvider homeProvider,
            IConfiguration config,
            ILoginService loginService,
            IRequestFilters requestFilters,
            AnalyticHomeUtils utils
            )
        {
            _analyticHomeProvider = homeProvider;
            _config = config;
            _loginService = loginService;
            _requestFilters = requestFilters;
            _utils = utils;
        }

        [HttpGet("Dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery(Name = "begTime")] string beg,
                                                        [FromQuery(Name = "endTime")] string end, 
                                                        [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                        [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
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

                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);               

                var sessions = await _analyticHomeProvider.GetSessionInfoAsync(prevBeg, endTime, companyIds, workerTypeIds);
                var sessionCur = sessions != null? sessions.Where(p => p.BegTime.Date >= begTime).ToList() : null;
                var sessionOld = sessions != null ? sessions.Where(p => p.BegTime.Date < begTime).ToList() : null;
                var typeIdCross = await _analyticHomeProvider.GetCrossPhraseTypeIdAsync();

                var dialogues = _analyticHomeProvider.GetDialoguesIncludedPhrase(prevBeg, endTime, companyIds, workerTypeIds)
                       .Select(p => new DialogueInfo
                       {
                           DialogueId = p.DialogueId,
                           ApplicationUserId = p.ApplicationUserId,
                           FullName = p.ApplicationUser.FullName,
                           BegTime = p.BegTime,
                           EndTime = p.EndTime,
                           CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count(),
                           SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                           SatisfactionScoreBeg = p.DialogueClientSatisfaction.FirstOrDefault().BegMoodByNN,
                           SatisfactionScoreEnd = p.DialogueClientSatisfaction.FirstOrDefault().EndMoodByNN
                       }).ToList();

                ////-----------------FOR BRANCH---------------------------------------------------------------
                List<BenchmarkModel> benchmarksList = (await _analyticHomeProvider.GetBenchmarksList(begTime, endTime, companyIds)).ToList();

                var dialoguesCur = (dialogues.Where(p => p.BegTime >= begTime).ToList());
                var dialoguesOld = (dialogues.Where(p => p.BegTime < begTime).ToList());

                var result = new UserOperations.Models.Get.HomeController.DashboardInfo()
                {
                    // DialoguesCount = _dbOperation.DialoguesCount(dialoguesCur),
                    DialoguesCount = _utils.DialoguesCount(dialoguesCur),
                    DialoguesCountDelta = -_utils.DialoguesCount(dialoguesOld),

                    EmployeeCount = _utils.EmployeeCount(dialoguesCur),
                    EmployeeCountDelta = -_utils.EmployeeCount(dialoguesOld),

                    BestEmployee = _utils.BestEmployee(dialoguesCur, sessionCur, begTime, endTime.AddDays(1)),
                    BestEmployeeEfficiency = _utils.BestEmployeeEfficiency(dialoguesCur, sessionCur, begTime, endTime.AddDays(1)),
                    BestProgressiveEmployee = _utils.BestProgressiveEmployee(dialogues, begTime),
                    BestProgressiveEmployeeDelta = _utils.BestProgressiveEmployeeDelta(dialogues, begTime),

                    SatisfactionDialogueDelta = _utils.SatisfactionDialogueDelta(dialogues),
                    SatisfactionIndex = _utils.SatisfactionIndex(dialoguesCur),
                    SatisfactionIndexDelta = -_utils.SatisfactionIndex(dialoguesOld),

                    LoadIndex = _utils.LoadIndex(sessionCur, dialoguesCur, begTime, endTime.AddDays(1)),
                    LoadIndexDelta = -_utils.LoadIndex(sessionOld, dialoguesOld, prevBeg, begTime),

                    CrossIndex = _utils.CrossIndex(dialoguesCur),
                    CrossIndexDelta = -_utils.CrossIndex(dialoguesOld),

                    AvgWorkingTimeEmployees = _utils.SessionAverageHours(sessionCur, begTime, endTime),
                    AvgWorkingTimeEmployeesDelta = -_utils.SessionAverageHours(sessionOld, prevBeg, begTime),

                    NumberOfDialoguesPerEmployees = Convert.ToInt32(_utils.DialoguesPerUser(dialoguesCur)),
                    NumberOfDialoguesPerEmployeesDelta = -Convert.ToInt32(_utils.DialoguesPerUser(dialoguesOld)),

                    DialogueDuration = _utils.DialogueAverageDuration(dialoguesCur, begTime, endTime),
                    DialogueDurationDelta = -_utils.DialogueAverageDuration(dialoguesOld, prevBeg, begTime)                 
                };


                //---benchmarks
                if (benchmarksList != null && benchmarksList.Count() != 0)
                {
                    result.SatisfactionIndexIndustryAverage = _analyticHomeProvider.GetBenchmarkIndustryAvg(benchmarksList, "SatisfactionIndexIndustryAvg");
                    result.SatisfactionIndexIndustryBenchmark = _analyticHomeProvider.GetBenchmarkIndustryMax(benchmarksList, "SatisfactionIndexIndustryBenchmark");

                    result.LoadIndexIndustryAverage = _analyticHomeProvider.GetBenchmarkIndustryAvg(benchmarksList, "LoadIndexIndustryAvg");
                    result.LoadIndexIndustryBenchmark = _analyticHomeProvider.GetBenchmarkIndustryMax(benchmarksList, "LoadIndexIndustryBenchmark"); 

                    result.CrossIndexIndustryAverage = _analyticHomeProvider.GetBenchmarkIndustryAvg(benchmarksList, "CrossIndexIndustryAvg"); 
                    result.CrossIndexIndustryBenchmark = _analyticHomeProvider.GetBenchmarkIndustryMax(benchmarksList, "CrossIndexIndustryBenchmark");
                }           

                result.NumberOfDialoguesPerEmployeesDelta += result.NumberOfDialoguesPerEmployees;
                result.AvgWorkingTimeEmployeesDelta +=result.AvgWorkingTimeEmployees;
                result.EmployeeCountDelta += result.EmployeeCount;
                result.CrossIndexDelta += result.CrossIndex;
                result.LoadIndexDelta += result.LoadIndex;
                result.SatisfactionIndexDelta += result.SatisfactionIndex;
                result.DialoguesCountDelta += result.DialoguesCount;
                result.DialogueDurationDelta += result.DialogueDuration;

                var jsonToReturn = JsonConvert.SerializeObject(result);
                return Ok(jsonToReturn);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("NewDashboard")]
        public async Task<IActionResult> GetNewDashboard([FromQuery(Name = "begTime")] string beg,
                                                   [FromQuery(Name = "endTime")] string end,
                                                   [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                   [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
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

                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var sessions = await _analyticHomeProvider.GetSessionInfoAsync(prevBeg, endTime, companyIds, workerTypeIds);
                var sessionCur = sessions != null ? sessions.Where(p => p.BegTime.Date >= begTime).ToList() : null;
                var sessionOld = sessions != null ? sessions.Where(p => p.BegTime.Date < begTime).ToList() : null;
                var typeIdCross = await _analyticHomeProvider.GetCrossPhraseTypeIdAsync();

                var dialogues = _analyticHomeProvider.GetDialoguesIncludedPhrase(prevBeg, endTime, companyIds, workerTypeIds)
                       .Select(p => new DialogueInfo
                       {
                           DialogueId = p.DialogueId,
                           ApplicationUserId = p.ApplicationUserId,
                           FullName = p.ApplicationUser.FullName,
                           BegTime = p.BegTime,
                           EndTime = p.EndTime,
                           CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count(),
                           SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                           SatisfactionScoreBeg = p.DialogueClientSatisfaction.FirstOrDefault().BegMoodByNN,
                           SatisfactionScoreEnd = p.DialogueClientSatisfaction.FirstOrDefault().EndMoodByNN
                       }).ToList();


              

                ////-----------------FOR BRANCH---------------------------------------------------------------
                List<BenchmarkModel> benchmarksList = (await _analyticHomeProvider.GetBenchmarksList(begTime, endTime, companyIds)).ToList();

                var dialoguesCur = dialogues.Where(p => p.BegTime >= begTime).ToList();
                var dialoguesOld = dialogues.Where(p => p.BegTime < begTime).ToList();

                var slideShowSessionsInDialoguesOld = await _analyticHomeProvider
                    .GetSlideShowWithDialogueIdFilteredByPoolAsync(prevBeg, begTime, companyIds, new List<Guid>(), workerTypeIds, false, dialoguesOld);
                var viewsOld = slideShowSessionsInDialoguesOld.Where(x => x.Campaign == null || !x.Campaign.IsSplash).Count();

                var slideShowSessionsInDialoguesCur = await _analyticHomeProvider
                    .GetSlideShowWithDialogueIdFilteredByPoolAsync(begTime, endTime, companyIds, new List<Guid>(), workerTypeIds, false, dialoguesCur);
                var viewsCur = slideShowSessionsInDialoguesCur.Where(x => x.Campaign == null || !x.Campaign.IsSplash).Count();

                var result = new NewDashboardInfo()
                {
                    ClientsCount = _utils.DialoguesCount(dialoguesCur),
                    ClientsCountDelta = -_utils.DialoguesCount(dialoguesOld),

                    EmployeeCount = (await _analyticHomeProvider.GetEmployees(endTime, companyIds, null, workerTypeIds)).Count(),
                    BestEmployees = _utils.BestThreeEmployees(dialoguesCur, sessionCur, begTime, endTime.AddDays(1)),

                    SatisfactionIndex = _utils.SatisfactionIndex(dialoguesCur),
                    SatisfactionIndexDelta = -_utils.SatisfactionIndex(dialoguesOld),

                    LoadIndex = _utils.LoadIndex(sessionCur, dialoguesCur, begTime, endTime.AddDays(1)),
                    LoadIndexDelta = -_utils.LoadIndex(sessionOld, dialoguesOld, prevBeg, begTime),

                    CrossIndex = _utils.CrossIndex(dialoguesCur),
                    CrossIndexDelta = -_utils.CrossIndex(dialoguesOld),

                    AdvCount = viewsCur,
                    AdvCountDelta = viewsCur - viewsOld,
                    AnswerCount = (await _analyticHomeProvider.GetAnswersAsync(begTime, endTime, companyIds, new List<Guid>(), workerTypeIds)).Count(),
                    AnswerCountDelta = - (await _analyticHomeProvider.GetAnswersAsync(prevBeg, begTime, companyIds, new List<Guid>(), workerTypeIds)).Count()//,
                    //EmployeeOnlineCount = await _analyticHomeProvider.GetSessionOnline(companyIds, workerTypeIds),
                    //EmployeeServingClientCount = 0,
                    //EmployeeTabletActiveCount = 0
                };


                //---benchmarks
                if (benchmarksList != null && benchmarksList.Count() != 0)
                {
                    result.SatisfactionIndexIndustryAverage = _analyticHomeProvider.GetBenchmarkIndustryAvg(benchmarksList, "SatisfactionIndexIndustryAvg");
                    result.SatisfactionIndexIndustryBenchmark = _analyticHomeProvider.GetBenchmarkIndustryMax(benchmarksList, "SatisfactionIndexIndustryBenchmark");

                    result.LoadIndexIndustryAverage = _analyticHomeProvider.GetBenchmarkIndustryAvg(benchmarksList, "LoadIndexIndustryAvg");
                    result.LoadIndexIndustryBenchmark = _analyticHomeProvider.GetBenchmarkIndustryMax(benchmarksList, "LoadIndexIndustryBenchmark");

                    result.CrossIndexIndustryAverage = _analyticHomeProvider.GetBenchmarkIndustryAvg(benchmarksList, "CrossIndexIndustryAvg");
                    result.CrossIndexIndustryBenchmark = _analyticHomeProvider.GetBenchmarkIndustryMax(benchmarksList, "CrossIndexIndustryBenchmark");
                }

                result.CrossIndexDelta += result.CrossIndex;
                result.LoadIndexDelta += result.LoadIndex;
                result.SatisfactionIndexDelta += result.SatisfactionIndex;
                result.AnswerCountDelta += result.AnswerCount;
                result.AdvCountDelta += result.AdvCount;
                result.ClientsCountDelta += result.ClientsCount;

                var jsonToReturn = JsonConvert.SerializeObject(result);
                return Ok(jsonToReturn);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("DashboardFiltered")]
        public async Task<IActionResult> GetDashboardFiltered([FromQuery(Name = "begTime")] string beg,
                                                  [FromQuery(Name = "endTime")] string end,
                                                  [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
                                                  [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
                                                  [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
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

                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var typeIdCross = await _analyticHomeProvider.GetCrossPhraseTypeIdAsync();
                var dialogues = _analyticHomeProvider.GetDialoguesIncludedPhrase(prevBeg, endTime, companyIds, workerTypeIds, applicationUserIds)
                        .Select(p => new DialogueInfo
                         {
                             DialogueId = p.DialogueId,
                             ApplicationUserId = p.ApplicationUserId,
                             BegTime = p.BegTime,
                             EndTime = p.EndTime,
                             CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count()
                         })
                         .ToList();

                var sessions = await _analyticHomeProvider.GetSessionInfoAsync(prevBeg, endTime, companyIds, workerTypeIds, applicationUserIds);

                var sessionCur = sessions.Where(p => p.BegTime.Date >= begTime).ToList();
                var sessionOld = sessions.Where(p => p.BegTime.Date < begTime).ToList();

                ////-----------------FOR BRANCH---------------------------------------------------------------
                List<BenchmarkModel> benchmarksList = (await _analyticHomeProvider.GetBenchmarksList(begTime, endTime, companyIds)).ToList();

                var dialoguesCur = dialogues.Where(p => p.BegTime >= begTime).ToList();
                var dialoguesOld = dialogues.Where(p => p.BegTime < begTime).ToList();

                double? crossIndexIndustryAverage = null, crossIndexIndustryBenchmark = null;
                double? loadIndexIndustryAverage = null, loadIndexIndustryBenchmark = null;

                var crossIndex = _utils.CrossIndex(dialoguesCur);
                var loadIndex = _utils.LoadIndex(sessionCur, dialoguesCur, begTime, endTime.AddDays(1));
                var dialoguesCount = _utils.DialoguesCount(dialoguesCur);

                //---benchmarks
                if (benchmarksList != null && benchmarksList.Count() != 0)
                {
                    loadIndexIndustryAverage = _analyticHomeProvider.GetBenchmarkIndustryAvg(benchmarksList, "LoadIndexIndustryAvg");
                    loadIndexIndustryBenchmark = _analyticHomeProvider.GetBenchmarkIndustryMax(benchmarksList, "LoadIndexIndustryBenchmark");

                    crossIndexIndustryAverage = _analyticHomeProvider.GetBenchmarkIndustryAvg(benchmarksList, "CrossIndexIndustryAvg");
                    crossIndexIndustryBenchmark = _analyticHomeProvider.GetBenchmarkIndustryMax(benchmarksList, "CrossIndexIndustryBenchmark");
                }

                var result = new
                {
                    DialoguesCount = dialoguesCount,
                    DialoguesCountDelta = dialoguesCount - _utils.DialoguesCount(dialoguesOld),

                    CrossIndex = _utils.CrossIndex(dialoguesCur),
                    CrossIndexDelta = crossIndex - _utils.CrossIndex(dialoguesOld),

                    CrossIndexIndustryAverage = crossIndexIndustryAverage,
                    CrossIndexIndustryBenchmark = crossIndexIndustryBenchmark,

                    LoadIndex = loadIndex,
                    LoadIndexDelta = loadIndex -_utils.LoadIndex(sessionOld, dialoguesOld, prevBeg, begTime),

                    LoadIndexIndustryAverage = loadIndexIndustryAverage,
                    LoadIndexIndustryBenchmark = loadIndexIndustryBenchmark
            };

                var jsonToReturn = JsonConvert.SerializeObject(result);
                return Ok(jsonToReturn);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        //[HttpGet("Recomendation")]
        //public IActionResult GetRecomendation([FromQuery(Name = "begTime")] string beg,
        //                                                [FromQuery(Name = "endTime")] string end, 
        //                                                [FromQuery(Name = "applicationUserId[]")] List<Guid> applicationUserIds,
        //                                                [FromQuery(Name = "companyId[]")] List<Guid> companyIds,
        //                                                [FromQuery(Name = "corporationId[]")] List<Guid> corporationIds,
        //                                                [FromQuery(Name = "workerTypeId[]")] List<Guid> workerTypeIds,
        //                                                [FromHeader] string Authorization)

        //{
        //    if (!_loginService.GetDataFromToken(Authorization, out var userClaims))
        //            return BadRequest("Token wrong");
        //    var role = userClaims["role"];
        //    var companyId = Guid.Parse(userClaims["companyId"]);     
        //    var begTime = _requestFilters.GetBegDate(beg);
        //    var endTime = _requestFilters.GetEndDate(end);
        //    _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);       
        //    var hintCount = !String.IsNullOrEmpty(_config["hint : hintCount"]) ? Convert.ToInt32(_config["hint : hintCount"]): 3;
          

        //    var hints = _analyticHomeProvider.GetDialogueHints(begTime, endTime, companyIds, applicationUserIds, workerTypeIds)
        //            .Select(p => new
        //            {
        //                p.HintText,
        //                p.IsAutomatic,
        //                p.IsPositive,
        //                p.Type
        //            }).ToList();

        //    var positiveTopHints = hints.Where(p => p.IsPositive == true)
        //        .GroupBy(p => p.HintText)
        //        .Select(p => new
        //        {
        //            HintText = p.Key,
        //            HintCount = p.Count()
        //        })
        //        .OrderByDescending(p => p.HintCount).Take(hintCount).Select(p => p.HintText).ToList();

        //    var negativeTopHints = hints.Where(p => p.IsPositive == false)
        //        .GroupBy(p => p.HintText)
        //        .Select(p => new
        //        {
        //            HintText = p.Key,
        //            HintCount = p.Count()
        //        })
        //        .OrderByDescending(p => p.HintCount).Take(hintCount).Select(p => p.HintText).ToList();
        //    var topHints = new List<TopHintInfo>();
        //    topHints.Add(new TopHintInfo
        //    {
        //        IsPositive = true,
        //        Hints = positiveTopHints
        //    });

        //    topHints.Add(new TopHintInfo
        //    {
        //        IsPositive = false,
        //        Hints = negativeTopHints
        //    });
        //    return Ok(JsonConvert.SerializeObject(topHints));
        //}
    }

}