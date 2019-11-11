using HBData;
using HBData.Models;
using HBData.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Providers
{
    public class AnalyticContentProvider : IAnalyticContentProvider
    {
        private readonly IGenericRepository _repository;
        public AnalyticContentProvider(IGenericRepository repository)
        {
            _repository = repository;
        } 


        public async Task<List<SlideShowInfo>> GetSlideShowsForOneDialogueAsync( Dialogue dialogue )
        {
            var slideShows = await _repository.GetAsQueryable<SlideShowSession>()
                    .Where(p => p.BegTime >= dialogue.BegTime
                             && p.BegTime <= dialogue.EndTime
                             && p.ApplicationUserId == dialogue.ApplicationUserId)
                             .Select(p =>
                                 new SlideShowInfo
                                 {
                                     BegTime = p.BegTime,
                                     ContentId = p.CampaignContent != null ? p.CampaignContent.ContentId : null,
                                     ContentName = p.CampaignContent != null ? p.CampaignContent.Content.Name : null,
                                     CampaignContentId = p.CampaignContentId,
                                     ContentType = p.ContentType,
                                     EndTime = p.EndTime,
                                     IsPoll = p.IsPoll,
                                     Url = p.Url,
                                     ApplicationUserId = (Guid)p.ApplicationUserId,
                                     EmotionAttention = EmotionAttentionCalculate(p.BegTime, p.EndTime, dialogue.DialogueFrame.ToList())
                                 }
                             )
                            .ToListAsyncSafe();
            return slideShows;
        }


        public async Task<List<SlideShowInfo>> GetSlideShowFilteredByPoolAsync(
           DateTime begTime,
           DateTime endTime,
           List<Guid> companyIds,
           List<Guid> applicationUserIds,
           List<Guid> workerTypeIds,
           bool isPool
           )
        {         
           var slideShows =  await _repository.GetAsQueryable<SlideShowSession>().Where(p => p.IsPoll == isPool
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
                                           ApplicationUserId = (Guid)p.ApplicationUserId
                                       }
                                   )
                                  .ToListAsyncSafe();
            return slideShows;
        }

        public async Task<List<CampaignContentAnswer>> GetAnswersInOneDialogueAsync(List<SlideShowInfo> slideShowInfos, DateTime begTime, DateTime endTime, Guid applicationUserId)
        {
            var answers = await _repository.GetAsQueryable<CampaignContentAnswer>()
                      .Where(p => slideShowInfos
                      .Select(x => x.CampaignContentId)
                      .Distinct()
                      .Contains(p.CampaignContentId)
                          && p.Time >= begTime
                          && p.Time <= endTime
                          && p.ApplicationUserId == applicationUserId).ToListAsyncSafe();
            return answers;
        }

        public List<AnswerInfo.AnswerOne> GetAnswersForOneContent(List<AnswerInfo.AnswerOne> answers, Guid? contentId)
        {
            return answers.Where(x => x.ContentId == contentId).ToList();
        }

        public double GetConversion(double viewsAmount, double answersAmount)
        {
            return viewsAmount != 0 ? answersAmount / viewsAmount : 0;
        }    

        public async Task<List<AnswerInfo.AnswerOne>> GetAnswersFullAsync(List<SlideShowInfo> slideShowSessionsAll, DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> applicationUserIds, List<Guid> workerTypeIds)
        {
            var answers = await GetAnswersAsync(begTime, endTime, companyIds, applicationUserIds, workerTypeIds);

            List<AnswerInfo.AnswerOne> answersResult = new List<AnswerInfo.AnswerOne>();
            foreach (var answ in answers)
            {
                var slideShowSessionForAnswer = slideShowSessionsAll.Where(x => x.BegTime <= answ.Time
                                                        && x.EndTime >= answ.Time
                                                        && x.ApplicationUserId == answ.ApplicationUserId)
                                                        .FirstOrDefault();
                var dialogueId = slideShowSessionForAnswer != null ? slideShowSessionForAnswer.DialogueId : null;

                AnswerInfo.AnswerOne oneAnswer = new AnswerInfo.AnswerOne
                {
                    Answer = answ.Answer,
                    Time = answ.Time,
                    DialogueId = dialogueId,
                    ContentId = answ.CampaignContent?.ContentId
                };
                answersResult.Add(oneAnswer);
            }
            return answersResult;
        }

        public List<SlideShowInfo> AddDialogueIdToShow(List<SlideShowInfo> slideShowSessionsAll, List<DialogueInfoWithFrames> dialogues)
        {
            foreach (var session in slideShowSessionsAll)
            {
                var dialog = dialogues.Where(x => x.BegTime <= session.BegTime && x.EndTime >= session.BegTime)
                        .FirstOrDefault();
                session.DialogueId = dialog?.DialogueId;
            }
            slideShowSessionsAll = slideShowSessionsAll.Where(x => x.DialogueId != null && x.DialogueId != default(Guid)).ToList();
            return slideShowSessionsAll;
        }

        
        
        //------------------FOR CONTENT ANALYTIC------------------------
        public EmotionAttention EmotionsDuringAdv(List<SlideShowInfo> shows)
        {
            List<EmotionAttention> emotionAttentionList = new List<EmotionAttention>();         
                foreach (var show in shows)
                {
                    var emotionAttention = EmotionAttentionCalculate(show.BegTime, show.EndTime, show.DialogueFrames);
                    if (emotionAttention != null)
                        emotionAttentionList.Add(emotionAttention);
                }
                return new EmotionAttention
                {
                    Attention = emotionAttentionList.Average(x => x.Attention),
                    Negative = emotionAttentionList.Average(x => x.Negative),
                    Neutral = emotionAttentionList.Average(x => x.Neutral),
                    Positive = emotionAttentionList.Average(x => x.Positive)
                };
        }

        public EmotionAttention EmotionDuringAdvOneDialogue(List<SlideShowInfo> shows, List<DialogueFrame> frames)
        {
            List<EmotionAttention> emotionAttentionList = new List<EmotionAttention>();
            if (frames != null && frames.Count() != 0)
            {
                foreach (var show in shows)
                {
                    var emotionAttention = EmotionAttentionCalculate(show.BegTime, show.EndTime, frames);
                    if (emotionAttention != null)
                        emotionAttentionList.Add(emotionAttention);
                }
                return new EmotionAttention
                {
                    Attention = emotionAttentionList.Average(x => x.Attention),
                    Negative = emotionAttentionList.Average(x => x.Negative),
                    Neutral = emotionAttentionList.Average(x => x.Neutral),
                    Positive = emotionAttentionList.Average(x => x.Positive)
                };
            }
            return null;
        }      

        //---PRIVATE---
        private EmotionAttention EmotionAttentionCalculate(DateTime begTime, DateTime endTime, List<DialogueFrame> frames)
        {
            if (frames == null || frames.Count() == 0) return null;
            //---time - advertaisment begin and end
            ReplaseFramesNullOnZero(frames);
            frames = frames.Where(x => x.Time >= begTime && x.Time <= endTime).ToList();
                if (frames?.Count() != 0)
                {
                    return new EmotionAttention
                    {
                        Attention = frames.Average(x => Math.Abs((decimal)x.YawShare) <= 20 ? 100 : 20),
                        Positive = frames.Average(x => x.SurpriseShare) + frames.Average(x => x.HappinessShare),
                        Negative = frames.Average(x => x.AngerShare) + frames.Average(x => x.DisgustShare) + frames.Average(x => x.FearShare) + frames.Average(x => x.SadnessShare) + frames.Average(x => x.ContemptShare),
                        Neutral = frames.Average(x => x.NeutralShare)
                    };
                }
            return null;
        }

        private async Task<IEnumerable<CampaignContentAnswer>> GetAnswersAsync(DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> applicationUserIds, List<Guid> workerTypeIds)
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

        private void ReplaseFramesNullOnZero(List<DialogueFrame> frames)
        {
            foreach (var item in frames)
            {
                item.YawShare = item.YawShare ?? 0;
                item.AngerShare = item.AngerShare ?? 0;
                item.ContemptShare = item.ContemptShare ?? 0;
                item.DisgustShare = item.DisgustShare ?? 0;
                item.FearShare = item.FearShare ?? 0;
                item.HappinessShare = item.HappinessShare ?? 0;
                item.NeutralShare = item.NeutralShare ?? 0;
                item.SadnessShare = item.SadnessShare ?? 0;
                item.SurpriseShare = item.SurpriseShare ?? 0;
            }
        }
    }
}
