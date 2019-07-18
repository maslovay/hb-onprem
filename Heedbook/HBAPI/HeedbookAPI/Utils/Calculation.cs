using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using HBData;
using UserOperations.Models.AnalyticModels;
using HBData.Models;
using UserOperations.Controllers;
using System.Reflection;

namespace UserOperations.Utils
{
    public class DBOperations
    {
        private readonly RecordsContext _context;
        private readonly IConfiguration _config;

        public DBOperations(RecordsContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        public T Max<T>(T val1, T val2) where T : IComparable<T>
        {  
            if ((val1 as DateTime?) == default(DateTime)) return val2;
            if ((val2 as DateTime?) == default(DateTime)) return val1;
            return val1.CompareTo(val2) > 0 ? val1 : val2;
        }
        public T Min<T>(T val1, T val2) where T : IComparable<T>
        {
            if ((val1 as DateTime?) == default(DateTime)) return val2;
            if ((val2 as DateTime?) == default(DateTime)) return val1;
            return val1.CompareTo(val2) < 0 ? val1 : val2;
        }

        // public DateTime MaxTime(DateTime time1, DateTime time2)
        // {
        //     return time1 > time2 ? time1 : time2;
        // }
        // public DateTime MinTime(DateTime time1, DateTime time2)
        // {
        //     return time1 > time2 ? time2 : time1;
        // }
        public double? SignedPower(double x, double power)
        {
            return (x != 0) ? Math.Sign(x) * Math.Pow(Math.Abs(x), power) : 0;
        }

        public double? MaxDouble(double? x, double? y)
        {
            return x > y ? x : y;
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


        public double? SatisfactionDialogueDelta(List<DialogueInfo> dialogues)
        {
            var delta = dialogues.Any() ? SignedPower(dialogues.Average(p =>
                Math.Pow(Convert.ToDouble(p.SatisfactionScoreEnd - p.SatisfactionScoreBeg + 1.0 / 3.0), 3)), 1.0 / 3.0) : 0;
            return delta;
        }
        public double? LoadIndex(double? workinHours, double? dialogueHours)
        {
            workinHours = MaxDouble(workinHours, dialogueHours);
            return workinHours != 0 ? (double?)dialogueHours / workinHours : 0;
        }

        public double? LoadIndex(List<SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg , DateTime end )
        {           
             var sessionHours = sessions.Any() ? sessions.Sum(p =>
                Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;

            var dialoguesHours = dialogues.Any() ? dialogues.Sum(p =>
                Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
            return 100 * LoadIndex( sessionHours, dialoguesHours);
        }

        // public double? LoadIndex(List<SessionInfoCompany> sessions, List<DialogueInfoCompany> dialogues, DateTime beg, DateTime end)
        // {
        //     var sessionHours = sessions.Any() ? sessions.Sum(p => 
        //         MinTime(p.EndTime, end).Subtract(MaxTime(p.BegTime, beg)).TotalHours) : 0;
        //     var dialoguesHours = dialogues.Any() ? dialogues.Sum(p => 
        //         MinTime(p.EndTime, end).Subtract(MaxTime(p.BegTime, beg)).TotalHours) : 0;
        //     return sessionHours != 0 ? (double?) 100 * dialoguesHours / sessionHours : null;
        // }

        public double? LoadIndex(List<SessionInfo> sessions, IGrouping<Guid, DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            var sessionsGroup = sessions.Where(p => p.ApplicationUserId == dialogues.Key);
            var sessionHours = sessionsGroup.Any() ? sessionsGroup.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
            var dialoguesHours = dialogues.Any() ? dialogues.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
            return 100 * LoadIndex( sessionHours, dialoguesHours);
        }

        public double? LoadIndex(List<SessionInfoCompany> sessions, IGrouping<Guid, DialogueInfoCompany> dialogues, DateTime beg, DateTime end)
        {
            var sessionsGroup = sessions.Where(p => p.CompanyId == dialogues.Key);
            var sessionHours = sessionsGroup.Any() ? sessionsGroup.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
            var dialoguesHours = dialogues.Any() ? dialogues.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
            return 100 * LoadIndex( sessionHours, dialoguesHours);
        }       
      
        public double? LoadIndex(List<SessionInfo> sessions, IGrouping<DateTime, DialogueInfo> dialogues,
            Guid applicationUserId, DateTime? date,  DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            var sessionsUser = sessions.Where(p => p.ApplicationUserId == applicationUserId && p.BegTime.Date == date);
            var sessionHours = sessionsUser.Any() ? Convert.ToDouble(sessionsUser.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours)) : 0;
            var dialoguesHours = dialogues.Any() ? Convert.ToDouble(dialogues.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours)) : 0;
            return 100 * LoadIndex( sessionHours, dialoguesHours);
        }

        public double? LoadIndex(IGrouping<Guid, SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            var sessionHours = sessions.Count() != 0 ? sessions.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
            var dialoguesHours = dialogues.Count() != 0 ? dialogues.Where(p => p.ApplicationUserId == sessions.Key).Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
            return 100 * LoadIndex( sessionHours, dialoguesHours);
        }

        public double? LoadIndex(List<SessionInfo> sessions, List<DialogueInfo> dialogues,
            Guid applicationUserId, DateTime date, DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            var sessionsUser = sessions.Where(p => p.ApplicationUserId == applicationUserId && p.BegTime.Date == date);
            var sessionHours = sessionsUser.Count() != 0 ? Convert.ToDouble(sessionsUser.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours)) : 0;
            var dialoguesHours = dialogues.Count() != 0 ? Convert.ToDouble(dialogues.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours)) : 0;
            return 100 * LoadIndex( sessionHours, dialoguesHours);
        }     

