using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UserOperations.Models.Get.HomeController
{
    public class DashboardInfo
    {
        public double? SatisfactionIndex;
        public double? SatisfactionIndexDelta;
        public double? SatisfactionIndexIndustryAverage;
        public double? SatisfactionIndexIndustryBenchmark;

        public double? LoadIndex;
        public double? LoadIndexDelta;
        public double? LoadIndexIndustryAverage;
        public double? LoadIndexIndustryBenchmark;

        public double? CrossIndex;
        public double? CrossIndexDelta;
        public double? CrossIndexIndustryAverage;
        public double? CrossIndexIndustryBenchmark;

        public int? EmployeeCount;
        public int? EmployeeCountDelta;
        public int? DialoguesCount;
        public int? DialoguesCountDelta;
        public int? NumberOfDialoguesPerEmployees;
        public int? NumberOfDialoguesPerEmployeesDelta;
        public double? AvgWorkingTimeEmployees;
        public double? AvgWorkingTimeEmployeesDelta;
        public string BestEmployee;
        public double? BestEmployeeEfficiency;
        public string BestProgressiveEmployee;
        public double? BestProgressiveEmployeeDelta;
        public double? SatisfactionDialogueDelta;

        public double? DialogueDuration;
        public double? DialogueDurationDelta;
    }
}