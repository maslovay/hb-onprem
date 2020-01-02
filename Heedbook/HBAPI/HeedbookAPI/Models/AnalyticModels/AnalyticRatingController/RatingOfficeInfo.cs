namespace UserOperations.Models.Get.AnalyticRatingController
{
    public class RatingOfficeInfo
    {
        public string CompanyId;
        public string FullName;
        public double? EfficiencyIndex;
        public double? SatisfactionIndex;
        public double? LoadIndex;
        public double? CrossIndex;
        public string Recommendation;
        public int DialoguesCount;
        public int DaysCount;
        public double? WorkingHoursDaily;
        public double? DialogueAverageDuration;
        public double? DialogueAveragePause;
        // public double? ClientsWorkingHoursDaily;
    }
}