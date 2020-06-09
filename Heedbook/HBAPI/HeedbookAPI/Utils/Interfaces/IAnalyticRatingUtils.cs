using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using UserOperations.Models.AnalyticModels;
using UserOperations.Models.Get.AnalyticRatingController;

namespace UserOperations.Utils.Interfaces
{
    public interface IAnalyticRatingUtils
    {
        double? CrossIndex(IGrouping<Guid?, DialogueInfo> dialogues);
        double? CrossIndex(List<DialogueInfo> dialogues);
        double? CrossIndex(IGrouping<DateTime, DialogueInfo> dialogues);
        double? DialogueAverageDuration(IGrouping<DateTime, DialogueInfo> dialogues, DateTime beg = default, DateTime end = default);
        double? DialogueAverageDuration(IGrouping<Guid?, DialogueInfo> dialogues, DateTime beg, DateTime end);
        double? DialogueHourAveragePause(List<double> sessionMinTime, List<DialogueInfo> dialogues, DateTime beg, DateTime end);
        double? DialogueSumDuration(IGrouping<DateTime, DialogueInfo> dialogues, DateTime beg = default, DateTime end = default);
        double? LoadIndex(double? workinHours, double? dialogueHours);
        double? LoadIndex(List<SessionInfo> sessions, IGrouping<Guid?, DialogueInfo> dialogues, DateTime beg, DateTime end);
        double? LoadIndex(List<SessionInfo> sessions, IGrouping<DateTime, DialogueInfo> dialogues, Guid? applicationUserId, DateTime? date, DateTime beg = default, DateTime end = default);
        double? LoadIndexWithTimeTableForUser(List<CompanyTimeTable> workingTimeTable, List<DialogueInfo> dialogues, DateTime beg, DateTime end);
        T Max<T>(T val1, T val2) where T : IComparable<T>;
        double? MaxDouble(double? x, double? y);
        T Min<T>(T val1, T val2) where T : IComparable<T>;
        double? SatisfactionIndex(IGrouping<Guid?, DialogueInfo> dialogues);
        double? SatisfactionIndex(List<DialogueInfo> dialogues);
        double? SessionAverageHours(List<SessionInfo> sessions, DateTime beg = default, DateTime end = default);
        double? SessionAverageHours(List<SessionInfo> sessions, Guid? applicationUserId, DateTime? date, DateTime beg = default, DateTime end = default);
    }
}