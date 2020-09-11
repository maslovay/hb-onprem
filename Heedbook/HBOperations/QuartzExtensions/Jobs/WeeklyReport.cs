using System;
using System.Collections.Generic;
// using UserOperations.Models.AnalyticModels;

namespace QuartzExtensions.Jobs
{       
    public class baseClass
    {
        public double totalAvg { get; set; }
        public double dynamic { get; set; }
        public Dictionary<DateTime, double> avgPerDay { get; set; }
        public int officeRating { get; set; }
        public int officeRatingChanges { get; set; }
        public int corporationRating {get; set;}
        public int corporationRatingChanges { get; set; }
        public int amountUsersInCorporation { get; set; }
        public int amountUsersInCompany { get; set; }
    }  
    public class WeeklyReport
    {
        public baseClass Satisfaction {get; set;}
        public baseClass PositiveEmotions {get; set;}
        public baseClass PositiveIntonations {get; set;}
        public baseClass SpeechEmotivity {get; set;}
        public baseClass NumberOfDialogues {get; set;}
        public baseClass WorkingHours_SessionsTotal {get; set;}
        public baseClass AvgDialogueTime {get; set;}
        public baseClass Workload {get; set;}
        public baseClass CrossPhrase {get; set;}
        public baseClass AlertPhrase {get; set;}
        public baseClass LoyaltyPhrase {get; set;}
        public baseClass NecessaryPhrase {get; set;}
        public baseClass FillersPhrase {get; set;}
    }   
}