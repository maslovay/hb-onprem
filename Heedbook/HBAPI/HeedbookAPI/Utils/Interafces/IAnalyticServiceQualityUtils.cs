using System;
using System.Collections.Generic;
using System.Linq;
using UserOperations.Models.AnalyticModels;
using UserOperations.Models.Get.AnalyticServiceQualityController;

namespace UserOperations.Utils.Interfaces
{
    public interface IAnalyticServiceQualityUtils
    {
        string BestEmployee(List<DialogueInfo> dialogues);
        double? BestEmployeeSatisfaction(List<DialogueInfo> dialogues);
        string BestProgressiveEmployee(List<DialogueInfo> dialogues, DateTime beg);
        double? BestProgressiveEmployeeDelta(List<DialogueInfo> dialogues, DateTime beg);
        int DialoguesCount(List<DialogueInfo> dialogues, Guid? applicationUserId = null, DateTime? date = null);
        double? LoyaltyIndex(List<ComponentsDialogueInfo> dialogues);
        double? LoyaltyIndex(IGrouping<Guid?, RatingDialogueInfo> dialogues);
        double? SatisfactionIndex(List<DialogueInfo> dialogues);
    }
}