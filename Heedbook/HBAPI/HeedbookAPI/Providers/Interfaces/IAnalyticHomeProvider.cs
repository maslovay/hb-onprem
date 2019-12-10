using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using UserOperations.Models.Get.HomeController;

namespace UserOperations.Providers
{
    public interface IAnalyticHomeProvider
    {
        Task<IEnumerable<BenchmarkModel>> GetBenchmarksList(DateTime begTime, DateTime endTime, List<Guid> companyIds);
        double? GetBenchmarkIndustryAvg(List<BenchmarkModel> benchmarksList, string banchmarkName);
        double? GetBenchmarkIndustryMax(List<BenchmarkModel> benchmarksList, string banchmarkName);
        Task<IEnumerable<Guid?>> GetIndustryIdsAsync(List<Guid> companyIds);
        Task<int> GetSessionOnline(List<Guid> companyIds, List<Guid> workerTypeIds);
        Task<IEnumerable<SessionInfo>> GetSessionInfoAsync( DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> workerTypeIds, List<Guid> userIds = null);
        IQueryable<Dialogue> GetDialoguesIncludedPhrase(DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> workerTypeIds, List<Guid> applicationUserIds = null);
        Task<Guid> GetCrossPhraseTypeIdAsync();
        Task<List<SlideShowInfo>> GetSlideShowWithDialogueIdFilteredByPoolAsync(
          DateTime begTime,
          DateTime endTime,
          List<Guid> companyIds,
          List<Guid> applicationUserIds,
          List<Guid> workerTypeIds,
          bool isPool,
          List<DialogueInfo> dialogues);
        Task<List<SlideShowInfo>> GetSlideShowWithDialogueIdFilteredByPoolAsync(
          DateTime begTime,
          DateTime endTime,
          List<Guid> companyIds,
          List<Guid> applicationUserIds,
          List<Guid> workerTypeIds,
          bool isPool,
          List<DialogueInfoWithFrames> dialogues);
        Task<List<ApplicationUser>> GetEmployees(
          DateTime endTime, 
          List<Guid> companyIds = null, 
          List<Guid> applicationUserIds = null, 
          List<Guid> workerTypeIds = null);
        Task<IEnumerable<CampaignContentAnswer>> GetAnswersAsync(
          DateTime begTime, 
          DateTime endTime, 
          List<Guid> companyIds, 
          List<Guid> applicationUserIds, 
          List<Guid> workerTypeIds);
    }
}
