using HBData;
using HBData.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserOperations.Models.AnalyticModels;
using UserOperations.Utils;

namespace UserOperations.Providers
{
    public class AnalyticContentProvider
    {
        private readonly RecordsContext _context;
        public AnalyticContentProvider(RecordsContext context)
        {
            _context = context;
        }
        public async Task<Dialogue> GetDialogueIncludedFramesByIdAsync( Guid dialogueId )
        {
            var dialogue = await _context.Dialogues
                      .Include(p => p.DialogueFrame)
                      .Where(p => p.DialogueId == dialogueId).FirstOrDefaultAsync();
            return dialogue;
        }

        public async Task<List<DialogueInfoWithFrames>> GetDialoguesWithFramesAsync(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds
            )
        {
            var dialogues = await _context.Dialogues
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
                   .ToListAsync();
            return dialogues;
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

        public async Task<List<CampaignContentAnswer>> GetAnswersInDialoguesAsync(List<SlideShowInfo> slideShowInfos, DateTime begTime, DateTime endTime, List<Guid> applicationUserIds)
        {
            var answers = await _context.CampaignContentAnswers
                        .Where(p => slideShowInfos
                        .Select(x => x.CampaignContent.CampaignContentId)
                        .Distinct()
                        .Contains(p.CampaignContentId) 
                            && p.Time >= begTime 
                            && p.Time <= endTime
                            && (!applicationUserIds.Any() || applicationUserIds.Contains(p.ApplicationUserId))).ToListAsync();
            return answers;
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
    }
}
