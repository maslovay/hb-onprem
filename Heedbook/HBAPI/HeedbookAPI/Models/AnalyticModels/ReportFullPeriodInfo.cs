using System;
using System.Collections.Generic;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Models.AnalyticModels
{
    public class ReportFullPeriodInfo
    {
        public string FullName;
        public Guid? ApplicationUserId;
        public DateTime Date;
        public double? SessionTime;
        public double? DialoguesTime;
        public double? Load;
        public List<ReportFullDayInfo> PeriodInfo;
    }
}