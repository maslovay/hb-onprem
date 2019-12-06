using System;

namespace UserOperations.Models.Get.AnalyticRatingController
{
    public class DialogueInfo
    {
        // public Guid? IndustryId;//---!!!for benchmarks only
        public Guid? CompanyId;//---!!!for benchmarks only
        public Guid DialogueId;
        public Guid ApplicationUserId;
        public DateTime BegTime;
        public DateTime EndTime;
        public string FullName;
        public int CrossCount;
        public double? SatisfactionScore;
        // public double? SatisfactionScoreBeg;
        // public double? SatisfactionScoreEnd;
    }
}