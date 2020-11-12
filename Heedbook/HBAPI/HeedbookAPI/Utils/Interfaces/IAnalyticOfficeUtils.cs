using System;
using System.Collections.Generic;
using System.Linq;
using HBData.Models;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Utils.Interfaces
{
    public interface IAnalyticOfficeUtils
    {
        Employee BestEmployeeLoad(List<DialogueInfo> dialogues, List<SessionInfo> sessions, DateTime beg, DateTime end);
        bool CheckIfDialogueInWorkingTime(Dialogue dialogue, WorkingTime[] times);
        int DeviceCount(List<DialogueInfo> dialogues);
        double? DialogueAverageDuration(List<DialogueInfo> dialogues, DateTime beg = default, DateTime end = default);
        int DialoguesCount(List<DialogueInfo> dialogues, Guid? applicationUserId = null, DateTime? date = null);
        double? DialogueTotalDuration(List<DialogueInfo> dialogues, DateTime beg = default, DateTime end = default);
        int EmployeeCount(List<DialogueInfo> dialogues);
        double? LoadIndex(List<SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg, DateTime end);
        double? LoadIndex(double? workinHours, double? dialogueHours);
        double? LoadIndex(List<SessionInfo> sessions, IGrouping<Guid, DialogueInfo> dialogues, DateTime beg, DateTime end);
        T Max<T>(T val1, T val2) where T : IComparable<T>;
        double? MaxDouble(double? x, double? y);
        T Min<T>(T val1, T val2) where T : IComparable<T>;
        double? SatisfactionIndex(List<DialogueInfo> dialogues);
        double? SessionAverageHours(List<SessionInfo> sessions, DateTime beg = default, DateTime end = default);
        double? SessionTotalHours(List<SessionInfo> sessions, DateTime beg, DateTime end);
    }
}