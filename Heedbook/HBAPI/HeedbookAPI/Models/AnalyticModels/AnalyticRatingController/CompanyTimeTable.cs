using System;
using System.Collections.Generic;

namespace UserOperations.Models.Get.AnalyticRatingController
{
    public class CompanyTimeTable
    {
        public Guid? CompanyId;
        public List<double> TimeTable;
    }
}