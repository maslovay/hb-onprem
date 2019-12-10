using System;

namespace UserOperations.Models.Get.AnalyticSpeechController
{
    public class DialogueInfo
    {
        // public Guid? IndustryId;//---!!!for benchmarks only
        // public Guid? CompanyId;//---!!!for benchmarks only
        public Guid DialogueId;
        public Guid ApplicationUserId;
        public DateTime BegTime;
        public DateTime EndTime;
        public string FullName;
        public int CrossCount;
        public int AlertCount;
        public double? SatisfactionScore;
        // public double? SatisfactionScoreBeg;
        // public double? SatisfactionScoreEnd;
        // public DateTime SessionBegTime;
        // public DateTime SessionEndTime;
        // public string WorkerType;
    }
}