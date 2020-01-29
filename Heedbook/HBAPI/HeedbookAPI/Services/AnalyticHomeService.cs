using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using UserOperations.Utils;
using System.Threading.Tasks;
using UserOperations.Models.Get.HomeController;
using UserOperations.Utils.AnalyticHomeUtils;
using HBData.Models;
using HBData.Repository;
using Microsoft.EntityFrameworkCore;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Services
{
    public class AnalyticHomeService
    {
        private readonly IGenericRepository _repository;
        private readonly LoginService _loginService;
        private readonly RequestFilters _requestFilters;
        private readonly AnalyticHomeUtils _utils;

        public AnalyticHomeService(
            IGenericRepository repository,
            LoginService loginService,
            RequestFilters requestFilters,
            AnalyticHomeUtils utils
            )
        {
            _repository = repository;
            _loginService = loginService;
            _requestFilters = requestFilters;
            _utils = utils;
        }


        public async Task<string> GetDashboard( string beg, string end,
                                                        List<Guid> companyIds, List<Guid> corporationIds,
                                                        List<Guid> deviceIds)
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();

                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var sessions = await GetSessionInfoAsync(prevBeg, endTime, companyIds, deviceIds);
                var sessionCur = sessions?.Where(p => p.BegTime.Date >= begTime).ToList();
                var sessionOld = sessions?.Where(p => p.BegTime.Date < begTime).ToList();
                var typeIdCross = await GetCrossPhraseTypeIdAsync();

                var dialogues = GetDialoguesIncludedPhrase(prevBeg, endTime, companyIds, null, deviceIds)
                       .Select(p => new DialogueInfo
                       {
                           DialogueId = p.DialogueId,
                           ApplicationUserId = p.ApplicationUserId,
                           DeviceId = p.DeviceId,
                           FullName = p.ApplicationUser.FullName,
                           BegTime = p.BegTime,
                           EndTime = p.EndTime,
                           CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count(),
                           SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                           SatisfactionScoreBeg = p.DialogueClientSatisfaction.FirstOrDefault().BegMoodByNN,
                           SatisfactionScoreEnd = p.DialogueClientSatisfaction.FirstOrDefault().EndMoodByNN
                       }).ToList();

                ////-----------------FOR BRANCH---------------------------------------------------------------
                List<BenchmarkModel> benchmarksList = (await GetBenchmarksList(begTime, endTime, companyIds)).ToList();

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
                    result.SatisfactionIndexIndustryAverage = GetBenchmarkIndustryAvg(benchmarksList, "SatisfactionIndexIndustryAvg");
                    result.SatisfactionIndexIndustryBenchmark = GetBenchmarkIndustryMax(benchmarksList, "SatisfactionIndexIndustryBenchmark");

                    result.LoadIndexIndustryAverage = GetBenchmarkIndustryAvg(benchmarksList, "LoadIndexIndustryAvg");
                    result.LoadIndexIndustryBenchmark = GetBenchmarkIndustryMax(benchmarksList, "LoadIndexIndustryBenchmark"); 

                    result.CrossIndexIndustryAverage = GetBenchmarkIndustryAvg(benchmarksList, "CrossIndexIndustryAvg"); 
                    result.CrossIndexIndustryBenchmark = GetBenchmarkIndustryMax(benchmarksList, "CrossIndexIndustryBenchmark");
                }           

                result.NumberOfDialoguesPerEmployeesDelta += result.NumberOfDialoguesPerEmployees;
                result.AvgWorkingTimeEmployeesDelta +=result.AvgWorkingTimeEmployees;
                result.EmployeeCountDelta += result.EmployeeCount;
                result.CrossIndexDelta += result.CrossIndex;
                result.LoadIndexDelta += result.LoadIndex;
                result.SatisfactionIndexDelta += result.SatisfactionIndex;
                result.DialoguesCountDelta += result.DialoguesCount;
                result.DialogueDurationDelta += result.DialogueDuration;
                var json = JsonConvert.SerializeObject(result);
                return json;
        }

        public async Task<NewDashboardInfo> GetNewDashboard( string beg, string end,
                                                             List<Guid> companyIds, List<Guid> corporationIds,
                                                             List<Guid> deviceIds)
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();

                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var sessions = await GetSessionInfoAsync(prevBeg, endTime, companyIds, deviceIds);
                var sessionCur = sessions?.Where(p => p.BegTime.Date >= begTime).ToList();
                var sessionOld = sessions?.Where(p => p.BegTime.Date < begTime).ToList();
                var typeIdCross = await GetCrossPhraseTypeIdAsync();

                var dialogues = GetDialoguesIncludedPhrase(prevBeg, endTime, companyIds, null, deviceIds)
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
                List<BenchmarkModel> benchmarksList = (await GetBenchmarksList(begTime, endTime, companyIds)).ToList();

                var dialoguesCur = dialogues.Where(p => p.BegTime >= begTime).ToList();
                var dialoguesOld = dialogues.Where(p => p.BegTime < begTime).ToList();

                var slideShowSessionsInDialoguesOld = await GetSlideShowWithDialogueIdFilteredByPoolAsync(prevBeg, begTime, companyIds, new List<Guid?>(), deviceIds, false, dialoguesOld);
                var viewsOld = slideShowSessionsInDialoguesOld.Where(x => x.Campaign == null || !x.Campaign.IsSplash).Count();

                var slideShowSessionsInDialoguesCur = await GetSlideShowWithDialogueIdFilteredByPoolAsync(begTime, endTime, companyIds, new List<Guid?>(), deviceIds, false, dialoguesCur);
                var viewsCur = slideShowSessionsInDialoguesCur.Where(x => x.Campaign == null || !x.Campaign.IsSplash).Count();

                var result = new NewDashboardInfo()
                {
                    ClientsCount = _utils.DialoguesCount(dialoguesCur),
                    ClientsCountDelta = -_utils.DialoguesCount(dialoguesOld),

                    EmployeeCount = (await GetEmployees(endTime, companyIds, null)).Count(),
                    BestEmployees = _utils.BestThreeEmployees(dialoguesCur, sessionCur, begTime, endTime.AddDays(1)),

                    SatisfactionIndex = _utils.SatisfactionIndex(dialoguesCur),
                    SatisfactionIndexDelta = -_utils.SatisfactionIndex(dialoguesOld),

                    LoadIndex = _utils.LoadIndex(sessionCur, dialoguesCur, begTime, endTime.AddDays(1)),
                    LoadIndexDelta = -_utils.LoadIndex(sessionOld, dialoguesOld, prevBeg, begTime),

                    CrossIndex = _utils.CrossIndex(dialoguesCur),
                    CrossIndexDelta = -_utils.CrossIndex(dialoguesOld),

                    AdvCount = viewsCur,
                    AdvCountDelta = viewsCur - viewsOld,
                    AnswerCount = (await GetAnswersAsync(begTime, endTime, companyIds, new List<Guid?>(), deviceIds)).Count(),
                    AnswerCountDelta = - (await GetAnswersAsync(prevBeg, begTime, companyIds, new List<Guid?>(), deviceIds)).Count()
                };

                //---benchmarks
                if (benchmarksList != null && benchmarksList.Count() != 0)
                {
                    result.SatisfactionIndexIndustryAverage = GetBenchmarkIndustryAvg(benchmarksList, "SatisfactionIndexIndustryAvg");
                    result.SatisfactionIndexIndustryBenchmark = GetBenchmarkIndustryMax(benchmarksList, "SatisfactionIndexIndustryBenchmark");

                    result.LoadIndexIndustryAverage = GetBenchmarkIndustryAvg(benchmarksList, "LoadIndexIndustryAvg");
                    result.LoadIndexIndustryBenchmark = GetBenchmarkIndustryMax(benchmarksList, "LoadIndexIndustryBenchmark");

                    result.CrossIndexIndustryAverage = GetBenchmarkIndustryAvg(benchmarksList, "CrossIndexIndustryAvg");
                    result.CrossIndexIndustryBenchmark = GetBenchmarkIndustryMax(benchmarksList, "CrossIndexIndustryBenchmark");
                }

                result.CrossIndexDelta += result.CrossIndex;
                result.LoadIndexDelta += result.LoadIndex;
                result.SatisfactionIndexDelta += result.SatisfactionIndex;
                result.AnswerCountDelta += result.AnswerCount;
                result.AdvCountDelta += result.AdvCount;
                result.ClientsCountDelta += result.ClientsCount;
                return result;
        }

        public async Task<string> GetDashboardFiltered(string beg, string end,
                                                      List<Guid?> applicationUserIds, List<Guid> companyIds, List<Guid> corporationIds, List<Guid> deviceIds)
        {
                var role = _loginService.GetCurrentRoleName();
                var companyId = _loginService.GetCurrentCompanyId();
                var begTime = _requestFilters.GetBegDate(beg);
                var endTime = _requestFilters.GetEndDate(end);
                var prevBeg = begTime.AddDays(-endTime.Subtract(begTime).TotalDays);

                _requestFilters.CheckRolesAndChangeCompaniesInFilter(ref companyIds, corporationIds, role, companyId);

                var typeIdCross = await GetCrossPhraseTypeIdAsync();
                var dialogues = GetDialoguesIncludedPhrase(prevBeg, endTime, companyIds, applicationUserIds, deviceIds)
                        .Select(p => new DialogueInfo
                         {
                             DialogueId = p.DialogueId,
                             ApplicationUserId = p.ApplicationUserId,
                             DeviceId = p.DeviceId,
                             BegTime = p.BegTime,
                             EndTime = p.EndTime,
                             CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count()
                         })
                         .ToList();

                var sessions = await GetSessionInfoAsync(prevBeg, endTime, companyIds, deviceIds);

                var sessionCur = sessions.Where(p => p.BegTime.Date >= begTime).ToList();
                var sessionOld = sessions.Where(p => p.BegTime.Date < begTime).ToList();

                ////-----------------FOR BRANCH---------------------------------------------------------------
                List<BenchmarkModel> benchmarksList = (await GetBenchmarksList(begTime, endTime, companyIds)).ToList();

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
                    loadIndexIndustryAverage = GetBenchmarkIndustryAvg(benchmarksList, "LoadIndexIndustryAvg");
                    loadIndexIndustryBenchmark = GetBenchmarkIndustryMax(benchmarksList, "LoadIndexIndustryBenchmark");

                    crossIndexIndustryAverage = GetBenchmarkIndustryAvg(benchmarksList, "CrossIndexIndustryAvg");
                    crossIndexIndustryBenchmark = GetBenchmarkIndustryMax(benchmarksList, "CrossIndexIndustryBenchmark");
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
            var res = JsonConvert.SerializeObject(result);
            return res;
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


        //---PRIVATE---
        private async Task<IEnumerable<BenchmarkModel>> GetBenchmarksList(DateTime begTime, DateTime endTime, List<Guid> companyIds)
        {
            var industryIds = await GetIndustryIdsAsync(companyIds);
            try
            {
                var benchmarksList = _repository.Get<Benchmark>().Where(x => x.Day >= begTime && x.Day <= endTime
                                                             && industryIds.Contains(x.IndustryId))
                                                              .Join(_repository.Get<BenchmarkName>(),
                                                              bench => bench.BenchmarkNameId,
                                                              names => names.Id,
                                                              (bench, names) => new BenchmarkModel { Name = names.Name, Value = bench.Value });
                return benchmarksList;
            }
            catch
            {
                return null;
            }
        }

        private double? GetBenchmarkIndustryAvg(List<BenchmarkModel> benchmarksList, string banchmarkName)
        {
            if (benchmarksList == null || benchmarksList.Count() == 0) return null;
            return benchmarksList.Any(x => x.Name == banchmarkName) ?
                 (double?)benchmarksList.Where(x => x.Name == banchmarkName).Average(x => x.Value) : null;
        }

        private double? GetBenchmarkIndustryMax(List<BenchmarkModel> benchmarksList, string banchmarkName)
        {
            if (benchmarksList == null || benchmarksList.Count() == 0) return null;
            return benchmarksList.Any(x => x.Name == banchmarkName) ?
                 (double?)benchmarksList.Where(x => x.Name == banchmarkName).Max(x => x.Value) : null;
        }

        private async Task<IEnumerable<Guid?>> GetIndustryIdsAsync(List<Guid> companyIds)
        {
            var industryIds = (await _repository.FindByConditionAsync<Company>(x => !companyIds.Any() || companyIds.Contains(x.CompanyId)))?
                     .Select(x => x.CompanyIndustryId).Distinct();
            return industryIds;
        }

        private async Task<int> GetSessionOnline(List<Guid> companyIds, List<Guid> deviceIds)
        {
            return await _repository.GetAsQueryable<Session>().Where(p =>
                     p.StatusId == 6
                     && (!companyIds.Any() || companyIds.Contains((Guid)p.Device.CompanyId))
                     && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId))).CountAsync();
        }
        private async Task<IEnumerable<SessionInfo>> GetSessionInfoAsync(
                        DateTime begTime, DateTime endTime, 
                        List<Guid> companyIds,
                        List<Guid> deviceIds = null)
        {
            var sessions = await _repository.GetAsQueryable<Session>()
                         .Where(p => p.BegTime >= begTime
                                 && p.EndTime <= endTime
                                 && p.StatusId == 7
                                 && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                                 && (deviceIds == null || (!deviceIds.Any() || deviceIds.Contains(p.DeviceId))))
                         .Select(p => new SessionInfo
                         {
                             ApplicationUserId = p.ApplicationUserId,
                             DeviceId = p.DeviceId,
                             BegTime = p.BegTime,
                             EndTime = p.EndTime
                         })
                         .ToListAsync();
            return sessions;
        }
        private IQueryable<Dialogue> GetDialoguesIncludedPhrase(DateTime begTime, DateTime endTime, 
                                List<Guid> companyIds,
                                List<Guid?> applicationUserIds = null,
                                List<Guid> deviceIds = null)
        {
            var dialogues = _repository.GetAsQueryable<Dialogue>()
                       .Include(p => p.ApplicationUser)
                       .Include(p => p.DialogueClientSatisfaction)
                       .Include(p => p.DialoguePhrase)
                       .Where(p => p.BegTime >= begTime
                               && p.EndTime <= endTime
                               && p.StatusId == 3
                               && p.InStatistic == true
                               && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                               && (applicationUserIds == null 
                                        || (!applicationUserIds.Any() 
                                                || (p.ApplicationUserId != null && applicationUserIds.Contains(p.ApplicationUserId))))
                               && (deviceIds == null
                                        || (!deviceIds.Any() || deviceIds.Contains(p.DeviceId))))
                        .AsQueryable();
            return dialogues;
        }
        private async Task<Guid> GetCrossPhraseTypeIdAsync()
        {
            var typeIdCross = await _repository.GetAsQueryable<PhraseType>()
                    .Where(p => p.PhraseTypeText == "Cross")
                    .Select(p => p.PhraseTypeId)
                    .FirstOrDefaultAsync();
            return typeIdCross;
        }
        private async Task<List<SlideShowInfo>> GetSlideShowWithDialogueIdFilteredByPoolAsync(
          DateTime begTime,
          DateTime endTime,
          List<Guid> companyIds,
          List<Guid?> applicationUserIds,
          List<Guid> deviceIds,
          bool isPool,
          List<DialogueInfo> dialogues
          )
        {
            var slideShows = await _repository.GetAsQueryable<SlideShowSession>()
                .Where(p => p.IsPoll == isPool
                    && p.BegTime >= begTime
                    && p.BegTime <= endTime
                    && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains((Guid)p.ApplicationUserId))
                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId))
                    && p.CampaignContent != null)
                .Select(p =>
                    new SlideShowInfo
                    {
                        BegTime = p.BegTime,
                        ContentId = p.CampaignContent.ContentId,
                        Campaign = p.CampaignContent.Campaign,
                        ContentType = p.ContentType,
                        ContentName = p.CampaignContent.Content != null ? p.CampaignContent.Content.Name : null,
                        EndTime = p.EndTime,
                        IsPoll = p.IsPoll,
                        Url = p.Url,
                        ApplicationUserId = (Guid)p.ApplicationUserId,
                        DialogueId = dialogues.FirstOrDefault(x => x.BegTime <= p.BegTime
                                && x.EndTime >= p.BegTime
                                && x.ApplicationUserId == p.ApplicationUserId)
                            .DialogueId
                    })
                .Where(x => x.DialogueId != null && x.DialogueId != default(Guid))
                .ToListAsyncSafe();
            return slideShows;
        }
        private async Task<List<SlideShowInfo>> GetSlideShowWithDialogueIdFilteredByPoolAsync(
           DateTime begTime,
           DateTime endTime,
           List<Guid> companyIds,
           List<Guid?> applicationUserIds,
           List<Guid> deviceIds,
           bool isPool,
           List<DialogueInfoWithFrames> dialogues
           )
        {
            var slideShows = await _repository.GetAsQueryable<SlideShowSession>()
                .Where(p => p.IsPoll == isPool
                    && p.BegTime >= begTime
                    && p.BegTime <= endTime
                    && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains((Guid)p.ApplicationUserId))
                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId))
                    && p.CampaignContent != null)
                .Select(p =>
                    new SlideShowInfo
                    {
                        BegTime = p.BegTime,
                        ContentId = p.CampaignContent.ContentId,
                        Campaign = p.CampaignContent.Campaign,
                        ContentType = p.ContentType,
                        ContentName = p.CampaignContent.Content != null ? p.CampaignContent.Content.Name : null,
                        EndTime = p.EndTime,
                        IsPoll = p.IsPoll,
                        Url = p.Url,
                        ApplicationUserId = p.ApplicationUserId,
                        DeviceId = p.DeviceId,
                        DialogueId = dialogues.FirstOrDefault(x => x.BegTime <= p.BegTime
                                && x.EndTime >= p.BegTime
                                && x.ApplicationUserId == p.ApplicationUserId)
                            .DialogueId,
                        DialogueFrames = dialogues.FirstOrDefault(x => x.BegTime <= p.BegTime
                                && x.EndTime >= p.BegTime
                                && x.ApplicationUserId == p.ApplicationUserId)
                            .DialogueFrame,
                        Age = dialogues.FirstOrDefault(x => x.BegTime <= p.BegTime
                                && x.EndTime >= p.BegTime
                                && x.ApplicationUserId == p.ApplicationUserId)
                            .Age,
                        Gender = dialogues.FirstOrDefault(x => x.BegTime <= p.BegTime
                                && x.EndTime >= p.BegTime
                                && x.ApplicationUserId == p.ApplicationUserId)
                            .Gender
                    })
                .Where(x => x.DialogueId != null && x.DialogueId != default(Guid))
                .ToListAsyncSafe();
            return slideShows;
        }
        private async Task<List<ApplicationUser>> GetEmployees(
                    DateTime endTime, 
                    List<Guid> companyIds = null, 
                    List<Guid?> applicationUserIds = null)
        {
            var employeeRole = (await _repository.FindOrNullOneByConditionAsync<ApplicationRole>(x => x.Name == "Employee")).Id;
            var users = _repository.GetAsQueryable<ApplicationUser>()
                   .Where(p =>
                       p.CreationDate <= endTime
                       && p.StatusId == 3
                       && (companyIds == null || (!companyIds.Any() || companyIds.Contains((Guid)p.CompanyId)))
                       && (applicationUserIds == null || (!applicationUserIds.Any() || applicationUserIds.Contains(p.Id)))
                       && (p.UserRoles.Any(x => x.RoleId == employeeRole))
                   ).ToList();
            return users;
        }
        private async Task<IEnumerable<CampaignContentAnswer>> GetAnswersAsync(
                        DateTime begTime, DateTime endTime, 
                        List<Guid> companyIds, 
                        List<Guid?> applicationUserIds, 
                        List<Guid> deviceIds)
        {
            var result = await _repository.GetAsQueryable<CampaignContentAnswer>()
                                     .Include(x => x.CampaignContent)
                                     .Where(p =>
                                    p.CampaignContent != null
                                    && (p.Time >= begTime && p.Time <= endTime)
                                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                                    && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId))
                                    && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))).ToListAsyncSafe();
            return result;
        }
    }

}