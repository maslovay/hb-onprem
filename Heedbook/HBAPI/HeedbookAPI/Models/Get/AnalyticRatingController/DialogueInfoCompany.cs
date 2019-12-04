using System;

namespace UserOperations.Models.Get.AnalyticRatingController
{
    public class DialogueInfoCompany
    {
        public Guid DialogueId;
        public Guid CompanyId;
        public DateTime BegTime;
        public DateTime EndTime;
        public string FullName;
        public int CrossCount;
        public double? SatisfactionScore;
        public double? SatisfactionScoreBeg;
        public double? SatisfactionScoreEnd;
        public DateTime SessionBegTime;
        public DateTime SessionEndTime;
        public string WorkerType;
    }
}