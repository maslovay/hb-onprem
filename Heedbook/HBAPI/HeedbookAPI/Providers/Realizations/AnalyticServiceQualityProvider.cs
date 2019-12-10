using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using UserOperations.Models.Get.AnalyticServiceQualityController;

namespace UserOperations.Providers
{
    public class AnalyticServiceQualityProvider : IAnalyticServiceQualityProvider
    {
        private readonly IGenericRepository _repository;
        public AnalyticServiceQualityProvider(
            IGenericRepository repository
        )
        {
            _repository = repository;
        }

        public async Task<List<ComponentsPhraseInfo>> GetComponentsPhraseInfo()
        {
            return await _repository.GetAsQueryable<PhraseType>()
                .Select(p => new ComponentsPhraseInfo {
                    PhraseTypeId = p.PhraseTypeId,
                    PhraseTypeText = p.PhraseTypeText,
                    Colour = p.Colour
                }).ToListAsyncSafe();
        }
        public async Task<List<ComponentsDialogueInfo>> GetComponentsDialogueInfo(
            DateTime begTime, 
            DateTime endTime, 
            List<Guid> companyIds, 
            List<Guid> applicationUserIds, 
            List<Guid> workerTypeIds,
            Guid loyaltyTypeId)
        {
            var dialogues = await _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
                .Select(p => new ComponentsDialogueInfo
                {
                    DialogueId = p.DialogueId,
                    PositiveTone = p.DialogueAudio.Average(q => q.PositiveTone),
                    NegativeTone = p.DialogueAudio.Average(q => q.NegativeTone),
                    NeutralityTone = p.DialogueAudio.Average(q => q.NeutralityTone),

                    EmotivityShare = p.DialogueSpeech.Average(q => q.PositiveShare),

                    HappinessShare = p.DialogueVisual.Average(q => q.HappinessShare),
                    NeutralShare = p.DialogueVisual.Average(q => q.NeutralShare),
                    SurpriseShare = p.DialogueVisual.Average(q => q.SurpriseShare),
                    SadnessShare = p.DialogueVisual.Average(q => q.SadnessShare),
                    AngerShare = p.DialogueVisual.Average(q => q.AngerShare),
                    DisgustShare = p.DialogueVisual.Average(q => q.DisgustShare),
                    ContemptShare = p.DialogueVisual.Average(q => q.ContemptShare),
                    FearShare = p.DialogueVisual.Average(q => q.FearShare),

                    AttentionShare = p.DialogueVisual.Average(q => q.AttentionShare),
                    Loyalty = p.DialoguePhraseCount.Where(q => q.PhraseTypeId == loyaltyTypeId).Sum(q => q.PhraseCount),
                })
                .ToListAsyncSafe();
            return dialogues;
        }
        public IQueryable<Dialogue> GetDialoguesIncludedPhrase(
            DateTime begTime, 
            DateTime endTime, 
            List<Guid> companyIds, 
            List<Guid> workerTypeIds, 
            List<Guid> applicationUserIds = null)
        {
            var dialogues = _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
                    && (applicationUserIds == null || (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))))
                .AsQueryable();
            return dialogues;
        }
        public IQueryable<PhraseType> GetPhraseTypes()
        {
            return _repository.GetAsQueryable<PhraseType>().AsQueryable();
        }
        // public async Task<List<RatingDialogueInfo>> GetRatingDialogueInfos(
            public IQueryable<RatingDialogueInfo> GetRatingDialogueInfos(
            DateTime begTime, 
            DateTime endTime, 
            List<Guid> companyIds, 
            List<Guid> applicationUserIds, 
            List<Guid> workerTypeIds,
            Guid typeIdLoyalty)
        {
            return _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
                .Select(p => new RatingDialogueInfo
                {
                    DialogueId = p.DialogueId,
                    ApplicationUserId = p.ApplicationUserId.ToString(),
                    FullName = p.ApplicationUser.FullName,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    //CrossCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdCross).Count(),
                    //AlertCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdAlert).Count(),
                    //NecessaryCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdNecessary).Count(),
                    LoyaltyCount = p.DialoguePhrase.Where(q => q.PhraseTypeId == typeIdLoyalty).Count(),
                    SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal,
                    PositiveTone = p.DialogueAudio.FirstOrDefault().PositiveTone,
                    AttentionShare = p.DialogueVisual.Average(q => q.AttentionShare),
                    PositiveEmotion = p.DialogueVisual.FirstOrDefault().SurpriseShare + p.DialogueVisual.FirstOrDefault().HappinessShare,
                    TextShare = p.DialogueSpeech.FirstOrDefault().PositiveShare,
                })
                .AsQueryable();
                // .ToListAsyncSafe(); 
        }
        public async Task<List<DialogueInfo>> GetDialogueInfos(
            DateTime begTime, 
            DateTime endTime, 
            List<Guid> companyIds, 
            List<Guid> applicationUserIds, 
            List<Guid> workerTypeIds)
        {
            return await _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
                .Select(p => new DialogueInfo
                {
                    DialogueId = p.DialogueId,
                    ApplicationUserId = p.ApplicationUserId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal
                })
                .ToListAsyncSafe();
        }        
    }
}