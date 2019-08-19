using System;
using System.Collections.Generic;
using UserOperations.Models.AnalyticModels;

namespace QuartzExtensions.Jobs
{           
    public class ViewLanguageDataReport
    {
        public string ApplicationUserName {get; set;}
        public LanguageDataReport LanguageDataReport {get; set;}
        public List<ReportData> Parameters {get; set;}
        public List<string> Base64Images {get; set;}
    }
}