        //Satisfaction index calculation
        public double? SatisfactionIndex(List<DialogueInfo> dialogues)
        {
            return dialogues.Any() ? dialogues.Where(p => p.SatisfactionScore != null && p.SatisfactionScore != 0).Average(p => p.SatisfactionScore) : null;
        }

        public double? SatisfactionIndex(IGrouping<Guid, DialogueInfo> dialogues)
        {
            return dialogues.Any() ? dialogues.Where(p => p.SatisfactionScore != null && p.SatisfactionScore != 0).Average(p => p.SatisfactionScore) : null;
        }

        public double? SatisfactionIndex(IGrouping<Guid, DialogueInfoCompany> dialogues)
        {
            return dialogues.Any() ? dialogues.Where(p => p.SatisfactionScore != null && p.SatisfactionScore != 0).Average(p => p.SatisfactionScore) : null;
        }

        // Cross index calculation
        public double? CrossIndex(List<DialogueInfo> dialogues)
        {
            var dialoguesCount = dialogues.Any() ? dialogues.Select(p => p.DialogueId).Distinct().Count() : 0;
            var crossDialoguesCount = dialogues.Any() ? dialogues.Sum(p => Math.Min(p.CrossCount, 1)) : 0;
            return dialoguesCount != 0 ? 100 * Convert.ToDouble(crossDialoguesCount) / Convert.ToDouble(dialoguesCount) : 0;
        }

        public double? CrossIndex(IGrouping<Guid, DialogueInfo> dialogues)
        {
            var dialoguesCount = dialogues.Any() ? dialogues.Select(p => p.DialogueId).Distinct().Count() : 0;
            var crossDialoguesCount = dialogues.Any() ? dialogues.Sum(p => Math.Min(p.CrossCount, 1)) : 0;
            return dialoguesCount != 0 ? 100 * Convert.ToDouble(crossDialoguesCount) / Convert.ToDouble(dialoguesCount) : 0;
        }

