using System;
using System.Collections.Generic;
// using UserOperations.Models.AnalyticModels;

namespace QuartzExtensions.Jobs
{     
    public class LanguageDataReport
    {
        public string ReportName {get; set;}
        public string IntroductionGreeting {get; set;}
        public string IntroductionDescription {get; set;}
        public Report Report { get; set; }
        public Indicators Indicators {get; set;}        
    }
    public class Report
    {
        public ReportIndicators Indicators {get; set;}
        public string Dynamics{get; set;}
        public string Value{get; set;}
        public string OfficeRating{get; set;}
        public string CorporationRating{get; set;}
    }
    public class ReportIndicators
    {
        public string Title{get; set;}
		public string Satisfaction {get; set;}
        public string PositiveEmotions {get; set;}
        public string PositiveIntonation {get; set;}
        public string SpeechEmotivity {get; set;}
		
    }
    public class Indicators
    {
        public string AlertPhrase {get; set;}
        public string AvgDialogueTime {get; set;}
        public string CrossPhrase {get; set;}
        public string FillersPhrase {get; set;}
        public string LoyaltyPhrase {get; set;}
        public string NecessaryPhrase {get; set;}
        public string NumberOfDialogues {get; set;}
        public string PositiveEmotions {get; set;}
        public string PositiveIntonations {get; set;}
        public string Satisfaction {get; set;}
        public string SpeechEmotivity {get; set;}
        public string WorkingHours_SessionsTotal {get; set;}
        public string Workload {get; set;}
    }
}