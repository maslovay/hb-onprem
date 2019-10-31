using HBData;
using HBData.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.Controllers;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Providers
{
    public interface IAnalyticCommonProvider
    {
         Task<IEnumerable<SessionInfo>> GetSessionInfoAsync(DateTime begTime, DateTime endTime, 
             List<Guid> companyIds, List<Guid> workerTypeIds, List<Guid> userIds = null);

         IQueryable<Dialogue> GetDialoguesIncludedPhrase(DateTime begTime, DateTime endTime, 
             List<Guid> companyIds, List<Guid> workerTypeIds, List<Guid> applicationUserIds = null);

         IQueryable<Dialogue> GetDialoguesIncludedClientProfile(DateTime begTime, DateTime endTime, 
             List<Guid> companyIds, List<Guid> applicationUserIds, List<Guid> workerTypeIds);

         Task<Dialogue> GetDialogueIncludedFramesByIdAsync(Guid dialogueId);

         Task<List<DialogueInfoWithFrames>> GetDialoguesInfoWithFramesAsync(
            DateTime begTime, DateTime endTime,
            List<Guid> companyIds, List<Guid> applicationUserIds, List<Guid> workerTypeIds
            );

         Task<List<Guid?>> GetPersondIdsAsync(DateTime begTime, DateTime endTime, List<Guid> companyIds);

         Task<Guid> GetCrossPhraseTypeIdAsync();
         List<ComponentsDialogueInfo> GetComponentsDialogueInfo(
            DateTime begTime, DateTime endTime,
            List<Guid> companyIds, List<Guid> applicationUserIds, List<Guid> workerTypeIds, Guid loyaltyTypeId);
         List<PhraseType> GetPhraseTypes();
         List<ComponentsPhraseInfo> GetComponentsPhraseInfo();

         List<RatingDialogueInfo> GetRatingDialogueInfos(
            DateTime begTime, DateTime endTime,
            List<Guid> companyIds, List<Guid> applicationUserIds, List<Guid> workerTypeIds, Guid typeIdLoyalty);

         List<DialogueInfo> GetDialogueInfos(
            DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> applicationUserIds, List<Guid> workerTypeIds);
    }
}
