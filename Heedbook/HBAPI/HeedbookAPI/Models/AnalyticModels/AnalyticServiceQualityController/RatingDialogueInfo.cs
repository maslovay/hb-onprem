using System;

namespace UserOperations.Models.Get.AnalyticServiceQualityController
{
    public class RatingDialogueInfo
    {
        public Guid DialogueId;
        public Guid? ApplicationUserId;
        public Guid DeviceId;
        public DateTime BegTime;
        public DateTime EndTime;
        public string FullName;
        public int CrossCount;
        public int AlertCount;
        public int NecessaryCount;
        public int LoyaltyCount;
        public double? SatisfactionScore;
        public double? PositiveTone;
        public double? AttentionShare;
        public double? PositiveEmotion;
        public double? TextShare;
    }
}