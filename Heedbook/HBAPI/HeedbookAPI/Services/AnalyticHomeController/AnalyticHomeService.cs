using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using UserOperations.Services;
using UserOperations.Models.Get;
using Newtonsoft.Json;
using HBData;
using UserOperations.Utils;
using UserOperations.Providers;
using System.Threading.Tasks;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Controllers
{
    public class AnalyticHomeService : Controller
    {
        private readonly IAnalyticCommonProvider _analyticCommonProvider;
        private readonly IAnalyticHomeProvider _analyticHomeProvider;
        private readonly IAnalyticContentProvider _analyticContentProvider;
        private readonly IConfiguration _config;        
        private readonly ILoginService _loginService;
        private readonly IDBOperations _dbOperation;
        private readonly IRequestFilters _requestFilters;
        public AnalyticHomeService(
            IAnalyticCommonProvider analyticProvider,
            IAnalyticHomeProvider homeProvider,
            IAnalyticContentProvider analyticContentProvider,
            IConfiguration config,
            ILoginService loginService,
            IDBOperations dbOperation,
            IRequestFilters requestFilters
            )
        {
            _analyticCommonProvider = analyticProvider;
            _analyticHomeProvider = homeProvider;
            _analyticContentProvider = analyticContentProvider;
            _config = config;
            _loginService = loginService;
            _dbOperation = dbOperation;
            _requestFilters = requestFilters;
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

                var sessions = await _analyticCommonProvider.GetSessionInfoAsync(prevBeg, endTime, companyIds, workerTypeIds);
                var sessionCur = sessions != null? sessions.Where(p => p.BegTime.Date >= begTime).ToList() : null;
                var sessionOld = sessions != null ? sessions.Where(p => p.BegTime.Date < begTime).ToList() : null;
                var typeIdCross = await _analyticCommonProvider.GetCrossPhraseTypeIdAsync();

                var dialogues = _analyticCommonProvider.GetDialoguesIncludedPhrase(prevBeg, endTime, companyIds, workerTypeIds)
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

                var result = new DashboardInfo()
                {
                    DialoguesCount = _dbOperation.DialoguesCount(dialoguesCur),
                    DialoguesCountDelta = -_dbOperation.DialoguesCount(dialoguesOld),

                    EmployeeCount = _dbOperation.EmployeeCount(dialoguesCur),
                    EmployeeCountDelta = -_dbOperation.EmployeeCount(dialoguesOld),

                    BestEmployee = _dbOperation.BestEmployee(dialoguesCur, sessionCur, begTime, endTime.AddDays(1)),
                    BestEmployeeEfficiency = _dbOperation.BestEmployeeEfficiency(dialoguesCur, sessionCur, begTime, endTime.AddDays(1)),
                    BestProgressiveEmployee = _dbOperation.BestProgressiveEmployee(dialogues, begTime),
                    BestProgressiveEmployeeDelta = _dbOperation.BestProgressiveEmployeeDelta(dialogues, begTime),

                    SatisfactionDialogueDelta = _dbOperation.SatisfactionDialogueDelta(dialogues),
                    SatisfactionIndex = _dbOperation.SatisfactionIndex(dialoguesCur),
                    SatisfactionIndexDelta = -_dbOperation.SatisfactionIndex(dialoguesOld),

                    LoadIndex = _dbOperation.LoadIndex(sessionCur, dialoguesCur, begTime, endTime.AddDays(1)),
                    LoadIndexDelta = -_dbOperation.LoadIndex(sessionOld, dialoguesOld, prevBeg, begTime),

                    CrossIndex = _dbOperation.CrossIndex(dialoguesCur),
                    CrossIndexDelta = -_dbOperation.CrossIndex(dialoguesOld),

                    AvgWorkingTimeEmployees = _dbOperation.SessionAverageHours(sessionCur, begTime, endTime),
                    AvgWorkingTimeEmployeesDelta = -_dbOperation.SessionAverageHours(sessionOld, prevBeg, begTime),

                    NumberOfDialoguesPerEmployees = Convert.ToInt32(_dbOperation.DialoguesPerUser(dialoguesCur)),
                    NumberOfDialoguesPerEmployeesDelta = -Convert.ToInt32(_dbOperation.DialoguesPerUser(dialoguesOld)),

                    DialogueDuration = _dbOperation.DialogueAverageDuration(dialoguesCur, begTime, endTime),
                    DialogueDurationDelta = -_dbOperation.DialogueAverageDuration(dialoguesOld, prevBeg, begTime)                 
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

                var sessions = await _analyticCommonProvider.GetSessionInfoAsync(prevBeg, endTime, companyIds, workerTypeIds);
                var sessionCur = sessions != null ? sessions.Where(p => p.BegTime.Date >= begTime).ToList() : null;
                var sessionOld = sessions != null ? sessions.Where(p => p.BegTime.Date < begTime).ToList() : null;
                var typeIdCross = await _analyticCommonProvider.GetCrossPhraseTypeIdAsync();

                var dialogues = _analyticCommonProvider.GetDialoguesIncludedPhrase(prevBeg, endTime, companyIds, workerTypeIds)
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

                var slideShowSessionsInDialoguesOld = await _analyticContentProvider
                    .GetSlideShowWithDialogueIdFilteredByPoolAsync(prevBeg, begTime, companyIds, new List<Guid>(), workerTypeIds, false, dialoguesOld);
                var viewsOld = slideShowSessionsInDialoguesOld.Where(x => x.Campaign == null || !x.Campaign.IsSplash).Count();

                var slideShowSessionsInDialoguesCur = await _analyticContentProvider
                    .GetSlideShowWithDialogueIdFilteredByPoolAsync(begTime, endTime, companyIds, new List<Guid>(), workerTypeIds, false, dialoguesCur);
                var viewsCur = slideShowSessionsInDialoguesCur.Where(x => x.Campaign == null || !x.Campaign.IsSplash).Count();

                var result = new NewDashboardInfo()
                {
                    ClientsCount = _dbOperation.DialoguesCount(dialoguesCur),
                    ClientsCountDelta = -_dbOperation.DialoguesCount(dialoguesOld),

                    EmployeeCount = (await _analyticCommonProvider.GetEmployees(endTime, companyIds, null, workerTypeIds)).Count(),
                    BestEmployees = _dbOperation.BestThreeEmployees(dialoguesCur, sessionCur, begTime, endTime.AddDays(1)),

                    SatisfactionIndex = _dbOperation.SatisfactionIndex(dialoguesCur),
                    SatisfactionIndexDelta = -_dbOperation.SatisfactionIndex(dialoguesOld),

                    LoadIndex = _dbOperation.LoadIndex(sessionCur, dialoguesCur, begTime, endTime.AddDays(1)),
                    LoadIndexDelta = -_dbOperation.LoadIndex(sessionOld, dialoguesOld, prevBeg, begTime),

                    CrossIndex = _dbOperation.CrossIndex(dialoguesCur),
                    CrossIndexDelta = -_dbOperation.CrossIndex(dialoguesOld),

                    AdvCount = viewsCur,
                    AdvCountDelta = viewsCur - viewsOld,
                    AnswerCount = (await _analyticContentProvider.GetAnswersAsync(begTime, endTime, companyIds, new List<Guid>(), workerTypeIds)).Count(),
                    AnswerCountDelta = - (await _analyticContentProvider.GetAnswersAsync(prevBeg, begTime, companyIds, new List<Guid>(), workerTypeIds)).Count()//,
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

                var typeIdCross = await _analyticCommonProvider.GetCrossPhraseTypeIdAsync();
                var dialogues = _analyticCommonProvider.GetDialoguesIncludedPhrase(prevBeg, endTime, companyIds, workerTypeIds, applicationUserIds)
                        .Select(p => new DialogueInfo
                         {
                             DialogueId = p.DialogueId,
                             ApplicationUserId = p.ApplicationUserId,
                             BegTime = p.BegTime,
                             EndTime = p.EndTime,
                             CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count()
                         })
                         .ToList();

                var sessions = await _analyticCommonProvider.GetSessionInfoAsync(prevBeg, endTime, companyIds, workerTypeIds, applicationUserIds);

                var sessionCur = sessions.Where(p => p.BegTime.Date >= begTime).ToList();
                var sessionOld = sessions.Where(p => p.BegTime.Date < begTime).ToList();

                ////-----------------FOR BRANCH---------------------------------------------------------------
                List<BenchmarkModel> benchmarksList = (await _analyticHomeProvider.GetBenchmarksList(begTime, endTime, companyIds)).ToList();

                var dialoguesCur = dialogues.Where(p => p.BegTime >= begTime).ToList();
                var dialoguesOld = dialogues.Where(p => p.BegTime < begTime).ToList();

                double? crossIndexIndustryAverage = null, crossIndexIndustryBenchmark = null;
                double? loadIndexIndustryAverage = null, loadIndexIndustryBenchmark = null;

                var crossIndex = _dbOperation.CrossIndex(dialoguesCur);
                var loadIndex = _dbOperation.LoadIndex(sessionCur, dialoguesCur, begTime, endTime.AddDays(1));
                var dialoguesCount = _dbOperation.DialoguesCount(dialoguesCur);

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
                    DialoguesCountDelta = dialoguesCount - _dbOperation.DialoguesCount(dialoguesOld),

                    CrossIndex = _dbOperation.CrossIndex(dialoguesCur),
                    CrossIndexDelta = crossIndex - _dbOperation.CrossIndex(dialoguesOld),

                    CrossIndexIndustryAverage = crossIndexIndustryAverage,
                    CrossIndexIndustryBenchmark = crossIndexIndustryBenchmark,

                    LoadIndex = loadIndex,
                    LoadIndexDelta = loadIndex -_dbOperation.LoadIndex(sessionOld, dialoguesOld, prevBeg, begTime),

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