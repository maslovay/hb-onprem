using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UserOperations.Models.AnalyticModels
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

    public class NewDashboardInfo
    {
        public int? EmployeeCount;
        //public int? EmployeeTabletActiveCount;
        //public int? EmployeeOnlineCount;
        //public int? EmployeeServingClientCount;

        public int? ClientsCount;
        public int? ClientsCountDelta;

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

        public int? AdvCount;
        public int? AdvCountDelta;

        public int? AnswerCount;
        public int? AnswerCountDelta;

        public List<string> BestEmployee;
    }

    //-----------BENCHMARKS------------
    public class BenchmarkModel
    {
        public string Name { get; set; }
        public double Value { get; set; }
    }
}
