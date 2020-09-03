using System;
using System.Collections.Generic;
using System.Linq;
using UserOperations.Models.AnalyticModels;
using UserOperations.Utils.Interfaces;

namespace UserOperations.Utils.AnalyticReportUtils
{
    public class AnalyticReportUtils : IAnalyticReportUtils
    {
        public List<ReportFullDayInfo> TimeTable(List<SessionInfo> sessions, List<DialogueInfo> dialogues, Guid? applicationUserId, DateTime date)
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
        public double? SessionAverageHours(List<SessionInfo> sessions, Guid? applicationUserId, DateTime? date, DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            var sessionsUser = sessions.Where(p => p.ApplicationUserId == applicationUserId && p.BegTime.Date == date);
            return sessionsUser.Any() ? (double?)sessionsUser.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : null;
        }
        public double? DialogueSumDuration(List<DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            return dialogues.Any() ? (double?)dialogues.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : null;
        }
        public double? LoadIndex(List<SessionInfo> sessions, List<DialogueInfo> dialogues,
            Guid? applicationUserId, DateTime date, DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            var sessionsUser = sessions.Where(p => p.ApplicationUserId == applicationUserId && p.BegTime.Date == date);
            var sessionHours = sessionsUser.Count() != 0 ? Convert.ToDouble(sessionsUser.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours)) : 0;
            var dialoguesHours = dialogues.Count() != 0 ? Convert.ToDouble(dialogues.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours)) : 0;
            return 100 * LoadIndex(sessionHours, dialoguesHours);
        }
        public double? LoadIndex(double? workinHours, double? dialogueHours)
        {
            workinHours = MaxDouble(workinHours, dialogueHours);
            return workinHours != 0 ? (double?)dialogueHours / workinHours : 0;
        }
        public double? MaxDouble(double? x, double? y)
        {
            return x > y ? x : y;
        }
        public int DialoguesCount(List<DialogueInfo> dialogues, Guid? applicationUserId = null, DateTime? date = null)
        {
            return dialogues.Any() ? dialogues
                .Where(p => (applicationUserId == null || p.ApplicationUserId == applicationUserId) &&
                    (date == null || p.BegTime.Date == date))
                .Select(p => p.DialogueId).Distinct().Count() : 0;
        }
        public double? DialogueSumDuration(IGrouping<DateTime, SessionInfo> sessions, List<DialogueInfo> dialogues, Guid? applicationUserId)
        {
            DateTime beg = sessions.Key;
            DateTime end = sessions.Key.AddDays(1);
            var dialoguesUser = dialogues.Where(p => p.ApplicationUserId == applicationUserId && (p.BegTime.Date == beg || p.EndTime.Date == sessions.Key));
            var dialoguesHours = dialoguesUser.Count() != 0 ? (double?)Convert.ToDouble(dialoguesUser.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours)) : null;
            return dialoguesHours ?? 0;
        }
        public double? SessionAverageHours(IGrouping<DateTime, SessionInfo> sessions)
        {
            DateTime beg = sessions.Key;
            DateTime end = sessions.Key.AddDays(1);
            var sessionHours = sessions.Any() ? (double?)Convert.ToDouble(sessions.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours)) : null;
            return sessionHours;
        }
        public double? LoadIndex(IGrouping<Guid?, SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            var sessionHours = sessions.Count() != 0 ? sessions.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
            var dialoguesHours = dialogues.Count() != 0 ? dialogues.Where(p => p.ApplicationUserId == sessions.Key).Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
            return 100 * LoadIndex(sessionHours, dialoguesHours);
        }
    }
}