using System;

namespace UserOperations.Models.Get.AnalyticRatingController
{
    public class RatingProgressUserInfo
    {
        public DateTime? Date;
        public int DialogueCount;
        public double? TotalScore;
        public double? Load;
        public double? LoadHours;
        public double? WorkingHours;
        public double? DialogueDuration;
        public double? CrossInProcents;
    }
}