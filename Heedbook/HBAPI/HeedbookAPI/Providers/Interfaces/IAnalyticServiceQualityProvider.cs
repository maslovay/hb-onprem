using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using UserOperations.Models.Get.AnalyticServiceQualityController;

namespace UserOperations.Providers
{
    public interface IAnalyticServiceQualityProvider
    {
        Task<List<ComponentsPhraseInfo>> GetComponentsPhraseInfo();
        Task<List<ComponentsDialogueInfo>> GetComponentsDialogueInfo(
            DateTime begTime, 
            DateTime endTime, 
            List<Guid> companyIds, 
            List<Guid> applicationUserIds, 
            List<Guid> workerTypeIds,
            Guid loyaltyTypeId);
        IQueryable<Dialogue> GetDialoguesIncludedPhrase(
            DateTime begTime, 
            DateTime endTime, 
            List<Guid> companyIds, 
            List<Guid> workerTypeIds, 
            List<Guid> applicationUserIds);
        Task<IEnumerable<PhraseType>> GetPhraseTypes();
        Task<List<RatingDialogueInfo>> GetRatingDialogueInfos(
            DateTime begTime, 
            DateTime endTime, 
            List<Guid> companyIds, 
            List<Guid> applicationUserIds, 
            List<Guid> workerTypeIds,
            Guid typeIdLoyalty);
        Task<List<DialogueInfo>> GetDialogueInfos(
            DateTime begTime, 
            DateTime endTime, 
            List<Guid> companyIds, 
            List<Guid> applicationUserIds, 
            List<Guid> workerTypeIds);
    }
}