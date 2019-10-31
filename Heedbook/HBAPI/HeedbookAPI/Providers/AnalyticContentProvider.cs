using HBData;
using HBData.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Providers
{
    public class AnalyticContentProvider
    {
        private readonly RecordsContext _context;
        public AnalyticContentProvider(RecordsContext context)
        {
            _context = context;
        } 


        public async Task<List<SlideShowInfo>> GetSlideShowFilteredByUserAsync(
          Dialogue dialogue
           )
        {
            var slideShows = await _context.SlideShowSessions
                    .Include(p => p.CampaignContent)
                    .ThenInclude(p => p.Content)
                    .Where(p => p.BegTime >= dialogue.BegTime
                             && p.BegTime <= dialogue.EndTime
                             && p.ApplicationUserId == dialogue.ApplicationUserId)
                             .Select(p =>
                                 new SlideShowInfo
                                 {
                                     BegTime = p.BegTime,
                                     ContentId = p.CampaignContent != null ? p.CampaignContent.ContentId : null,
                                     CampaignContentId = p.CampaignContentId,
                                     ContentType = p.ContentType,
                                     ContentName = p.CampaignContent.Content.Name,
                                     EndTime = p.EndTime,
                                     IsPoll = p.IsPoll,
                                     Url = p.Url,
                                     ApplicationUserId = (Guid)p.ApplicationUserId,
                                     EmotionAttention = SatisfactionDuringAdv(p, dialogue)
                                 }
                             )
                            .ToListAsync();
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
           var slideShows =  await _context.SlideShowSessions.Where(p => p.IsPoll == isPool)
                          .Include(p => p.ApplicationUser)
                          .Include(p => p.CampaignContent)
                          .ThenInclude(p => p.Content)
                          .Include(p => p.CampaignContent)
                          .ThenInclude(p => p.Campaign)
                          .Where(p => p.BegTime >= begTime
                                   && p.BegTime <= endTime
                                   && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                                   && (!applicationUserIds.Any() || applicationUserIds.Contains((Guid)p.ApplicationUserId))
                                   && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
                                   && p.CampaignContent != null)
                                   .Select(p =>
                                       new SlideShowInfo
                                       {
                                           BegTime = p.BegTime,
                                           ContentId = p.CampaignContent != null ? p.CampaignContent.ContentId : null,
                                           CampaignContent = p.CampaignContent,
                                           ContentType = p.ContentType,
                                           ContentName = p.CampaignContent != null ? p.CampaignContent.Content.Name : null,
                                           EndTime = p.EndTime,
                                           IsPoll = p.IsPoll,
                                           Url = p.Url,
                                           ApplicationUserId = (Guid)p.ApplicationUserId
                                       }
                                   )
                                  .ToListAsync();
            return slideShows;
        }

        public async Task<List<CampaignContentAnswer>> GetAnswersInOneDialogueAsync(List<SlideShowInfo> slideShowInfos, DateTime begTime, DateTime endTime, Guid applicationUserId)
        {
            var answers = await _context.CampaignContentAnswers
                      .Where(p => slideShowInfos
                      .Select(x => x.CampaignContentId)
                      .Distinct()
                      .Contains(p.CampaignContentId)
                          && p.Time >= begTime
                          && p.Time <= endTime
                          && p.ApplicationUserId == applicationUserId).ToListAsync();
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

        public EmotionAttention SatisfactionDuringAdv(List<SlideShowInfo> sessions, List<DialogueInfoWithFrames> dialogues)
        {
            EmotionAttention result = new EmotionAttention();
            if (dialogues != null)
            {
                foreach (var session in sessions)
                {
                    List<DialogueFrame> frames = dialogues.Where(x => x.DialogueId == session.DialogueId).FirstOrDefault()?.DialogueFrame.ToList();
                    var beg = session.BegTime;
                    var end = session.EndTime;
                    frames = frames != null ? frames.Where(x => x.Time >= beg && x.Time <= end).ToList() : null;
                    if (frames != null && frames.Count() != 0)
                    {
                        result.Attention = frames.Average(x => Math.Abs((decimal)x.YawShare) <= 20 ? 100 : 20);
                        result.Positive = frames.Average(x => x.SurpriseShare) + frames.Average(x => x.HappinessShare);
                        result.Negative = frames.Average(x => x.DisgustShare) + frames.Average(x => x.FearShare) + frames.Average(x => x.SadnessShare) + frames.Average(x => x.ContemptShare);
                        result.Neutral = frames.Average(x => x.NeutralShare);
                        return result;
                    }
                }
            }
            return null;
        }

        public EmotionAttention SatisfactionDuringAdv(List<SlideShowInfo> sessions, Dialogue dialogue)
        {
            EmotionAttention result = new EmotionAttention();
            if (dialogue != null)
            {
                foreach (var session in sessions)
                {
                    List<DialogueFrame> frames = dialogue.DialogueFrame.ToList();
                    var beg = session.BegTime;
                    var end = session.EndTime;
                    frames = frames != null ? frames.Where(x => x.Time >= beg && x.Time <= end).ToList() : null;
                    if (frames != null && frames.Count() != 0)
                    {
                        result.Attention = frames.Average(x => Math.Abs((decimal)x.YawShare) <= 20 ? 100 : 20);
                        result.Positive = frames.Average(x => x.SurpriseShare) + frames.Average(x => x.HappinessShare);
                        result.Negative = frames.Average(x => x.DisgustShare) + frames.Average(x => x.FearShare) + frames.Average(x => x.SadnessShare) + frames.Average(x => x.ContemptShare);
                        result.Neutral = frames.Average(x => x.NeutralShare);
                        return result;
                    }
                }
            }
            return null;
        }      

        public EmotionAttention SatisfactionDuringAdv(SlideShowSession session, Dialogue dialogue)
        {
            EmotionAttention result = new EmotionAttention();
            if (dialogue != null)
            {
                List<DialogueFrame> frames = dialogue.DialogueFrame.ToList();
                var beg = session.BegTime;
                var end = session.EndTime;
                frames = frames != null ? frames.Where(x => x.Time >= beg && x.Time <= end).ToList() : null;
                if (frames != null && frames.Count() != 0)
                {
                    result.Attention = frames.Average(x => Math.Abs((decimal)x.YawShare) <= 20 ? 100 : 20);
                    result.Positive = frames.Average(x => x.SurpriseShare) + frames.Average(x => x.HappinessShare);
                    result.Negative = frames.Average(x => x.DisgustShare) + frames.Average(x => x.FearShare) + frames.Average(x => x.SadnessShare) + frames.Average(x => x.ContemptShare);
                    result.Neutral = frames.Average(x => x.NeutralShare);
                    return result;
                }
            }
            return null;
        }

        //---PRIVATE---
        private async Task<List<CampaignContentAnswer>> GetAnswersAsync(DateTime begTime, DateTime endTime, List<Guid> companyIds, List<Guid> applicationUserIds, List<Guid> workerTypeIds)
        {
            //---all answers in period for company/user
            var result = await _context.CampaignContentAnswers
                                  .Where(p =>
                                   p.CampaignContent != null
                                   && (p.Time >= begTime && p.Time <= endTime)
                                   && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))
                                   && (!workerTypeIds.Any() || workerTypeIds.Contains((Guid)p.ApplicationUser.WorkerTypeId))
                                   && (!companyIds.Any() || companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                                    ).ToListAsync();

            return result;
        }
    }
}
