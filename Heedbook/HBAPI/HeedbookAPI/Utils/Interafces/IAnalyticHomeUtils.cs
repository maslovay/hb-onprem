using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using HBData;
using UserOperations.Models.Get.HomeController;
using HBData.Models;
using UserOperations.Controllers;
using System.Reflection;
using UserOperations.Models.Get;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Utils.AnalyticHomeUtils
{
    public interface IAnalyticHomeUtils
    {
        string BestEmployee(List<DialogueInfo> dialogues, List<SessionInfo> sessions, DateTime beg, DateTime end);
        double? BestEmployeeEfficiency(List<DialogueInfo> dialogues, List<SessionInfo> sessions, DateTime beg, DateTime end);
        string BestProgressiveEmployee(List<DialogueInfo> dialogues, DateTime beg);
        double? BestProgressiveEmployeeDelta(List<DialogueInfo> dialogues, DateTime beg);
        List<BestEmployee> BestThreeEmployees(List<DialogueInfo> dialogues, List<SessionInfo> sessions, DateTime beg, DateTime end);
        double? CrossIndex(IGrouping<Guid?, DialogueInfo> dialogues);
        double? CrossIndex(List<DialogueInfo> dialogues);
        double? DialogueAverageDuration(List<DialogueInfo> dialogues, DateTime beg = default, DateTime end = default);
        int DialoguesCount(List<DialogueInfo> dialogues, Guid? applicationUserId = null, DateTime? date = null);
        double? DialoguesPerUser(List<DialogueInfo> dialogues);
        double? EfficiencyIndex(List<SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg, DateTime end);
        double? EfficiencyIndex(List<SessionInfo> sessions, IGrouping<Guid?, DialogueInfo> dialogues, DateTime beg, DateTime end);
        int EmployeeCount(List<DialogueInfo> dialogues);
        double? LoadIndex(List<SessionInfo> sessions, IGrouping<Guid?, DialogueInfo> dialogues, DateTime beg, DateTime end);
        double? LoadIndex(List<SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg, DateTime end);
        double? LoadIndex(double? workinHours, double? dialogueHours);
        double? LoadIndexWithTimeTable(List<double> workingTimeTable, List<DialogueInfo> dialogues, DateTime beg, DateTime end);
        T Max<T>(T val1, T val2) where T : IComparable<T>;
        double? MaxDouble(double? x, double? y);
        T Min<T>(T val1, T val2) where T : IComparable<T>;
        double? SatisfactionDialogueDelta(List<DialogueInfo> dialogues);
        double? SatisfactionIndex(IGrouping<Guid?, DialogueInfo> dialogues);
        double? SatisfactionIndex(List<DialogueInfo> dialogues);
        double? SessionAverageHours(List<SessionInfo> sessions, DateTime beg = default, DateTime end = default);
        double? SignedPower(double x, double power);
    }
}