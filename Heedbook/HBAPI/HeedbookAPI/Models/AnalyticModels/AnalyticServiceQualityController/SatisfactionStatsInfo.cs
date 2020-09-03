using System.Collections.Generic;

namespace UserOperations.Models.Get.AnalyticServiceQualityController
{
    public class SatisfactionStatsInfo
    {
        public double? AverageSatisfactionScore;
        public List<SatisfactionStatsDayInfo> PeriodSatisfaction;            
    }
}