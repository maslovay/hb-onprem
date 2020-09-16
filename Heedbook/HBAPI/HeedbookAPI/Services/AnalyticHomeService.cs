using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Threading.Tasks;
using UserOperations.Models.Get.HomeController;
using HBData.Models;
using HBData.Repository;
using Microsoft.EntityFrameworkCore;
using UserOperations.Models.AnalyticModels;
using UserOperations.Utils.Interfaces;
using HBLib.Utils.Interfaces;

namespace UserOperations.Services
{
    public class AnalyticHomeService
    {
        private readonly IGenericRepository _repository;
        private readonly ILoginService _loginService;
        private readonly IRequestFilters _requestFilters;
        private readonly IAnalyticHomeUtils _utils;
        private readonly IDBOperations _dbOperations;

        public AnalyticHomeService(
            IGenericRepository repository,
            ILoginService loginService,
            IRequestFilters requestFilters,
            IAnalyticHomeUtils utils,
            IDBOperations dbOperations
            )
        {
            _repository = repository;
            _loginService = loginService;
            _requestFilters = requestFilters;
            _utils = utils;
            _dbOperations = dbOperations;
        }

        public async Task<NewDashboardInfo> GetNewDashboard(string beg, string end,
                                                             List<Guid> companyIds, List<Guid> corporationIds,
                                                             List<Guid> deviceIds)
        {
            int active = 3;
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


            var workingTimes = _repository.GetAsQueryable<WorkingTime>().Where(x => !companyIds.Any() || companyIds.Contains(x.CompanyId)).ToArray();
            var devicesFiltered = _repository.GetAsQueryable<Device>()
                                  .Where(x => companyIds.Contains(x.CompanyId)
                                      && (!deviceIds.Any() || deviceIds.Contains(x.DeviceId))
                                      && x.StatusId == active)
                                  .ToList();
            var timeTableForDevices = _dbOperations.WorkingTimeDoubleList(workingTimes, begTime, endTime, companyIds, devicesFiltered, role);

            var dialogues = GetDialoguesIncluded(prevBeg, endTime, companyIds, null, deviceIds)
                       .Select(p => new DialogueInfo
                       {
                           DialogueId = p.DialogueId,
                           ApplicationUserId = p.ApplicationUserId,
                           BegTime = p.BegTime,
                           EndTime = p.EndTime,
                           CrossCount = p.DialoguePhrase.Where(q => q.Phrase.PhraseTypeId == typeIdCross).Count(),
                           SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                           SatisfactionScoreBeg = p.DialogueClientSatisfaction.FirstOrDefault().BegMoodByNN,
                           SatisfactionScoreEnd = p.DialogueClientSatisfaction.FirstOrDefault().EndMoodByNN,
                           SmilesShare = p.DialogueFrame.Average(x => x.HappinessShare),
                           DeviceId = p.DeviceId,
                           CompanyId = p.Device.CompanyId,
                           SlideShowSessions = p.SlideShowSessions
                       }).ToList();
                dialogues = dialogues.Select(p => 
                        {
                            p.IsInWorkingTime = _dbOperations.CheckIfDialogueInWorkingTime(
                                p, 
                                workingTimes.Where(x => x.CompanyId == p.CompanyId).ToArray());
                            return p;
                        })
                    .ToList();
                ////-----------------FOR BRANCH---------------------------------------------------------------
                List<BenchmarkModel> benchmarksList = (await GetBenchmarksList(begTime, endTime, companyIds)).ToList();

                var dialoguesCur = dialogues.Where(p => p.BegTime >= begTime).ToList();
                var dialoguesOld = dialogues.Where(p => p.BegTime < begTime).ToList();

                var dialoguesDevicesCur = dialoguesCur.Where(x => x.IsInWorkingTime).ToList();
                var dialoguesDevicesOld = dialoguesOld.Where(x => x.IsInWorkingTime).ToList();

                // var slideShowSessionsInDialoguesOld = await GetSlideShowWithDialogueIdFilteredByPoolAsync(false, dialoguesOld.Select(x => x.DialogueId).ToList());
                // var slideShowSessionsInDialoguesCur = await GetSlideShowWithDialogueIdFilteredByPoolAsync(false, dialoguesCur.Select(x => x.DialogueId).ToList());
                var slideShowSessionsInDialoguesOld = dialogues.SelectMany(p => p.SlideShowSessions)
                    .Where(p => p.BegTime < begTime).ToList();
                var slideShowSessionsInDialoguesCur = dialogues.SelectMany(p => p.SlideShowSessions)
                    .Where(p => p.BegTime >= begTime).ToList();
                var viewsCur = slideShowSessionsInDialoguesCur.Count();

                var result = new NewDashboardInfo()
                {
                    ClientsCount = _utils.DialoguesCount(dialoguesCur),
                    ClientsCountDelta = -_utils.DialoguesCount(dialoguesOld),

                    EmployeeCount = (await GetEmployees(endTime, companyIds, null)).Count(),
                    //BestEmployees = _utils.BestThreeEmployees(dialoguesCur, sessionCur, begTime, endTime.AddDays(1)),

                    SatisfactionIndex = _utils.SatisfactionIndex(dialoguesCur),
                    SatisfactionIndexDelta = -_utils.SatisfactionIndex(dialoguesOld),

                    LoadIndex = _utils.LoadIndexWithTimeTable(timeTableForDevices, dialoguesCur.Where(x => x.ApplicationUserId != null).ToList(), begTime, endTime.AddDays(1)),
                    LoadIndexDelta = -_utils.LoadIndexWithTimeTable(timeTableForDevices, dialoguesOld.Where(x => x.ApplicationUserId != null).ToList(), prevBeg, begTime),

                    CrossIndex = _utils.CrossIndex(dialoguesCur),
                    CrossIndexDelta = -_utils.CrossIndex(dialoguesOld),

                    AdvCount = viewsCur,
                    AdvCountDelta = viewsCur - slideShowSessionsInDialoguesOld.Count(),
                    AnswerCount = (await GetAnswersAsync(begTime, endTime, companyIds, new List<Guid?>(), deviceIds)).Count(),
                    AnswerCountDelta = -(await GetAnswersAsync(prevBeg, begTime, companyIds, new List<Guid?>(), deviceIds)).Count(),

                    SmilesShare = dialoguesCur.Average(x => x.SmilesShare),
                    SmilesShareDelta = dialoguesCur.Average(x => x.SmilesShare) - dialoguesOld.Average(x => x.SmilesShare),

                    WorkloadValueAvgByWorkingTime = _dbOperations.WorklLoadByTimeIndex(timeTableForDevices, dialoguesDevicesCur, begTime, endTime),
                    WorkloadDynamicsWorkingTime = -_dbOperations.WorklLoadByTimeIndex(timeTableForDevices, dialoguesDevicesOld, prevBeg, begTime)
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

                    result.WorkLoadByTimeIndustryAverage = GetBenchmarkIndustryAvg(benchmarksList, "WorkLoadByTimeIndustryAvg");
                    result.WorkLoadByTimeIndustryBenchmark = GetBenchmarkIndustryMax(benchmarksList, "WorkLoadByTimeIndustryBenchmark");
                }

                result.CrossIndexDelta += result.CrossIndex;
                result.LoadIndexDelta += result.LoadIndex;
                result.WorkloadDynamicsWorkingTime += result.WorkloadValueAvgByWorkingTime;
                result.SatisfactionIndexDelta += result.SatisfactionIndex;
                result.AnswerCountDelta += result.AnswerCount;
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
                            CrossCount = p.DialoguePhrase.Where(q => q.Phrase.PhraseTypeId == typeIdCross).Count()
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

                int active = 3;
                var workingTimes = _repository.GetAsQueryable<WorkingTime>().Where(x => !companyIds.Any() || companyIds.Contains(x.CompanyId)).ToArray();
                System.Console.WriteLine($"workingTimes: {JsonConvert.SerializeObject(workingTimes)}");
                var devicesFiltered = _repository.GetAsQueryable<Device>()
                    .Where(x => companyIds.Contains(x.CompanyId)
                        && (!deviceIds.Any() || deviceIds.Contains(x.DeviceId))
                        && x.StatusId == active)
                    .ToList();
                var timeTableForDevices = _dbOperations.WorkingTimeDoubleList(workingTimes, begTime, endTime, companyIds, devicesFiltered, role);

                var crossIndex = _utils.CrossIndex(dialoguesCur);
                var loadIndex = _utils.LoadIndexWithTimeTable(timeTableForDevices, dialoguesCur.Where(x => x.ApplicationUserId != null).ToList(), begTime, endTime.AddDays(1));
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
                    LoadIndexDelta = loadIndex -_utils.LoadIndexWithTimeTable(timeTableForDevices, dialoguesOld.Where(x => x.ApplicationUserId != null).ToList(), prevBeg, begTime),

                    LoadIndexIndustryAverage = loadIndexIndustryAverage,
                    LoadIndexIndustryBenchmark = loadIndexIndustryBenchmark
            };
            var res = JsonConvert.SerializeObject(result);
            return res;
        }

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

