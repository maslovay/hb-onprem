using System;
using System.Collections.Generic;

namespace QuartzExtensions.Utils.WeeklyReport
{           
    public class ReportData
    {
        public double ColourData {get; set;}
        public string Name {get; set;}
        public string Description {get; set;}
        public baseClass Data {get; set;}
        public string Base64Image {get; set;}
        public Boolean Thumbnail {get; set;}
        public Boolean NotPercentage {get;set;}
        public Boolean Integer {get;set;}
        public int ReportStyle {get;set;} = 0;
    }
}
