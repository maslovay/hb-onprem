using System;
using System.Collections.Generic;
using HBData.Models;

namespace UserOperations.Models.AnalyticModels
{

    public class SessionInfo
    {
        public Guid? IndustryId;//---!!!for benchmarks only
        public Guid? CompanyId;//---!!!for benchmarks only
        public Guid ApplicationUserId;
        public DateTime BegTime;
        public DateTime EndTime;

        public string FullName;
    }

    public class DialogueInfo
    {
        public Guid? IndustryId;//---!!!for benchmarks only
        public Guid? CompanyId;//---!!!for benchmarks only
        public Guid DialogueId;
        public Guid ApplicationUserId;
        public DateTime BegTime;
        public DateTime EndTime;
        public string FullName;
        public int CrossCount;
        public int AlertCount;
        public double? SatisfactionScore;
        public double? SatisfactionScoreBeg;
        public double? SatisfactionScoreEnd;
        public DateTime SessionBegTime;
        public DateTime SessionEndTime;
        public string WorkerType;
    }

    public class DialogueInfoWithFrames
    {
        public Guid DialogueId;
        public Guid ApplicationUserId;
        public DateTime BegTime;
        public DateTime EndTime;
        public DateTime SessionBegTime;
        public DateTime SessionEndTime;
        public List<DialogueFrame> DialogueFrame;
        public double? Age;
        public string Gender;
    }

    public class EmotionAttention
    {
        public double? Positive { get; set; }
        public double? Negative { get; set; }
        public double? Neutral { get; set; }
        public double? Attention { get; set; }
    }

    public class SessionInfoCompany
    {
        public Guid CompanyId;
        public DateTime BegTime;
        public DateTime EndTime;

        public string FullName;
    }

    public class DialogueInfoCompany
    {
        public Guid DialogueId;
        public Guid CompanyId;
        public DateTime BegTime;
        public DateTime EndTime;
        public string FullName;
        public int CrossCount;
        public double? SatisfactionScore;
        public double? SatisfactionScoreBeg;
        public double? SatisfactionScoreEnd;
        public DateTime SessionBegTime;
        public DateTime SessionEndTime;
        public string WorkerType;
    }


    //public class TopHintInfo
    //{
    //    public bool IsPositive;
    //    public List<string> Hints;
    //}

    public class EfficiencyOptimizationHourInfo
    {
        public double Load;
        public int UsersCount;
    }

    public class EfficiencyLoadClientTimeInfo
    {
        public string Time;
        public double? ClientCount;
    }

    public class EfficiencyLoadClientDayInfo
    {
        public string Day;
        public double? ClientCount;
    }

    public class EfficiencyLoadEmployeeTimeInfo
    {
        public string Time;
        public double? SatisfactionIndex;
        public double? LoadIndex;
    }

    public class EfficiencyLoadDialogueTimeSatisfactionInfo
    {
        public double? SatisfactionScore;
        public double? Weight;
    }

    public class ReportFullDayInfo
    {
        public int ActivityType;
        public DateTime Beg;
        public DateTime End;
        public Guid? DialogueId;
    }

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

    public class SlideShowInfo
    {
        public DateTime BegTime { get; set; }
        public Guid? ContentId { get; set; }
        public Guid? CampaignContentId { get; set; }
        public CampaignContent CampaignContent { get; set; }
        public string ContentType { get; set; }
        public string ContentName { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsPoll { get; set; }
        public string Url { get; set; }
        public Guid ApplicationUserId { get; set; }
        public Guid? DialogueId { get; set; }
        public double? Age { get; set; }
        public string Gender { get; set; }
        public EmotionAttention EmotionAttention { get; set; }
    }


    //--------------AnalyticLoadOfficeController-------------------------
    public class Employee
    {
        public string BestEmployeeName;
        public Guid BestEmployeeId;
        public double? LoadValue;
        public DateTime? Date;
    }

    class EfficiencyDashboardInfoNew
    {
        public double? WorkloadValueAvg;
        public double? WorkloadDynamics;
        public int? DialoguesCount;
        public double? DialoguesNumberAvgPerEmployee;
        public double? AvgWorkingTime;
        public double? AvgDurationDialogue;
        public double? DialoguesNumberAvgPerDayOffice;
        public Employee BestEmployee;
    }

    //---------AnalyticWeeklyReport------------
    public class UserWeeklyInfo
    {
        public UserWeeklyInfo(int corp, int comp)
        {
            AmountUsersInCompany = comp;
            AmountUsersInCorporation = corp;
        }
        public double? TotalAvg;
        public double? Dynamic;
        public Dictionary<DateTime, double> AvgPerDay;
        public int? OfficeRating;
        public int? OfficeRatingChanges;
        public int? CorporationRating;
        public int? CorporationRatingChanges;
        public int AmountUsersInCorporation { get; set; }
        public int AmountUsersInCompany { get; set; }

    }   
}