        public double? CrossIndex(IGrouping<Guid, DialogueInfoCompany> dialogues)
        {
            var dialoguesCount = dialogues.Any() ? dialogues.Select(p => p.DialogueId).Distinct().Count() : 0;
            var crossDialoguesCount = dialogues.Any() ? dialogues.Sum(p => Math.Min(p.CrossCount, 1)) : 0;
            return dialoguesCount != 0 ? 100 * Convert.ToDouble(crossDialoguesCount) / Convert.ToDouble(dialoguesCount) : 0;
        }
        public double? CrossIndex(IGrouping<string, RatingDialogueInfo> dialogues)
        {
            var dialoguesCount = dialogues.Any() ? dialogues.Select(p => p.DialogueId).Distinct().Count() : 0;
            var crossDialoguesCount = dialogues.Any() ? dialogues.Sum(p => Math.Min(p.CrossCount, 1)) : 0;
            return dialoguesCount != 0 ? 100 * Convert.ToDouble(crossDialoguesCount) / Convert.ToDouble(dialoguesCount) : 0;
        }
        public double? AlertIndex(IGrouping<string, RatingDialogueInfo> dialogues)
        {
            var dialoguesCount = dialogues.Any() ? dialogues.Select(p => p.DialogueId).Distinct().Count() : 0;
            var alertDialoguesCount = dialogues.Any() ? dialogues.Sum(p => Math.Min(p.AlertCount, 1)) : 0;
            return dialoguesCount != 0 ? 100 * Convert.ToDouble(alertDialoguesCount) / Convert.ToDouble(dialoguesCount) : 0;
        }
        public double? AlertIndex(IGrouping<Guid, DialogueInfo> dialogues)
        {
            var dialoguesCount = dialogues.Any() ? dialogues.Select(p => p.DialogueId).Distinct().Count() : 0;
            var alertDialoguesCount = dialogues.Any() ? dialogues.Sum(p => Math.Min(p.AlertCount, 1)) : 0;
            return dialoguesCount != 0 ? 100 * Convert.ToDouble(alertDialoguesCount) / Convert.ToDouble(dialoguesCount) : 0;
        }
           public double? NecessaryIndex(IGrouping<string, RatingDialogueInfo> dialogues)
        {
            var dialoguesCount = dialogues.Any() ? dialogues.Select(p => p.DialogueId).Distinct().Count() : 0;
            var necessaryDialoguesCount = dialogues.Any() ? dialogues.Sum(p => Math.Min(p.NecessaryCount, 1)) : 0;
            return dialoguesCount != 0 ? 100 * Convert.ToDouble(necessaryDialoguesCount) / Convert.ToDouble(dialoguesCount) : 0;
        }

        public double? LoyaltyIndex(List<ComponentsDialogueInfo> dialogues)
        {
            var dialoguesCount = dialogues.Any() ? dialogues.Select(p => p.DialogueId).Distinct().Count() : 0;
            var crossDialoguesCount = dialogues.Any() ? dialogues.Sum(p => Math.Min(p.Loyalty, 1)) : 0;
            return dialoguesCount != 0 ? 100 * Convert.ToDouble(crossDialoguesCount) / Convert.ToDouble(dialoguesCount) : 0;
        }
      

        // Efficiency index calculation
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

        public double? EfficiencyIndex(List<SessionInfo> sessions, IGrouping<Guid, DialogueInfo> dialogues, DateTime beg, DateTime end)
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

        public double? EfficiencyIndex(List<SessionInfoCompany> sessions, IGrouping<Guid, DialogueInfoCompany> dialogues, DateTime beg, DateTime end)
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


        public int EmployeeCount(List<DialogueInfo> dialogues)
        {
            return dialogues.Any() ? dialogues.Select(p => p.ApplicationUserId).Distinct().Count() : 0;
        }

        public int EmployeeCount(List<EfficiencyOptimizationHourInfo> info, double maxLoad, double maxPercent, double quantile = 0.95)
        {
            var percent = info.Any() ? info.Where(p => p.Load > maxLoad).Count() / info.Count() : 0;
            if (percent > maxPercent)
            {
                return Convert.ToInt32(Math.Ceiling(info.Where(p => p.Load > maxPercent).Average(p => p.UsersCount) + 1));
            }
            else
            {
                if (info.Any())
                {
                    var index = Math.Max(Convert.ToInt32(Math.Round((Convert.ToDouble(info.Count()) * quantile))) - 1, 0);
                    var res = info.Select(p => new
                    {
                        userCount = (p.Load > maxLoad) ? (p.UsersCount + 1) :
                        Convert.ToInt32(Math.Ceiling(p.Load / maxLoad * p.UsersCount))
                    });
                    return res.OrderBy(p => p.userCount).Select(p => p.userCount).ToList()[index];
                }
                else
                {
                    return 0;
                }
            }
        }

