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
    public class AnalyticHomeUtils
    {
        private readonly IConfiguration _config;

        public AnalyticHomeUtils(IConfiguration config)
        {
            _config = config;
        }

        public double? MaxDouble(double? x, double? y)
        {
            return x > y ? x : y;
        }
        public double? SignedPower(double x, double power)
        {
            return (x != 0) ? Math.Sign(x) * Math.Pow(Math.Abs(x), power) : 0;
        }
        public T Min<T>(T val1, T val2) where T : IComparable<T>
        {
            if ((val1 as DateTime?) == default(DateTime)) return val2;
            if ((val2 as DateTime?) == default(DateTime)) return val1;
            return val1.CompareTo(val2) < 0 ? val1 : val2;
        }
        public T Max<T>(T val1, T val2) where T : IComparable<T>
        {
            if ((val1 as DateTime?) == default(DateTime)) return val2;
            if ((val2 as DateTime?) == default(DateTime)) return val1;
            return val1.CompareTo(val2) > 0 ? val1 : val2;
        }

        public int DialoguesCount(List<DialogueInfo> dialogues, Guid? applicationUserId = null, DateTime? date = null)
        {
            return dialogues.Any() ? dialogues
                .Where(p => (applicationUserId == null || p.ApplicationUserId == applicationUserId) &&
                    (date == null || p.BegTime.Date == date))
                .Select(p => p.DialogueId).Distinct().Count() : 0;
        }
        public int EmployeeCount(List<DialogueInfo> dialogues)
        {
            return dialogues.Any() ? dialogues.Select(p => p.ApplicationUserId).Distinct().Count() : 0;
        }
        public string BestEmployee(List<DialogueInfo> dialogues, List<SessionInfo> sessions, DateTime beg, DateTime end)
        {
            return dialogues.Any() ? dialogues
                .GroupBy(p => p.ApplicationUserId)
                .Select(p => new
                {
                    FullName = p.First().FullName,
                    EfficiencyIndex = EfficiencyIndex(sessions, p, beg, end)
                    //EfficiencyIndex
                })
                .OrderByDescending(p => p.EfficiencyIndex)
                .Take(1)
                .FirstOrDefault()
                .FullName : "";
        }
        public double? BestProgressiveEmployeeDelta(List<DialogueInfo> dialogues, DateTime beg)
        {
            return dialogues.Any() ? dialogues
                .GroupBy(p => p.ApplicationUserId)
                .Select(p => new
                {
                    FullName = p.First().FullName,
                    SatisfactionScoreDelta = p.Where(q => q.BegTime.Date >= beg.Date).Average(q => q.SatisfactionScore) - p.Where(q => q.EndTime.Date < beg.Date).Average(q => q.SatisfactionScore)
                })
                .OrderByDescending(p => p.SatisfactionScoreDelta)
                .Take(1)
                .FirstOrDefault()
                .SatisfactionScoreDelta : 0;
        }
        public string BestProgressiveEmployee(List<DialogueInfo> dialogues, DateTime beg)
        {
            return dialogues.Any() ? dialogues
                .GroupBy(p => p.ApplicationUserId)
                .Select(p => new
                {
                    FullName = p.First().FullName,
                    SatisfactionScoreDelta = p.Where(q => q.BegTime.Date >= beg.Date).Average(q => q.SatisfactionScore) - p.Where(q => q.EndTime.Date < beg.Date).Average(q => q.SatisfactionScore)
                })
                .OrderByDescending(p => p.SatisfactionScoreDelta)
                .Take(1)
                .FirstOrDefault()
                .FullName : "";
        }
        public double? BestEmployeeEfficiency(List<DialogueInfo> dialogues, List<SessionInfo> sessions, DateTime beg, DateTime end)
        {
            return dialogues.Any() ? dialogues
                .GroupBy(p => p.ApplicationUserId)
                .Select(p => new
                {
                    FullName = p.First().FullName,
                    EfficiencyIndex = EfficiencyIndex(sessions, p, beg, end)
                    //EfficiencyIndex
                })
                .OrderByDescending(p => p.EfficiencyIndex)
                .Take(1)
                .FirstOrDefault()
                .EfficiencyIndex : 0;
        }
        public List<BestEmployee> BestThreeEmployees(List<DialogueInfo> dialogues, List<SessionInfo> sessions, DateTime beg, DateTime end)
        {
            return dialogues.Any() ? dialogues
                .Where(p => p.ApplicationUserId != null)
                .GroupBy(p => p.ApplicationUserId)
                .Select(p => new BestEmployee
                {
                    Name = p.First().FullName,
                    EfficiencyIndex = EfficiencyIndex(sessions, p, beg, end),
                    SatisfactionIndex = SatisfactionIndex(p),
                    LoadIndex = LoadIndex(sessions, p, beg, end),
                    CrossIndex = CrossIndex(p)
                })
                .OrderByDescending(p => p.EfficiencyIndex)
                .Take(3).ToList() : new List<BestEmployee>();
        }

        public double? EfficiencyIndex(List<SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            double? result = 0;
            double? normCoeff = 0;

            result += (SatisfactionIndex(dialogues) != null) ? Convert.ToDouble(_config["Weights: SatisfactionIndexWeight"]) * SatisfactionIndex(dialogues) : 0;
            normCoeff += Convert.ToDouble(_config["Weights: SatisfactionIndexWeight"]);

            result += (LoadIndex(sessions, dialogues, beg, end) != null) ? Convert.ToDouble(_config["Weights: LoadIndexWeight"]) * LoadIndex(sessions, dialogues, beg, end) : 0;
            normCoeff += Convert.ToDouble(_config["Weights: LoadIndexWeight"]);

            result += (CrossIndex(dialogues) != null) ? Convert.ToDouble(_config["Weights: CrossIndexWeight"]) * CrossIndex(dialogues) : 0;
            normCoeff += Convert.ToDouble(_config["Weights: CrossIndexWeight"]);

            normCoeff = (normCoeff != 0 & normCoeff != null) ? normCoeff : 1;
            return normCoeff != 0 ? result / normCoeff : null;
        }
        public double? EfficiencyIndex(List<SessionInfo> sessions, IGrouping<Guid?, DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            double? result = 0;
            double? normCoeff = 0;

            result += (SatisfactionIndex(dialogues) != null) ? Convert.ToDouble(_config["Weights: SatisfactionIndexWeight"]) * SatisfactionIndex(dialogues) : 0;
            normCoeff += Convert.ToDouble(_config["Weights: SatisfactionIndexWeight"]);

            result += (LoadIndex(sessions, dialogues, beg, end) != null) ? Convert.ToDouble(_config["Weights: LoadIndexWeight"]) * LoadIndex(sessions, dialogues, beg, end) : 0;
            normCoeff += Convert.ToDouble(_config["Weights: LoadIndexWeight"]);

            result += (CrossIndex(dialogues) != null) ? Convert.ToDouble(_config["Weights: CrossIndexWeight"]) * CrossIndex(dialogues) : 0;
            normCoeff += Convert.ToDouble(_config["Weights: CrossIndexWeight"]);

            normCoeff = (normCoeff != 0 & normCoeff != null) ? normCoeff : 1;
            return normCoeff != 0 ? result / normCoeff : null;
        }
        public double? CrossIndex(IGrouping<Guid?, DialogueInfo> dialogues)
        {
            var dialoguesCount = dialogues.Any() ? dialogues.Select(p => p.DialogueId).Distinct().Count() : 0;
            var crossDialoguesCount = dialogues.Any() ? dialogues.Sum(p => Math.Min(p.CrossCount, 1)) : 0;
            return dialoguesCount != 0 ? 100 * Convert.ToDouble(crossDialoguesCount) / Convert.ToDouble(dialoguesCount) : 0;
        }
        public double? CrossIndex(List<DialogueInfo> dialogues)
        {
            var dialoguesCount = dialogues.Any() ? dialogues.Select(p => p.DialogueId).Distinct().Count() : 0;
            var crossDialoguesCount = dialogues.Any() ? dialogues.Sum(p => Math.Min(p.CrossCount, 1)) : 0;
            return dialoguesCount != 0 ? 100 * Convert.ToDouble(crossDialoguesCount) / Convert.ToDouble(dialoguesCount) : 0;
        }
        public double? SatisfactionDialogueDelta(List<DialogueInfo> dialogues)
        {
            var delta = dialogues.Any() ? SignedPower(dialogues.Average(p =>
                Math.Pow(Convert.ToDouble(p.SatisfactionScoreEnd - p.SatisfactionScoreBeg + 1.0 / 3.0), 3)), 1.0 / 3.0) : 0;
            return delta;
        }
        public double? SatisfactionIndex(IGrouping<Guid?, DialogueInfo> dialogues)
        {
            return dialogues.Any() ? dialogues.Where(p => p.SatisfactionScore != null && p.SatisfactionScore != 0).Average(p => p.SatisfactionScore) : null;
        }
        public double? SatisfactionIndex(List<DialogueInfo> dialogues)
        {
            return dialogues.Any() ? dialogues.Where(p => p.SatisfactionScore != null && p.SatisfactionScore != 0).Average(p => p.SatisfactionScore) : null;
        }
        public double? LoadIndex(List<SessionInfo> sessions, IGrouping<Guid?, DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            var sessionsGroup = sessions.Where(p => p.ApplicationUserId == dialogues.Key);
            var sessionHours = sessionsGroup.Any() ? sessionsGroup.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
            var dialoguesHours = dialogues.Any() ? dialogues.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
            return 100 * LoadIndex( sessionHours, dialoguesHours);
        }
        public double? LoadIndex(List<SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            var sessionHours = sessions.Any() ? sessions.Sum(p =>
               Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;

            var dialoguesHours = dialogues.Any() ? dialogues.Sum(p =>
                Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
            return 100 * LoadIndex(sessionHours, dialoguesHours);
        }
        public double? LoadIndex(double? workinHours, double? dialogueHours)
        {
            workinHours = MaxDouble(workinHours, dialogueHours);
            return workinHours != 0 ? (double?)dialogueHours / workinHours : 0;
        }

        public double? DialoguesPerUser(List<DialogueInfo> dialogues)
        {
            if (dialogues.Any())
            {
                return dialogues.GroupBy(p => p.BegTime.Date).Select(p => new
                {
                    Count = p.Count(),
                    UsersCount = p.Select(q => q.ApplicationUserId).Distinct().Count()
                })
                .Average(p => p.Count / p.UsersCount);
            }
            else
            {
                return 0;
            }
        }
        public double? DialogueAverageDuration(List<DialogueInfo> dialogues, DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            return dialogues.Any() ? dialogues.Average(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
        }   
        public double? SessionAverageHours(List<SessionInfo> sessions, DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            return sessions.Any() ?
                (double?)sessions.GroupBy(p => p.BegTime.Date)
                        .Select(q => 
                            q.Sum(r => Min(r.EndTime, end).Subtract(Max(r.BegTime, beg)).TotalHours) / q.Select(r => r.ApplicationUserId).Distinct().Count()
                        ).Average() : null;
        }
    }
}