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
        Task<Dialogue> GetDialogueIncludedFramesByIdAsync(Guid dialogueId);

        Task<List<DialogueInfoWithFrames>> GetDialoguesWithFramesAsync(
            DateTime begTime,
            DateTime endTime,
            List<Guid> companyIds,
            List<Guid> applicationUserIds,
            List<Guid> workerTypeIds
            );

        Task<List<SlideShowInfo>> GetSlideShowsForOneDialogueAsync(Dialogue dialogue);


        Task<List<SlideShowInfo>> GetSlideShowFilteredByPoolAsync(
           DateTime begTime,
           DateTime endTime,
           List<Guid> companyIds,
           List<Guid> applicationUserIds,
           List<Guid> workerTypeIds,
           bool isPool
           );
        Task<List<CampaignContentAnswer>> GetAnswersInOneDialogueAsync(List<SlideShowInfo> slideShowInfos, DateTime begTime,
            DateTime endTime, Guid applicationUserId);

        Task<List<CampaignContentAnswer>> GetAnswersInDialoguesAsync(List<SlideShowInfo> slideShowInfos, DateTime begTime,
            DateTime endTime, List<Guid> applicationUserIds);


        //------------------FOR CONTENT ANALYTIC------------------------

        EmotionAttention EmotionsDuringAdv(List<SlideShowInfo> shows, List<DialogueInfoWithFrames> dialogues);

        EmotionAttention EmotionDuringAdvOneDialogue(List<SlideShowInfo> shows, List<DialogueFrame> frames);
    }
}
