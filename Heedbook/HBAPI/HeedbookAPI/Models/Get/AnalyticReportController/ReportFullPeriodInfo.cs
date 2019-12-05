using System;
using System.Collections.Generic;

namespace UserOperations.Models.Get.AnalyticReportController
{
    public class ReportFullPeriodInfo
    {
        public string FullName;
        public Guid ApplicationUserId;
        public string WorkerType;
        public DateTime Date;
        public double? SessionTime;
        public double? DialoguesTime;
        public double? Load;
        public List<ReportFullDayInfo> PeriodInfo;
    }
}