        //private async Task<int> GetSessionOnline(List<Guid> companyIds, List<Guid> deviceIds)
        //{
        //    return await _repository.GetAsQueryable<Session>().Where(p =>
        //             p.StatusId == 6
        //             && (!companyIds.Any() || companyIds.Contains((Guid)p.Device.CompanyId))
        //             && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId))).CountAsync();
        //}
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

        private IQueryable<Dialogue> GetDialoguesIncluded(DateTime begTime, DateTime endTime,
                                List<Guid> companyIds,
                                List<Guid?> applicationUserIds = null,
                                List<Guid> deviceIds = null)
        {
            var dialogues = _repository.GetAsQueryable<Dialogue>()
                .Include(p => p.ApplicationUser)
                .Include(p => p.Device)
                .Include(p => p.DialogueClientSatisfaction)
                .Include(p => p.DialogueFrame)
                .Include(p => p.DialoguePhrase)
                .Include(p => p.SlideShowSessions)
                       .Where(p => p.BegTime >= begTime
                               && p.EndTime <= endTime
                               && p.StatusId == 3
                               && p.InStatistic == true
                               && (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                               && (applicationUserIds == null
                                        || (!applicationUserIds.Any()
                                                || (p.ApplicationUserId != null && applicationUserIds.Contains(p.ApplicationUserId))))
                               && (deviceIds == null
                                        || (!deviceIds.Any() || deviceIds.Contains(p.DeviceId))));
            return dialogues;
        }

        private IQueryable<Dialogue> GetDialoguesIncludedPhrase(DateTime begTime, DateTime endTime,
                              List<Guid> companyIds,
                              List<Guid?> applicationUserIds = null,
                              List<Guid> deviceIds = null)
        {
            var dialogues = _repository.GetAsQueryable<Dialogue>()
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
                                        || (!deviceIds.Any() || deviceIds.Contains(p.DeviceId))));
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
          bool isPool,
          List<Guid> dialogueIds
          )
        {
            var slideShows = await _repository.GetAsQueryable<SlideShowSession>()
                .Where(p => p.IsPoll == isPool
                    && p.DialogueId != null && dialogueIds.Contains((Guid)p.DialogueId)
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
                        DialogueId = p.DialogueId
                    })
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