using System;

namespace UserOperations.Models.Get.AnalyticOfficeController
{
    public class SessionInfo
    {
        // public Guid? IndustryId;//---!!!for benchmarks only
        // public Guid? CompanyId;//---!!!for benchmarks only
        public Guid ApplicationUserId;//
        public DateTime BegTime;//
        public DateTime EndTime;//

        // public string FullName;
    }
}