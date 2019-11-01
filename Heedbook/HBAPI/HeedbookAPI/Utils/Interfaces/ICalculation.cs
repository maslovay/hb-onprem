using System;
using System.Collections.Generic;
using System.Linq;
using UserOperations.Models.AnalyticModels;
using UserOperations.Controllers;

namespace UserOperations.Utils
{
    public interface IDBOperations
    {
        T Max<T>(T val1, T val2) where T : IComparable<T>;
        T Min<T>(T val1, T val2) where T : IComparable<T>;

        double? SignedPower(double x, double power);

        double? MaxDouble(double? x, double? y);

        double? DialoguesPerUser(List<DialogueInfo> dialogues);


        double? SatisfactionDialogueDelta(List<DialogueInfo> dialogues);
        double? LoadIndex(double? workinHours, double? dialogueHours);
        double? LoadIndex(List<SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg, DateTime end);

        double? LoadIndex(List<SessionInfo> sessions, List<DialogueInfo> dialogues);

        double? LoadIndex(List<SessionInfo> sessions, IGrouping<Guid, DialogueInfo> dialogues, DateTime beg, DateTime end);

        double? LoadIndex(List<SessionInfoCompany> sessions, IGrouping<Guid, DialogueInfoCompany> dialogues, DateTime beg, DateTime end);

        double? LoadIndex(List<SessionInfo> sessions, IGrouping<DateTime, DialogueInfo> dialogues,
            Guid applicationUserId, DateTime? date, DateTime beg = default(DateTime), DateTime end = default(DateTime));

        double? LoadIndex(IGrouping<Guid, SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg, DateTime end);

        double? LoadIndex(List<SessionInfo> sessions, List<DialogueInfo> dialogues,
            Guid applicationUserId, DateTime date, DateTime beg = default(DateTime), DateTime end = default(DateTime));

        //Satisfaction index calculation
        double? SatisfactionIndex(List<DialogueInfo> dialogues);

        double? SatisfactionIndex(IGrouping<Guid, DialogueInfo> dialogues);

        double? SatisfactionIndex(IGrouping<Guid, DialogueInfoCompany> dialogues);

        // Cross index calculation
        double? CrossIndex(List<DialogueInfo> dialogues);

        double? CrossIndex(IGrouping<Guid, DialogueInfo> dialogues);

        double? CrossIndex(IGrouping<Guid, DialogueInfoCompany> dialogues);
        double? CrossIndex(IGrouping<string, RatingDialogueInfo> dialogues);
        double? AlertIndex(IGrouping<string, RatingDialogueInfo> dialogues);
        double? AlertIndex(IGrouping<Guid, DialogueInfo> dialogues);
        double? NecessaryIndex(IGrouping<string, RatingDialogueInfo> dialogues);

        double? LoyaltyIndex(IGrouping<string, RatingDialogueInfo> dialogues);

        double? LoyaltyIndex(List<ComponentsDialogueInfo> dialogues);


        // Efficiency index calculation
        double? EfficiencyIndex(List<SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg, DateTime end);

        double? EfficiencyIndex(List<SessionInfo> sessions, IGrouping<Guid, DialogueInfo> dialogues, DateTime beg, DateTime end);

        double? EfficiencyIndex(List<SessionInfoCompany> sessions, IGrouping<Guid, DialogueInfoCompany> dialogues, DateTime beg, DateTime end);


        int EmployeeCount(List<DialogueInfo> dialogues);
        int EmployeeCount(List<EfficiencyOptimizationHourInfo> info, double maxLoad, double maxPercent, double quantile = 0.95);

        int DialoguesCount(List<DialogueInfo> dialogues, Guid? applicationUserId = null, DateTime? date = null);
        Employee BestEmployeeLoad(List<DialogueInfo> dialogues, List<SessionInfo> sessions, DateTime beg, DateTime end);
        string BestEmployee(List<DialogueInfo> dialogues, List<SessionInfo> sessions, DateTime beg, DateTime end);

        string BestEmployee(List<DialogueInfo> dialogues);

        double? BestEmployeeEfficiency(List<DialogueInfo> dialogues, List<SessionInfo> sessions, DateTime beg, DateTime end);

