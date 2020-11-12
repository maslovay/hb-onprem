using System;
using System.Collections.Generic;
using System.Linq;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Utils.Interfaces
{
    public interface IAnalyticReportUtils
    {
        int DialoguesCount(List<DialogueInfo> dialogues, Guid? applicationUserId = null, DateTime? date = null);
        double? DialogueSumDuration(List<DialogueInfo> dialogues, DateTime beg, DateTime end);
        double? DialogueSumDuration(IGrouping<DateTime, SessionInfo> sessions, List<DialogueInfo> dialogues, Guid? applicationUserId);
        double? LoadIndex(List<SessionInfo> sessions, List<DialogueInfo> dialogues, Guid? applicationUserId, DateTime date, DateTime beg = default, DateTime end = default);
        double? LoadIndex(double? workinHours, double? dialogueHours);
        double? LoadIndex(IGrouping<Guid?, SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg, DateTime end);
        T Max<T>(T val1, T val2) where T : IComparable<T>;
        double? MaxDouble(double? x, double? y);
        T Min<T>(T val1, T val2) where T : IComparable<T>;
        double? SessionAverageHours(List<SessionInfo> sessions, Guid? applicationUserId, DateTime? date, DateTime beg = default, DateTime end = default);
        double? SessionAverageHours(IGrouping<DateTime, SessionInfo> sessions);
        List<ReportFullDayInfo> Sum(List<ReportFullDayInfo> curRes, ReportFullDayInfo newInterval);
        List<ReportFullDayInfo> TimeTable(List<SessionInfo> sessions, List<DialogueInfo> dialogues, Guid? applicationUserId, DateTime date);
    }
}