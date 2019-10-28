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


        public async Task<List<SlideShowInfo>> GetSlideShowsForOneDialogueAsync( Dialogue dialogue )
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
                                     EmotionAttention = EmotionAttentionCalculate(p.BegTime, p.EndTime, dialogue.DialogueFrame.ToList())
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

        public EmotionAttention EmotionsDuringAdv2(List<SlideShowInfo> shows, List<DialogueInfoWithFrames> dialogues)
        {
            List<EmotionAttention> emotionAttentionList = new List<EmotionAttention>();
            if (dialogues != null)
            {
                foreach (var show in shows)
                {
                    var dialogue = dialogues.Where(x => x.DialogueId == show.DialogueId).FirstOrDefault();
                    var emotionAttention = EmotionAttentionCalculate(show.BegTime, show.EndTime, dialogue.DialogueFrame);
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

        public EmotionAttention EmotionsDuringAdv(List<SlideShowInfo> shows, List<DialogueInfoWithFrames> dialogues)
        {
            var frames = dialogues != null ? dialogues.SelectMany(x => x.DialogueFrame).ToList() : null;
            return EmotionDuringAdvOneDialogue(shows, frames);
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

        private EmotionAttention EmotionAttentionCalculate(DateTime begTime, DateTime endTime, List<DialogueFrame> frames)
        {
            //---time - advertaisment begin and end
            frames = frames.Where(x => x.Time >= begTime && x.Time <= endTime).ToList();
                if (frames?.Count() != 0)
                {
                    return new EmotionAttention
                    {
                        Attention = frames.Average(x => Math.Abs((decimal)x.YawShare) <= 20 ? 100 : 20),
                        Positive = frames.Average(x => x.SurpriseShare) + frames.Average(x => x.HappinessShare),
                        Negative = frames.Average(x => x.DisgustShare) + frames.Average(x => x.FearShare) + frames.Average(x => x.SadnessShare) + frames.Average(x => x.ContemptShare),
                        Neutral = frames.Average(x => x.NeutralShare)
                    };
                }
            return null;
        }
    }
}