        public int DialoguesCount(List<DialogueInfo> dialogues, Guid? applicationUserId = null, DateTime? date = null)
        {
            return dialogues.Any() ? dialogues
                .Where(p => (applicationUserId == null || p.ApplicationUserId == applicationUserId) &&
                    (date == null || p.BegTime.Date == date))
                .Select(p => p.DialogueId).Distinct().Count() : 0;
        }
        public Employee BestEmployeeLoad(List<DialogueInfo> dialogues, List<SessionInfo> sessions, DateTime beg, DateTime end)
        {
            return dialogues.Any() ? dialogues
                .GroupBy(p => p.ApplicationUserId)
                .Select(p => new Employee
                {
                    BestEmployeeId = p.First().ApplicationUserId,
                    BestEmployeeName = p.First().FullName,
                    LoadValue = LoadIndex(sessions, p, beg, end),
                    Date = p.First().BegTime
                    //EfficiencyIndex
                })
                .OrderByDescending(p => p.LoadValue)
                .Take(1)
                .FirstOrDefault() : null;
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

        public string BestEmployee(List<DialogueInfo> dialogues)
        {
            return dialogues.Count() != 0 ? dialogues
                .GroupBy(p => p.ApplicationUserId)
                .Select(p => new
                {
                    FullName = p.First().FullName,
                    SatisfactionScore = p.Average(q => q.SatisfactionScore)
                })
                .OrderByDescending(p => p.SatisfactionScore)
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

        public double? BestEmployeeSatisfaction(List<DialogueInfo> dialogues)
        {
            return dialogues.Count() != 0 ? dialogues
                .GroupBy(p => p.ApplicationUserId)
                .Select(p => new
                {
                    FullName = p.First().FullName,
                    SatisfactionScore = p.Average(q => q.SatisfactionScore)
                })
                .OrderByDescending(p => p.SatisfactionScore)
                .Take(1)
                .FirstOrDefault()
                .SatisfactionScore : null;
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
        public double? SessionTotalHours(List<SessionInfo> sessions, DateTime beg, DateTime end)
        {
            return sessions.Any() ?
                (double?)sessions.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
        }

        public double? SessionAverageHours(List<SessionInfo> sessions, DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            return sessions.Any() ?
                (double?)sessions.GroupBy(p => p.BegTime.Date)
                        .Select(q => 
                            q.Sum(r => Min(r.EndTime, end).Subtract(Max(r.BegTime, beg)).TotalHours) / q.Select(r => r.ApplicationUserId).Distinct().Count()
                        ).Average() : null;
        }

        public double? SessionAverageHours(IGrouping<DateTime, SessionInfo> sessions)
        {     
            DateTime beg = sessions.Key;
            DateTime end = sessions.Key.AddDays(1);
            var sessionHours = sessions.Any() ? (double?)Convert.ToDouble(sessions.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours)) : null;
            return sessionHours;
        }
        public double? SessionAverageHours(List<SessionInfo> sessions, Guid applicationUserId, DateTime? date, DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            var sessionsUser = sessions.Where(p => p.ApplicationUserId == applicationUserId && p.BegTime.Date == date);
            return sessionsUser.Any() ? (double?)sessionsUser.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : null;
        }


        public double? DialogueSumDuration(IGrouping<DateTime, DialogueInfo> dialogues, DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            return dialogues.Any() ? (double?)dialogues.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : null;
        }

        public double? DialogueSumDuration(List<DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            return dialogues.Any() ? (double?)dialogues.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : null;
        }

        public double? DialogueSumDuration(IGrouping<DateTime, SessionInfo> sessions, List<DialogueInfo> dialogues, Guid applicationUserId)
        {
            DateTime beg = sessions.Key;
            DateTime end = sessions.Key.AddDays(1);
            var dialoguesUser = dialogues.Where(p => p.ApplicationUserId == applicationUserId && (p.BegTime.Date == beg || p.EndTime.Date == sessions.Key));
            var dialoguesHours = dialoguesUser.Count() != 0 ? (double?)Convert.ToDouble(dialoguesUser.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours)) : null;
            return dialoguesHours??0;
        }
        public double? DialogueAveragePause(List<SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            var sessionHours = sessions.Any() ? sessions.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
            var dialoguesHours = dialogues.Any() ? dialogues.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
            return dialogues.Any() ? (double?)(sessionHours - dialoguesHours) / dialogues.Select(p => p.DialogueId).Distinct().Count() : null;
        }
        public double? DialogueAveragePause(List<SessionInfo> sessions, IGrouping<Guid, DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            var sessionHours = sessions.Where(p => p.ApplicationUserId == dialogues.Key).Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours);
            var dialoguesHours = dialogues.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours);
            return (sessionHours - dialoguesHours) / dialogues.Select(p => p.DialogueId).Distinct().Count();
        }

