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
    public interface IAnalyticContentProvider
    {

        Task<List<SlideShowInfo>> GetSlideShowsForOneDialogueAsync(Dialogue dialogue);


        Task<List<SlideShowInfo>> GetSlideShowFilteredByPoolAsync(DateTime begTime, DateTime endTime,
          List<Guid> companyIds, List<Guid> applicationUserIds, List<Guid> workerTypeIds, bool isPool);

        Task<List<CampaignContentAnswer>> GetAnswersInOneDialogueAsync(List<SlideShowInfo> slideShowInfos, DateTime begTime, DateTime endTime, Guid applicationUserId);

        List<AnswerInfo.AnswerOne> GetAnswersForOneContent(List<AnswerInfo.AnswerOne> answers, Guid? contentId);

        double GetConversion(double viewsAmount, double answersAmount);

        Task<List<AnswerInfo.AnswerOne>> GetAnswersFullAsync(List<SlideShowInfo> slideShowSessionsAll,
            DateTime begTime, DateTime endTime,
            List<Guid> companyIds, List<Guid> applicationUserIds, List<Guid> workerTypeIds);

        List<SlideShowInfo> AddDialogueIdToShow(List<SlideShowInfo> slideShowSessionsAll, List<DialogueInfoWithFrames> dialogues);

        //------------------FOR CONTENT ANALYTIC------------------------
        EmotionAttention EmotionsDuringAdv(List<SlideShowInfo> shows);

        EmotionAttention EmotionDuringAdvOneDialogue(List<SlideShowInfo> shows, List<DialogueFrame> frames);
    }
}