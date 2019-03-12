using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using UserOperations.Data;
using UserOperations.Repository;
using UserOperations.Models.AnalyticModels;

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

        public DateTime MaxTime(DateTime time1, DateTime time2)
        {
            return time1 > time2 ? time1 : time2;
        }
        public DateTime MinTime(DateTime time1, DateTime time2)
        {
            return time1 > time2 ? time2 : time1;
        }
        public double? SignedPower(double x, double power)
        {
            return (x != 0) ? Math.Sign(x) * Math.Pow(Math.Abs(x), power): 0;
        }


        public double? SatisfactionDialogueDelta(List<DialogueInfo> dialogues)
        {
            var delta = dialogues.Count() != 0 ? SignedPower(dialogues.Average(p => 
                Math.Pow(Convert.ToDouble(p.SatisfactionScoreEnd - p.SatisfactionScoreBeg + 1.0/3.0), 3)), 1.0/3.0) : 0;
            return delta;
        }

        public double? LoadIndex(List<SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            var sessionHours = sessions.Count() != 0 ? sessions.Sum(p => 
                MinTime(p.EndTime, end).Subtract(MaxTime(p.BegTime, beg)).TotalHours) : 0;
            var dialoguesHours = dialogues.Count() != 0 ? dialogues.Sum(p => 
                MinTime(p.EndTime, end).Subtract(MaxTime(p.BegTime, beg)).TotalHours) : 0;
            return sessionHours != 0 ? (double?) 100 * dialoguesHours / sessionHours : null;
        }

        public double? LoadIndex(List<SessionInfo> sessions, IGrouping<Guid, DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            var sessionsGroup = sessions.Where(p => p.ApplicationUserId == dialogues.Key);
            var sessionHours = sessionsGroup.Count() != 0 ? sessionsGroup.Sum(p => MinTime(p.EndTime, end).Subtract(MaxTime(p.BegTime, beg)).TotalHours) : 0;
            var dialoguesHours = dialogues.Count() != 0 ? dialogues.Sum(p => MinTime(p.EndTime, end).Subtract(MaxTime(p.BegTime, beg)).TotalHours) : 0;
            return sessionHours != 0 ? (double?)100 * dialoguesHours / sessionHours : null;
        }

        public double? LoadIndex(List<SessionInfo> sessions, IGrouping<DateTime, DialogueInfo> dialogues,
            Guid applicationUserId, DateTime? date, DateTime beg, DateTime end)
        {
            var sessionsUser = sessions.Where(p => p.ApplicationUserId == applicationUserId && p.BegTime.Date == date);
            var sessionHours = sessionsUser.Count() != 0 ? Convert.ToDouble(sessionsUser.Sum(p => MinTime(p.EndTime, end).Subtract(MaxTime(p.BegTime, beg)).TotalHours)) : 0;
            var dialoguesHours = dialogues.Count() != 0 ? Convert.ToDouble(dialogues.Sum(p => MinTime(p.EndTime, end).Subtract(MaxTime(p.BegTime, beg)).TotalHours)) : 0;
            return sessionHours != 0 ? (double?)dialoguesHours / sessionHours : null;
        }

        //Satisfaction index calculation
        public double? SatisfactionIndex(List<DialogueInfo> dialogues)
        {
            return dialogues.Count() != 0 ? dialogues.Where(p => p.SatisfactionScore != null && p.SatisfactionScore != 0).Average(p => p.SatisfactionScore) : null;
        }

        public double? SatisfactionIndex(IGrouping<Guid, DialogueInfo> dialogues)
        {
            return dialogues.Count() != 0 ? dialogues.Where(p => p.SatisfactionScore != null && p.SatisfactionScore != 0).Average(p => p.SatisfactionScore) : null;
        }

        // Cross index calculation
        public double? CrossIndex(List<DialogueInfo> dialogues)
        {
            var dialoguesCount = dialogues.Count() != 0 ? dialogues.Select(p => p.DialogueId).Distinct().Count() : 0;
            var crossDialoguesCount = dialogues.Count() != 0 ? dialogues.Sum(p => Math.Min(p.CrossCout, 1)) : 0;
            return  dialoguesCount != 0 ? 100 * Convert.ToDouble(crossDialoguesCount) / Convert.ToDouble(dialoguesCount) : 0;
        }

        public double? CrossIndex(IGrouping<Guid, DialogueInfo> dialogues)
        {
            var dialoguesCount = dialogues.Count() != 0 ? dialogues.Select(p => p.DialogueId).Distinct().Count() : 0;
            var crossDialoguesCount = dialogues.Count() != 0 ? dialogues.Sum(p => Math.Min(p.CrossCout, 1)) : 0;
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
            return normCoeff != 0 ? result / normCoeff: null;
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


        public int EmployeeCount(List<DialogueInfo> dialogues)
        {
            return dialogues.Count() != 0 ? dialogues.Select(p => p.ApplicationUserId).Distinct().Count() : 0;
        }

        public int EmployeeCount(List<EfficiencyOptimizationHourInfo> info, double maxLoad, double maxPercent, double quantile = 0.95)
        {
            var percent = (info.Count() > 0) ? info.Where(p => p.Load > maxLoad).Count() / info.Count() : 0;
            if (percent > maxPercent)
            {
                return Convert.ToInt32(Math.Ceiling(info.Where(p => p.Load > maxPercent).Average(p => p.UsersCount) + 1));
            }
            else
            {
                if (info.Count() > 0)
                {
                    var index = Math.Max(Convert.ToInt32(Math.Round((Convert.ToDouble(info.Count()) * quantile))) - 1, 0);
                    var res = info.Select(p => new { userCount = (p.Load > maxLoad) ? (p.UsersCount + 1) : 
                        Convert.ToInt32(Math.Ceiling(p.Load / maxLoad * p.UsersCount)) });
                    return res.OrderBy(p => p.userCount).Select(p => p.userCount).ToList()[index];
                }
                else
                {
                    return 0;
                }
            }
        }

        public int DialoguesCount(List<DialogueInfo> dialogues)
        {
            return dialogues.Count() != 0 ? dialogues.Select(p => p.DialogueId).Distinct().Count() : 0;
        }

        public string BestEmployee(List<DialogueInfo> dialogues, List<SessionInfo> sessions, DateTime beg, DateTime end)
        {
            return dialogues.Count() != 0 ? dialogues
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

        public double? BestEmployeeEfficiency(List<DialogueInfo> dialogues, List<SessionInfo> sessions, DateTime beg, DateTime end)
        {
            return dialogues.Count() != 0 ? dialogues
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

        public string BestProgressiveEmployee(List<DialogueInfo> dialogues, DateTime beg)
        {
            return dialogues.Count() != 0 ? dialogues
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
            return dialogues.Count() != 0 ? dialogues
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

        public double? SessionAverageHours(List<SessionInfo> sessions, DateTime beg, DateTime end)
        {
            return sessions.Count() != 0 ? 
                (double?) sessions.GroupBy(p => p.BegTime.Date).Select(q => q.Sum(p => MinTime(p.EndTime, end).Subtract(MaxTime(p.BegTime, beg)).TotalHours) / q.Select(p => p.ApplicationUserId).Distinct().Count()).Average(): null;
        }

        public double? SessionAverageHours(List<SessionInfo> sessions, Guid applicationUserId, DateTime? date, DateTime beg, DateTime end)
        {
            var sessionsUser = sessions.Where(p => p.ApplicationUserId == applicationUserId && p.BegTime.Date == date);
            return sessionsUser.Count() != 0 ? (double?)sessionsUser.Sum(p => MinTime(p.EndTime, end).Subtract(MaxTime(p.BegTime, beg)).TotalHours) : null;
        }

        public double? DialogueAverageHoursDaily(IGrouping<Guid, DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            return dialogues.Sum(p => MinTime(p.EndTime, end).Subtract(MaxTime(p.BegTime, beg)).TotalHours) / dialogues.Select(p => MaxTime(p.BegTime, beg).Date).Distinct().Count();
        }

        public double? DialogueAverageHoursDaily(IGrouping<DateTime, DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            return dialogues.Count() != 0 ? (double?)dialogues.Sum(p => MinTime(p.EndTime, end).Subtract(MaxTime(p.BegTime, beg)).TotalHours) : null;
        }

        public double? DialogueAveragePause(List<SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            var sessionHours = sessions.Count() != 0 ? sessions.Sum(p => MinTime(p.EndTime, end).Subtract(MaxTime(p.BegTime, beg)).TotalHours): 0;
            var dialoguesHours = dialogues.Count() != 0 ? dialogues.Sum(p => MinTime(p.EndTime, end).Subtract(MaxTime(p.BegTime, beg)).TotalHours) : 0;
            return dialogues.Count != 0 ? (double?)(sessionHours - dialoguesHours) / dialogues.Select(p => p.DialogueId).Distinct().Count(): null;
        }
        public double? DialogueAveragePause(List<SessionInfo> sessions, IGrouping<Guid, DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            var sessionHours = sessions.Where(p => p.ApplicationUserId == dialogues.Key).Sum(p => MinTime(p.EndTime, end).Subtract(MaxTime(p.BegTime, beg)).TotalHours);
            var dialoguesHours = dialogues.Sum(p => MinTime(p.EndTime, end).Subtract(MaxTime(p.BegTime, beg)).TotalHours);
            return (sessionHours - dialoguesHours) / dialogues.Select(p => p.DialogueId).Distinct().Count();
        }

        public double? DialogueAverageDuration(List<DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            var dialoguesHours = dialogues.Count() !=0 ? dialogues.Sum(p => MinTime(p.EndTime, end).Subtract(MaxTime(p.BegTime, beg)).TotalHours): 0;
            return dialogues.Count() != 0 ? dialoguesHours / dialogues.Select(p => p.DialogueId).Distinct().Count() : 0;
        }
        public double? DialogueAverageDuration(IGrouping<Guid, DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            return dialogues.Sum(p => MinTime(p.EndTime, end).Subtract(MaxTime(p.BegTime, beg)).TotalHours) / dialogues.Select(p => p.DialogueId).Distinct().Count();
        }
        public double? DialogueAverageDuration(IGrouping<DateTime, DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            return dialogues.Count() != 0 ? (double?) dialogues.Average(r => MinTime(r.EndTime, end).Subtract(MaxTime(r.BegTime, beg)).TotalHours) : null;
        }

        public double? DialogueAverageDurationDaily(IGrouping<Guid, DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            return dialogues
                .GroupBy(p => p.BegTime.Date)
                .Select(p => new
                {
                    WorkingTime = p.Sum(q => Math.Max(MinTime(q.EndTime, end).Subtract(MaxTime(q.BegTime, beg)).TotalHours, 0))
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
                .Sum(p => MinTime(p.EndTime, end).Subtract(MaxTime(p.BegTime, beg)).TotalHours);

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
                    period += (MinTime(dateTimeEnd, end) - MaxTime(dateTimeBeg, beg)).TotalHours;
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
                    periodDialogue += (MinTime(dateTimeEnd, dialogue.EndTime) - MaxTime(dateTimeBeg, dialogue.BegTime)).TotalHours;
                    periodSession += (MinTime(dateTimeEnd, dialogue.SessionEndTime) - MaxTime(dateTimeBeg, dialogue.SessionBegTime)).TotalHours;
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
            return (satisfactionInfo.Sum(p => p.Weight) != 0 && satisfactionInfo.Count() != 0) ? satisfactionInfo.Sum(p => p.SatisfactionScore * p.Weight) / satisfactionInfo.Sum(p => p.Weight) : 0;
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


    }
}