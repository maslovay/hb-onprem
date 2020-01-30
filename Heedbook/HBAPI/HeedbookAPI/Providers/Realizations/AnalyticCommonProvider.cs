using HBData.Models;
using HBData.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.Models.AnalyticModels;
using UserOperations.Models.Get.AnalyticServiceQualityController;

namespace UserOperations.Providers
{
    public class AnalyticCommonProvider
    {
        private readonly IGenericRepository _repository;
        public AnalyticCommonProvider(IGenericRepository repository)
        {
            _repository = repository;
        }
        public async Task<IEnumerable<Models.AnalyticModels.SessionInfoFull>> GetSessionInfoAsync( DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> workerTypeIds, List<Guid> userIds = null)
        {
            var sessions = await _repository.GetAsQueryable<Session>()
                         .Where(p => p.BegTime >= begTime
                                 && p.EndTime <= endTime
                                 && p.StatusId == 7
                                 && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                                 && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
                                 && (userIds == null || (!userIds.Any() || userIds.Contains(p.ApplicationUserId))))
                         .Select(p => new Models.AnalyticModels.SessionInfoFull
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

        public IQueryable<Dialogue> GetDialoguesIncludedClientProfile(DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> applicationUserIds, List<Guid> workerTypeIds)
        {
            var data = _repository.GetAsQueryable<Dialogue>()
                .Include(p => p.DialogueClientProfile)
                .Include(p => p.ApplicationUser)
                .Where(p => p.BegTime >= begTime &&
                    p.EndTime <= endTime &&
                    p.StatusId == 3 &&
                    p.InStatistic == true &&
                    (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId)) &&
                    (!applicationUserIds.Any() || applicationUserIds.Contains((Guid)p.ApplicationUserId)) &&
                    (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))).AsQueryable();
            return data;
        }

        public async Task<Dialogue> GetDialogueIncludedFramesByIdAsync(Guid dialogueId)
        {
            var dialogue = await _repository.GetAsQueryable<Dialogue>()
                .Include(p => p.DialogueFrame)
                .Where(p => p.DialogueId == dialogueId).FirstOrDefaultAsync();
            return dialogue;
        }

        public async Task<List<DialogueInfoWithFrames>> GetDialoguesInfoWithFramesAsync(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds
            )
        {
            var dialogues = await _repository.GetAsQueryable<Dialogue>()
                   .Include(p => p.ApplicationUser)
                   .Include(p => p.DialogueClientSatisfaction)
                   .Include(p => p.DialogueFrame)
                   .Where(p => p.BegTime >= begTime
                           && p.EndTime <= endTime
                           && p.StatusId == 3
                           && p.InStatistic == true
                           && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                           && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                           && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))
                   .Select(p => new DialogueInfoWithFrames
                   {
                       DialogueId = p.DialogueId,
                       ApplicationUserId = p.ApplicationUserId,
                       BegTime = p.BegTime,
                       EndTime = p.EndTime,
                       DialogueFrame = p.DialogueFrame.ToList(),
                       Gender = p.DialogueClientProfile.Max(x => x.Gender),
                       Age = p.DialogueClientProfile.Average(x => x.Age)
                   })
                   .ToListAsyncSafe();
            return dialogues;
        }

        public async Task<List<Guid?>> GetPersondIdsAsync(DateTime begTime, DateTime endTime, List<Guid> companyIds)
        {
            var persondIds = await GetDialogues(begTime, endTime, companyIds)
                    .Where ( p => p.PersonId != null )
                    .Select(p => p.PersonId).Distinct()
                    .ToListAsyncSafe();
            return persondIds;
        }

        public async Task<Guid> GetCrossPhraseTypeIdAsync()
        {
            var typeIdCross = await _repository.GetAsQueryable<PhraseType>()
                    .Where(p => p.PhraseTypeText == "Cross")
                    .Select(p => p.PhraseTypeId)
                    .FirstOrDefaultAsync();
            return typeIdCross;
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
                .Include(p => p.ApplicationUser)
                .Include(p => p.DialoguePhraseCount)
                .Include(p => p.DialogueAudio)
                .Include(p => p.DialogueSpeech)
                .Include(p => p.DialogueVisual)
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
        public async Task<IEnumerable<PhraseType>> GetPhraseTypes()
        {
            return await _repository.FindAllAsync<PhraseType>();
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

        public async Task<List<RatingDialogueInfo>> GetRatingDialogueInfos(
            DateTime begTime, 
            DateTime endTime, 
            List<Guid> companyIds, 
            List<Guid> applicationUserIds, 
            List<Guid> workerTypeIds,
            Guid typeIdLoyalty)
        {
            return await _repository.GetAsQueryable<Dialogue>()
                .Include(p => p.ApplicationUser)
                .Include(p => p.DialogueClientSatisfaction)
                .Include(p => p.DialoguePhrase)
                .Include(p => p.DialogueAudio)
                .Include(p => p.DialogueVisual)
                .Include(p => p.DialogueSpeech)
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
                .ToListAsyncSafe(); 
        }

        public async Task<List<Models.AnalyticModels.DialogueInfoFull>> GetDialogueInfos(
            DateTime begTime, 
            DateTime endTime, 
            List<Guid> companyIds, 
            List<Guid> applicationUserIds, 
            List<Guid> workerTypeIds)
        {
            return await _repository.GetAsQueryable<Dialogue>()
                .Include(p => p.ApplicationUser)
                .Include(p => p.DialogueClientSatisfaction)
                .Where(p => p.BegTime >= begTime
                    && p.EndTime <= endTime
                    && p.StatusId == 3
                    && p.InStatistic == true
                    && (!companyIds.Any() || companyIds.Contains((Guid) p.ApplicationUser.CompanyId))
                    && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                    && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid) p.ApplicationUser.WorkerTypeId)))
                .Select(p => new Models.AnalyticModels.DialogueInfoFull
                {
                    DialogueId = p.DialogueId,
                    ApplicationUserId = p.ApplicationUserId,
                    BegTime = p.BegTime,
                    EndTime = p.EndTime,
                    SatisfactionScore = p.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal
                })
                .ToListAsyncSafe();
        }

        private IQueryable<Dialogue> GetDialogues(DateTime begTime, DateTime endTime, List<Guid> companyIds = null, List<Guid> applicationUserIds = null, List<Guid> workerTypeIds = null)
        {
            var data = _repository.GetAsQueryable<Dialogue>()
                    .Where(p => p.BegTime >= begTime &&
                        p.EndTime <= endTime &&
                        p.StatusId == 3 &&
                        p.InStatistic == true &&
                        (companyIds == null || (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))) &&
                        (applicationUserIds == null || (!applicationUserIds.Any() || applicationUserIds.Contains((Guid)p.ApplicationUserId))) &&
                        (workerTypeIds == null || (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId)))).AsQueryable();
            return data;
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
    }
}
