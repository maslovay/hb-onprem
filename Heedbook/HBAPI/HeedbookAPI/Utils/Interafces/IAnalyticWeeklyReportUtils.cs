using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HBData.Models;

namespace UserOperations.Utils.Interfaces
{
    public interface IAnalyticWeeklyReportUtils
    {
        Dictionary<DateTime, double> AvgDialogueTimePerDay(List<VWeeklyUserReport> dialogues, DateTime reportBegTime);
        double AvgDialogueTimeTotal(List<VWeeklyUserReport> dialogues);
        Dictionary<DateTime, double> AvgNumberOfDialoguesPerDay(List<VWeeklyUserReport> dialogues, DateTime reportBegTime);
        Dictionary<DateTime, double> AvgPerDay(List<VWeeklyUserReport> dialogues, string property, DateTime reportBegTime);
        Dictionary<DateTime, double> AvgWorkingHoursPerDay(List<VSessionUserWeeklyReport> sessions, DateTime reportBegTime);
        Dictionary<DateTime, double> AvgWorkloadPerDay(List<VWeeklyUserReport> dialogues, List<VSessionUserWeeklyReport> sessions, DateTime reportBegTime);
        int? OfficeRating(List<VWeeklyUserReport> dialogues, Guid userId, string property);
        int? OfficeRatingDialoguesAmount(List<VWeeklyUserReport> dialogues, Guid userId);
        int? OfficeRatingDialogueTime(List<VWeeklyUserReport> dialogues, Guid userId);
        int? OfficeRatingPositiveEmotPlace(List<VWeeklyUserReport> dialogues, Guid userId);
        int? OfficeRatingPositiveIntonationPlace(List<VWeeklyUserReport> dialogues, Guid userId);
        int? OfficeRatingSatisfactionPlace(List<VWeeklyUserReport> dialogues, Guid userId);
        int? OfficeRatingSpeechEmotPlace(List<VWeeklyUserReport> dialogues, Guid userId);
        int? OfficeRatingWorkingHours(List<VSessionUserWeeklyReport> sessions, Guid userId);
        int? OfficeRatingWorkload(List<VWeeklyUserReport> dialogues, List<VSessionUserWeeklyReport> sessions, Guid userId);
        Dictionary<DateTime, double> PhraseAvgPerDay(List<VWeeklyUserReport> dialogues, string property, DateTime reportBegTime);
        double? PhraseTotalAvg(List<VWeeklyUserReport> dialogues, string property);
        double? TotalAvg(List<VWeeklyUserReport> dialogues, string property);
        double WorkloadTotal(List<VWeeklyUserReport> dialogues, List<VSessionUserWeeklyReport> sessions);
    }
}