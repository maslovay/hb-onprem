using HBData.Models;
using HBData.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.Models.Get.HomeController;

namespace UserOperations.Providers
{
    public class AnalyticHomeProvider : IAnalyticHomeProvider
    {
        private readonly IGenericRepository _repository;
        public AnalyticHomeProvider(IGenericRepository repository)
        {
            _repository = repository;
        }

        //public IQueryable<DialogueHint> GetDialogueHints(DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> applicationUserIds, List<Guid> workerTypeIds)
        //{
        //    var hints = _context.DialogueHints
        //            .Include(p => p.Dialogue)
        //            .Include(p => p.Dialogue.ApplicationUser)
        //            .Where(p => p.Dialogue.BegTime >= begTime && p.Dialogue.EndTime <= endTime
        //                    && p.Dialogue.InStatistic == true
        //                    && (!companyIds.Any() || companyIds.Contains((Guid)p.Dialogue.ApplicationUser.CompanyId))
        //                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.Dialogue.ApplicationUserId))
        //                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.Dialogue.ApplicationUser.WorkerTypeId)))
        //           .AsQueryable();
        //    return hints;
        //}

        public async Task<IEnumerable<BenchmarkModel>> GetBenchmarksList(DateTime begTime, DateTime endTime, List<Guid> companyIds)
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

        public double? GetBenchmarkIndustryAvg(List<BenchmarkModel> benchmarksList, string banchmarkName)
        {
            if (benchmarksList == null || benchmarksList.Count() == 0) return null;
           return benchmarksList.Any(x => x.Name == banchmarkName) ? 
                (double?)benchmarksList.Where(x => x.Name == banchmarkName).Average(x => x.Value) : null;
        }

        public double? GetBenchmarkIndustryMax(List<BenchmarkModel> benchmarksList, string banchmarkName)
        {
            if (benchmarksList == null || benchmarksList.Count() == 0) return null;
            return benchmarksList.Any(x => x.Name == banchmarkName) ?
                 (double?)benchmarksList.Where(x => x.Name == banchmarkName).Max(x => x.Value) : null;
        }

        public async Task<IEnumerable<Guid?>> GetIndustryIdsAsync(List<Guid> companyIds)
        {
            var industryIds = (await _repository.FindByConditionAsync<Company>(x => !companyIds.Any() || companyIds.Contains(x.CompanyId)))?
                     .Select(x => x.CompanyIndustryId).Distinct();
            return industryIds;
        }

        public async Task<int> GetSessionOnline( List<Guid> companyIds, List<Guid> workerTypeIds)
        {
            return await _repository.GetAsQueryable<Session>().Where(p =>
                     p.StatusId == 6
                     && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                     && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))).CountAsync();
        }
        public async Task<IEnumerable<SessionInfo>> GetSessionInfoAsync( DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> workerTypeIds, List<Guid> userIds = null)
        {
            var sessions = await _repository.GetAsQueryable<Session>()
                         .Where(p => p.BegTime >= begTime
                                 && p.EndTime <= endTime
                                 && p.StatusId == 7
                                 && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                                 && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
                                 && (userIds == null || (!userIds.Any() || userIds.Contains(p.ApplicationUserId))))
                         .Select(p => new SessionInfo
                         {
                             ApplicationUserId = p.ApplicationUserId,
                             BegTime = p.BegTime,
                             EndTime = p.EndTime
                         })
                         .ToListAsync();
            return sessions;
        }
        public IQueryable<Dialogue> GetDialoguesIncludedPhrase(DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> workerTypeIds, List<Guid> applicationUserIds = null)
        {
            var dialogues = _repository.GetAsQueryable<Dialogue>()
                       .Include(p => p.ApplicationUser)
                       .Include(p => p.DialogueClientSatisfaction)
                       .Include(p => p.DialoguePhrase)
                       .Where(p => p.BegTime >= begTime
                               && p.EndTime <= endTime
                               && p.StatusId == 3
                               && p.InStatistic == true
                               && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                               && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
                               && (applicationUserIds == null || (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId)))).AsQueryable();
            return dialogues;
        }
        public async Task<Guid> GetCrossPhraseTypeIdAsync()
        {
            var typeIdCross = await _repository.GetAsQueryable<PhraseType>()
                    .Where(p => p.PhraseTypeText == "Cross")
                    .Select(p => p.PhraseTypeId)
                    .FirstOrDefaultAsync();
            return typeIdCross;
        }
        public async Task<List<SlideShowInfo>> GetSlideShowWithDialogueIdFilteredByPoolAsync(
          DateTime begTime,
          DateTime endTime,
          List<Guid> companyIds,
          List<Guid> applicationUserIds,
          List<Guid> workerTypeIds,
          bool isPool,
          List<DialogueInfo> dialogues
          )
        {
            var slideShows = await _repository.GetAsQueryable<SlideShowSession>()
                .Where(p => p.IsPoll == isPool
                    && p.BegTime >= begTime
                    && p.BegTime <= endTime
                    && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains((Guid)p.ApplicationUserId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
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
        public async Task<List<SlideShowInfo>> GetSlideShowWithDialogueIdFilteredByPoolAsync(
           DateTime begTime,
           DateTime endTime,
           List<Guid> companyIds,
           List<Guid> applicationUserIds,
           List<Guid> workerTypeIds,
           bool isPool,
           List<DialogueInfoWithFrames> dialogues
           )
        {         
            var slideShows =  await _repository.GetAsQueryable<SlideShowSession>()
                .Where(p => p.IsPoll == isPool
                    && p.BegTime >= begTime
                    && p.BegTime <= endTime
                    && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains((Guid)p.ApplicationUserId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
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
        public async Task<List<ApplicationUser>> GetEmployees(DateTime endTime, List<Guid> companyIds = null, List<Guid> applicationUserIds = null, List<Guid> workerTypeIds = null)
        {
            var employeeRole = (await _repository.FindOrNullOneByConditionAsync<ApplicationRole>(x => x.Name == "Employee")).Id;
            var users =  _repository.GetAsQueryable<ApplicationUser>()
                   .Where(p =>
                       p.CreationDate <= endTime
                       && p.StatusId == 3
                       && (companyIds == null || (!companyIds.Any() || companyIds.Contains((Guid)p.CompanyId)))
                       && (applicationUserIds == null || ( !applicationUserIds.Any() || applicationUserIds.Contains(p.Id)))
                       && (workerTypeIds == null || (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.WorkerTypeId)))
                       && (p.UserRoles.Any(x => x.RoleId == employeeRole))
                   ).ToList();
            return users;
        }
        public async Task<IEnumerable<CampaignContentAnswer>> GetAnswersAsync(DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> applicationUserIds, List<Guid> workerTypeIds)
        {
            var result = await _repository.GetAsQueryable<CampaignContentAnswer>()
                                     .Include(x => x.CampaignContent)
                                     .Where(p =>
                                    p.CampaignContent != null
                                    && (p.Time >= begTime && p.Time <= endTime)
                                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
                                    && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))).ToListAsyncSafe();
            return result;
        }
    }
}
