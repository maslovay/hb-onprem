using System.Collections.Generic;

namespace UserOperations.Models.Get.HomeController
{
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

        public double? SmilesShare;
        public double? SmilesShareDelta;
        public double? WorkloadValueAvgByWorkingTime;
        public double? WorkloadDynamicsWorkingTime;
        public double? WorkLoadByTimeIndustryAverage;
        public double? WorkLoadByTimeIndustryBenchmark;
        //public List<BestEmployee> BestEmployees;
    }
}