        double? BestEmployeeSatisfaction(List<DialogueInfo> dialogues);

        string BestProgressiveEmployee(List<DialogueInfo> dialogues, DateTime beg);
        double? BestProgressiveEmployeeDelta(List<DialogueInfo> dialogues, DateTime beg);
        double? SessionTotalHours(List<SessionInfo> sessions, DateTime beg, DateTime end);
        double? SessionAverageHours(List<SessionInfo> sessions, DateTime beg = default(DateTime), DateTime end = default(DateTime));
        double? SessionAverageHours(IGrouping<DateTime, SessionInfo> sessions);
        double? SessionAverageHours(List<SessionInfo> sessions, Guid applicationUserId, DateTime? date, DateTime beg = default(DateTime), DateTime end = default(DateTime));


        double? DialogueSumDuration(IGrouping<DateTime, DialogueInfo> dialogues, DateTime beg = default(DateTime), DateTime end = default(DateTime));
        double? DialogueSumDuration(List<DialogueInfo> dialogues, DateTime beg, DateTime end);
        double? DialogueSumDuration(IGrouping<DateTime, SessionInfo> sessions, List<DialogueInfo> dialogues, Guid applicationUserId);
        double? DialogueAveragePause(List<SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg, DateTime end);
        double? DialogueAveragePause(List<SessionInfo> sessions, IGrouping<Guid, DialogueInfo> dialogues, DateTime beg, DateTime end);

        List<double> DialogueAvgPauseListInMinutes(List<SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg, DateTime end);

        double? DialogueAveragePause(List<SessionInfoCompany> sessions, IGrouping<Guid, DialogueInfoCompany> dialogues, DateTime beg, DateTime end);

        double? DialogueAverageDuration(List<DialogueInfo> dialogues, DateTime beg = default(DateTime), DateTime end = default(DateTime));

        double? DialogueAverageDuration(IGrouping<Guid, DialogueInfo> dialogues, DateTime beg = default(DateTime), DateTime end = default(DateTime));
        double? DialogueAverageDuration(IGrouping<Guid, DialogueInfoCompany> dialogues, DateTime beg, DateTime end);
        double? DialogueAverageDuration(IGrouping<DateTime, DialogueInfo> dialogues, DateTime beg = default(DateTime),
            DateTime end = default(DateTime));

        double? DialogueAverageDurationDaily(IGrouping<Guid, DialogueInfo> dialogues, DateTime beg, DateTime end);

        int? WorkingDaysCount(IGrouping<Guid, DialogueInfo> dialogues);

        double LoadPeriod(DateTime beg, DateTime end, List<DialogueInfo> dialogues, List<SessionInfo> sessions);
        List<EfficiencyOptimizationHourInfo> LoadDaily(DateTime beg, List<DialogueInfo> dialogues, List<SessionInfo> sessions);

        double? PeriodIntersection(DateTime beg, DateTime end, TimeSpan timeBeg, TimeSpan timeEnd);

        EfficiencyLoadDialogueTimeSatisfactionInfo PeriodSatisfaction(DialogueInfo dialogue, TimeSpan timeBeg, TimeSpan timeEnd);
        bool IsIntersect(DateTime beg1, DateTime end1, DateTime beg2, DateTime end2);

        bool TimeInPeriod(DateTime beg, DateTime end, TimeSpan time);

        double? LoadInterval(List<DialogueInfo> dialogues, List<SessionInfo> sessions, TimeSpan beg, TimeSpan end);

        double? SatisfactionInterval(List<DialogueInfo> dialogues, TimeSpan beg, TimeSpan end);

        List<EfficiencyLoadEmployeeTimeInfo> EmployeeTimeCalculation(List<DialogueInfo> dialogues, List<SessionInfo> sessions);

        List<ReportFullDayInfo> Sum(List<ReportFullDayInfo> curRes, ReportFullDayInfo newInterval);

        List<ReportFullDayInfo> TimeTable(List<SessionInfo> sessions, List<DialogueInfo> dialogues, Guid applicationUserId, DateTime date);
    }
}