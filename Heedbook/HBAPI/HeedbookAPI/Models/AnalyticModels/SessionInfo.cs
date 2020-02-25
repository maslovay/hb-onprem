using System;

namespace UserOperations.Models.AnalyticModels
{
    public class SessionInfo
    {
        public Guid? IndustryId;//---!!!for benchmarks only
        public Guid? CompanyId;//---!!!for benchmarks only
        public Guid? ApplicationUserId;
        public Guid DeviceId;//
        public DateTime BegTime;//
        public DateTime EndTime;//
        public string FullName;
    }
}