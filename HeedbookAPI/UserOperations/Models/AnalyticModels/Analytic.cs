using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace UserOperations.Models.AnalyticModels
{

    public class SessionInfo
    {
        public Guid ApplicationUserId;
        public DateTime BegTime;
        public DateTime EndTime;

        public string FullName;
    }

    public class DialogueInfo
    {
        public Guid DialogueId;
        public Guid ApplicationUserId;
        public DateTime BegTime;
        public DateTime EndTime;
        public string FullName;
        public int CrossCout;
        public double? SatisfactionScore;
        public double? SatisfactionScoreBeg;
        public double? SatisfactionScoreEnd;
        public DateTime SessionBegTime;
        public DateTime SessionEndTime; 
    }

    class DashboardInfo
    {
        public double? EfficiencyIndex;
        public double? EfficiencyIndexDelta;
        public double? EfficiencyIndexPeak;
        public double? SatisfactionIndex;
        public double? SatisfactionIndexDelta;
        public double? SatisfactionIndexDeltaBranch;
        public double? SatisfactionIndexBranch;
        public double? LoadIndex;
        public double? LoadIndexDelta;
        public double? LoadIndexDeltaBranch;
        public double? LoadIndexBranch;
        public double? CrossIndex;
        public double? CrossIndexDelta;
        public double? CrossIndexDeltaBranch;
        public double? CrossIndexBranch;
        public int? EmployeeCount;
        public int? EmployeeCountDelta;
        public int? DialoguesCount;
        public int? DialoguesCountDelta;
        public string BestEmployee;
        public double? BestEmployeeEfficiency;
        public string BestProgressiveEmployee;
        public double? BestProgressiveEmployeeDelta;
        public double? SatisfactionDialogueDelta;
    }

    public class TopHintInfo
    {
        public bool IsPositive;
        public List<string> Hints;
    }

    class EfficiencyDashboardInfo
    {
        public double? LoadIndex;
        public double? LoadIndexDelta;
        public int? DialoguesCount;
        public double? EmployeeCount;
        public double? DialoguesPerEmployee;
        public int? EmployeeOptimalCount;
        public double? WorkingHours;
        public double? WorkingHoursDelta;
        public double? DialogueAveragePause;
        public double? DialogueAverageDuration;
    }

    public class EfficiencyRatingInfo
    {
        public string FullName;
        public double? LoadIndex;
        public int? DialoguesCount;
        public int? WorkingDaysCount;
        public double? WorkingHoursDaily;
        public double? DialogueAverageDuration;
        public double? DialogueAveragePause;
        public double? ClientsWorkingHoursDaily;
    }

    public class EfficiencyOptimizationHourInfo
    {
        public double Load;
        public int UsersCount;
    }

    public class EfficiencyOptimizationDayInfo
    {
        public List<EfficiencyOptimizationHourInfo> DayLoads;
        public DateTime Date;
    }

    public class EfficiencyOptimizationEmployeeInfo
    {
        public TimeSpan Time;
        public int OptimalEmployeeCount;
        public double? RealEmployeeCount;
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

    public class EfficiencyLoadClientsCountInfo
    {
        public List<EfficiencyLoadClientTimeInfo> ClientTimeInfo;
        public List<EfficiencyLoadClientDayInfo> ClientDayInfo;
        public List<EfficiencyLoadEmployeeTimeInfo> EmployeeTimeInfo;
    }


    public class EfficiencyLoadDialogueTimeSatisfactionInfo
    {
        public double? SatisfactionScore;
        public double? Weight;
    }
}