using System;
using System.Collections.Generic;

namespace UserOperations.Models.Get.AnalyticReportController
{
    public class ReportPartPeriodEmployeeInfo
    {
        public string FullName;
        public Guid ApplicationUserId;
        // public string WorkerType;
        public double? LoadIndexAverage;
        public List<ReportPartDayEmployeeInfo> PeriodInfo;
    }
}