        public List<double> DialogueAvgPauseListInMinutes(List<SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            List<double> pauses = new List<double>();
            if (!sessions.Any() || !dialogues.Any()) return null;
            foreach( var sessionGrouping in sessions.GroupBy(x => x.ApplicationUserId))
            {
            foreach( var ses in sessionGrouping.OrderBy(p => p.BegTime))
            {
                var dialogInSession = dialogues
                        .Where(p => 
                        p.ApplicationUserId == ses.ApplicationUserId
                        && p.BegTime >= Max(ses.BegTime, beg) 
                        && p.EndTime <= Min(ses.EndTime, end))
                        .OrderBy(p => p.BegTime)
                        .ToArray();
                if(dialogInSession != null && dialogInSession.Count() != 0)
                {
                    pauses.Add(dialogInSession.First().BegTime.Subtract(Max(ses.BegTime, beg)).TotalMinutes);
                    for ( int i = 1; i < dialogInSession.Length - 1; i++)
                    {
                        pauses.Add(dialogInSession[i].BegTime.Subtract(dialogInSession[i-1].EndTime).TotalMinutes);
                    }
                    pauses.Add(Min(ses.EndTime, end).Subtract(dialogInSession.Last().EndTime).TotalMinutes);
                }
                else
                pauses.Add(Min(ses.EndTime, end).Subtract(Max(ses.BegTime, beg)).TotalMinutes);
            }
            }
            return pauses;
        }
        public double? DialogueAveragePause(List<SessionInfoCompany> sessions, IGrouping<Guid, DialogueInfoCompany> dialogues, DateTime beg, DateTime end)
        {
            var sessionHours = sessions.Where(p => p.CompanyId == dialogues.Key).Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours);
            var dialoguesHours = dialogues.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours);
            return (sessionHours - dialoguesHours) / dialogues.Select(p => p.DialogueId).Distinct().Count();
        }

        public double? DialogueAverageDuration(List<DialogueInfo> dialogues, DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            return dialogues.Any() ? dialogues.Average(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
        }

        public double? DialogueAverageDuration(IGrouping<Guid, DialogueInfo> dialogues, DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            return dialogues.Average(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours);
        }
        public double? DialogueAverageDuration(IGrouping<Guid, DialogueInfoCompany> dialogues, DateTime beg, DateTime end)
        {
            return dialogues.Average(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours);
        }
        public double? DialogueAverageDuration(IGrouping<DateTime, DialogueInfo> dialogues, DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            return dialogues.Any() ? (double?)dialogues.Average(r => Min(r.EndTime, end).Subtract(Max(r.BegTime, beg)).TotalHours) : null;
        }

        public double? DialogueAverageDurationDaily(IGrouping<Guid, DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            return dialogues
                .GroupBy(p => p.BegTime.Date)
                .Select(p => new
                {
                    WorkingTime = p.Sum(q => Math.Max(Min(q.EndTime, end).Subtract(Max(q.BegTime, beg)).TotalHours, 0))
                })
                .Where(p => p.WorkingTime != 0)
                .Average(p => p.WorkingTime);
        }

        public int? WorkingDaysCount(IGrouping<Guid, DialogueInfo> dialogues)
        {
            return dialogues.Select(p => p.BegTime.Date).Distinct().Count();
        }

        public double LoadPeriod(DateTime beg, DateTime end, List<DialogueInfo> dialogues, List<SessionInfo> sessions)
        {
            var loadHours = dialogues
                .Where(p => (p.BegTime <= beg && p.EndTime >= beg) ||
                            (p.BegTime >= beg && p.BegTime <= end))
                .Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours);

            var userCount = sessions
                .Where(p => (p.BegTime <= beg && p.EndTime >= beg) ||
                            (p.BegTime >= beg && p.BegTime <= end))
                .Select(p => p.ApplicationUserId).Distinct().Count();

            var totalLoadHours = userCount * end.Subtract(beg).TotalHours;

            var load = totalLoadHours != 0 ? Convert.ToDouble(loadHours) / Convert.ToDouble(totalLoadHours) : 0;
            return load;
        }

        public List<EfficiencyOptimizationHourInfo> LoadDaily(DateTime beg, List<DialogueInfo> dialogues, List<SessionInfo> sessions)
        {
            var curDate = beg.Date;
            var result = new List<EfficiencyOptimizationHourInfo>();
            for (int i = 0; i < 24; i++)
            {
                var begPeriod = curDate.AddHours(i);
                var endPeriod = curDate.AddHours(i + 1);
                result.Add(new EfficiencyOptimizationHourInfo
                {
                    Load = LoadPeriod(begPeriod, endPeriod, dialogues, sessions),
                    UsersCount = sessions
                        .Where(p => (p.BegTime <= begPeriod && p.EndTime >= begPeriod) ||
                                    (p.BegTime >= begPeriod && p.BegTime <= endPeriod))
                        .Select(p => p.ApplicationUserId).Distinct().Count()
                });
            }
            return result;
        }

        public double? PeriodIntersection(DateTime beg, DateTime end, TimeSpan timeBeg, TimeSpan timeEnd)
        {
            var begDate = beg.Date;
            var endDate = end.Date;

            double? period = 0;

            for (var i = begDate; i <= endDate; i = i.AddDays(1))
            {
                var dateTimeBeg = i.AddHours(timeBeg.TotalHours);
                var dateTimeEnd = i.AddHours(timeEnd.TotalHours);
                if (IsIntersect(dateTimeBeg, dateTimeEnd, beg, end))
                {
                    period += (Min(dateTimeEnd, end) - Max(dateTimeBeg, beg)).TotalHours;
                }
            }
            return period;
        }

        public EfficiencyLoadDialogueTimeSatisfactionInfo PeriodSatisfaction(DialogueInfo dialogue, TimeSpan timeBeg, TimeSpan timeEnd)
        {
            var result = new EfficiencyLoadDialogueTimeSatisfactionInfo();
            var begDate = dialogue.BegTime.Date;
            var endDate = dialogue.EndTime.Date;

            double? periodDialogue = 0;
            double? periodSession = 0;

            for (var i = begDate; i <= endDate; i = i.AddDays(1))
            {
                var dateTimeBeg = i.AddHours(timeBeg.TotalHours);
                var dateTimeEnd = i.AddHours(timeEnd.TotalHours);
                if (IsIntersect(dateTimeBeg, dateTimeEnd, dialogue.BegTime, dialogue.EndTime))
                {
                    periodDialogue += (Min(dateTimeEnd, dialogue.EndTime) - Max(dateTimeBeg, dialogue.BegTime)).TotalHours;
                    periodSession += (Min(dateTimeEnd, dialogue.SessionEndTime) - Max(dateTimeBeg, dialogue.SessionBegTime)).TotalHours;
                }
            }
            result.Weight = (periodSession != 0) ? periodDialogue / periodSession : null;
            result.SatisfactionScore = dialogue.SatisfactionScore;
            return result;
        }

        public bool IsIntersect(DateTime beg1, DateTime end1, DateTime beg2, DateTime end2)
        {
            if (beg1 <= end2 && beg1 >= beg2) return true;
            else if (end1 <= end2 && end1 >= beg2) return true;
            else if (beg1 < beg2 && end1 > end2) return true;
            else return false;
        }

        public bool TimeInPeriod(DateTime beg, DateTime end, TimeSpan time)
        {
            var begDate = beg.Date;
            var endDate = end.Date;
            var result = 0;
            for (var i = begDate; i <= endDate; i = i.AddDays(1))
            {
                if (i.AddHours(time.TotalHours) >= beg && i.AddHours(time.TotalHours) <= end) result += 1;
            }
            return result > 0;

        }

        public double? LoadInterval(List<DialogueInfo> dialogues, List<SessionInfo> sessions, TimeSpan beg, TimeSpan end)
        {
            double? dialoguesTime = 0;
            double? sessionsTime = 0;

            foreach (var dialogue in dialogues)
            {
                dialoguesTime += PeriodIntersection(dialogue.BegTime, dialogue.EndTime, beg, end);
            }
            foreach (var session in sessions)
            {
                sessionsTime += PeriodIntersection(session.BegTime, session.EndTime, beg, end);
            }
            return sessionsTime != 0 ? 100 * dialoguesTime / sessionsTime : 0;
        }

        public double? SatisfactionInterval(List<DialogueInfo> dialogues, TimeSpan beg, TimeSpan end)
        {
            var satisfactionInfo = new List<EfficiencyLoadDialogueTimeSatisfactionInfo>();
            foreach (var dialogue in dialogues)
            {
                satisfactionInfo.Add(PeriodSatisfaction(dialogue, beg, end));
            }
            return (satisfactionInfo.Sum(p => p.Weight) != 0 && satisfactionInfo.Any()) ? satisfactionInfo.Sum(p => p.SatisfactionScore * p.Weight) / satisfactionInfo.Sum(p => p.Weight) : 0;
        }

        public List<EfficiencyLoadEmployeeTimeInfo> EmployeeTimeCalculation(List<DialogueInfo> dialogues, List<SessionInfo> sessions)
        {
            var result = new List<EfficiencyLoadEmployeeTimeInfo>();

            for (var i = TimeSpan.Zero; i < TimeSpan.FromHours(24); i = i + TimeSpan.FromHours(1))
            {
                result.Add(new EfficiencyLoadEmployeeTimeInfo
                {
                    LoadIndex = LoadInterval(dialogues, sessions, i, i + TimeSpan.FromHours(1)),
                    SatisfactionIndex = SatisfactionInterval(dialogues, i, i + TimeSpan.FromHours(1)),
                    Time = i.ToString()
                });
            }
            return result;
        }

        public List<ReportFullDayInfo> Sum(List<ReportFullDayInfo> curRes, ReportFullDayInfo newInterval)
        {
            var intervals = curRes;
            try
            {
                newInterval.End = Min(newInterval.End, intervals.Max(p => p.End));
                newInterval.Beg = Max(newInterval.Beg, intervals.Min(p => p.Beg));

                foreach (var interval in intervals.Where(p => p.Beg >= newInterval.Beg && p.End <= newInterval.End))
                {
                    interval.ActivityType += 1;
                }

                // case inside
                var begInterval = intervals.Where(p => p.Beg < newInterval.Beg && p.End > newInterval.End);

                if (begInterval.Count() == 1)
                {

                    var end = begInterval.First().End;
                    var dialogueId = begInterval.First().DialogueId;
                    var type = begInterval.First().ActivityType;

                    begInterval.First().End = newInterval.Beg;

                    newInterval.ActivityType = type + newInterval.ActivityType;
                    if (dialogueId != null || newInterval.DialogueId != null)
                        newInterval.DialogueId = Guid.Parse(dialogueId.ToString() + newInterval.DialogueId.ToString());
                    intervals.Add(newInterval);

                    intervals.Add(new ReportFullDayInfo
                    {
                        Beg = newInterval.End,
                        End = end,
                        DialogueId = dialogueId,
                        ActivityType = type
                    });
                }
                else
                {
                    begInterval = intervals.Where(p => p.Beg < newInterval.Beg && p.End > newInterval.Beg);
                    if (begInterval.Count() == 1)
                    {
                        var end = begInterval.First().End;
                        var dialogueId = begInterval.First().DialogueId;
                        var type = begInterval.First().ActivityType;

                        begInterval.First().End = newInterval.Beg;

                        intervals.Add(new ReportFullDayInfo
                        {
                            Beg = newInterval.Beg,
                            End = end,
                            DialogueId = dialogueId != null || newInterval.DialogueId != null ?
                                    Guid.Parse(dialogueId.ToString() + newInterval.DialogueId.ToString())
                                    : dialogueId,
                            ActivityType = type + newInterval.ActivityType
                        });
                    }

                    var endInterval = intervals.Where(p => p.Beg < newInterval.End && p.End > newInterval.End);

                    if (endInterval.Count() == 1)
                    {
                        var end = endInterval.First().End;
                        var dialogueId = endInterval.First().DialogueId;
                        var type = endInterval.First().ActivityType;

                        var endIntervalNew = endInterval.First();
                        endIntervalNew.End = newInterval.End;
                        if (endIntervalNew.DialogueId != null || newInterval.DialogueId != null)
                            endIntervalNew.DialogueId = Guid.Parse(endIntervalNew.DialogueId.ToString() + newInterval.DialogueId.ToString());
                        endIntervalNew.ActivityType += newInterval.ActivityType;

                        intervals.Add(new ReportFullDayInfo
                        {
                            Beg = newInterval.End,
                            End = end,
                            DialogueId = dialogueId,
                            ActivityType = type
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("SUM Ex" + e.Message);
            }

            return intervals;
        }

        public List<ReportFullDayInfo> TimeTable(List<SessionInfo> sessions, List<DialogueInfo> dialogues, Guid applicationUserId, DateTime date)
        {
            var result = new List<ReportFullDayInfo>();

            result.Add(new ReportFullDayInfo
            {
                Beg = date.Date,
                End = date.Date.AddDays(1),
                ActivityType = 0,
                DialogueId = null
            });

            if (sessions.Count() != 0)
            {
                foreach (var session in sessions.Where(p => p.BegTime.Date == date && p.ApplicationUserId == applicationUserId))
                {
                    result = Sum(result, new ReportFullDayInfo
                    {
                        Beg = session.BegTime,
                        End = session.EndTime,
                        DialogueId = null,
                        ActivityType = 1
                    });
                }
            }

            if (dialogues.Count() != 0)
            {
                foreach (var dialogue in dialogues)
                {
                    result = Sum(result, new ReportFullDayInfo
                    {
                        Beg = dialogue.BegTime,
                        End = dialogue.EndTime,
                        DialogueId = dialogue.DialogueId,
                        ActivityType = 1
                    });
                }
            }

            result = result.OrderBy(p => p.Beg).ToList();
            foreach (var element in result.Where(p => p.ActivityType != 0 && p.ActivityType != 1 && p.ActivityType != 2))
            {
                element.ActivityType = 0;
            }

            foreach (var element in result.Where(p => p.DialogueId != null))
            {
                element.ActivityType = 2;
            }
            return result;
        }

        //------------------FOR CONTENT ANALYTIC------------------------


        public EmotionAttention SatisfactionDuringAdv(List<SlideShowInfo> sessions, List<DialogueInfoWithFrames> dialogues)
        {
            EmotionAttention result = new EmotionAttention();
            if (dialogues != null)
            {
                foreach (var session in sessions)
                {
                    List<DialogueFrame> frames = dialogues.Where(x => x.DialogueId == session.DialogueId).FirstOrDefault()?.DialogueFrame.ToList();
                    var beg = session.BegTime;
                    var end = session.EndTime;
                    frames = frames != null ? frames.Where(x => x.Time >= beg && x.Time <= end).ToList() : null;
                    if (frames != null && frames.Count() != 0)
                    {
                        result.Attention = frames.Average(x => Math.Abs((decimal)x.YawShare) <= 20 ? 100 : 20);
                        result.Positive = frames.Average(x => x.SurpriseShare) + frames.Average(x => x.HappinessShare);
                        result.Negative = frames.Average(x => x.DisgustShare) + frames.Average(x => x.FearShare) + frames.Average(x => x.SadnessShare) + frames.Average(x => x.ContemptShare);
                        result.Neutral = frames.Average(x => x.NeutralShare);
                        return result;
                    }
                }
            }
            return null;
        }

        public EmotionAttention SatisfactionDuringAdv(List<SlideShowInfo> sessions, Dialogue dialogue)
        {
            EmotionAttention result = new EmotionAttention();
            if (dialogue != null)
            {
                foreach (var session in sessions)
                {
                    List<DialogueFrame> frames = dialogue.DialogueFrame.ToList();
                    var beg = session.BegTime;
                    var end = session.EndTime;
                    frames = frames != null ? frames.Where(x => x.Time >= beg && x.Time <= end).ToList() : null;
                    if (frames != null && frames.Count() != 0)
                    {
                        result.Attention = frames.Average(x => Math.Abs((decimal)x.YawShare) <= 20 ? 100 : 20);
                        result.Positive = frames.Average(x => x.SurpriseShare) + frames.Average(x => x.HappinessShare);
                        result.Negative = frames.Average(x => x.DisgustShare) + frames.Average(x => x.FearShare) + frames.Average(x => x.SadnessShare) + frames.Average(x => x.ContemptShare);
                        result.Neutral = frames.Average(x => x.NeutralShare);
                        return result;
                    }
                }
            }
            return null;
        }

        public EmotionAttention SatisfactionDuringAdv(SlideShowSession session, Dialogue dialogue)
        {
            EmotionAttention result = new EmotionAttention();
            if (dialogue != null)
            {
                List<DialogueFrame> frames = dialogue.DialogueFrame.ToList();
                var beg = session.BegTime;
                var end = session.EndTime;
                frames = frames != null ? frames.Where(x => x.Time >= beg && x.Time <= end).ToList() : null;
                if (frames != null && frames.Count() != 0)
                {
                    result.Attention = frames.Average(x => Math.Abs((decimal)x.YawShare) <= 20 ? 100 : 20);
                    result.Positive = frames.Average(x => x.SurpriseShare) + frames.Average(x => x.HappinessShare);
                    result.Negative = frames.Average(x => x.DisgustShare) + frames.Average(x => x.FearShare) + frames.Average(x => x.SadnessShare) + frames.Average(x => x.ContemptShare);
                    result.Neutral = frames.Average(x => x.NeutralShare);
                    return result;
                }
            }
            return null;
        }     
    }
}