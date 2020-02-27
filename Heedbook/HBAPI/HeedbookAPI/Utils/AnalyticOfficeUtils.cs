using System;
using System.Collections.Generic;
using System.Linq;
using HBData.Models;
using Microsoft.Extensions.Configuration;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Utils.AnalyticOfficeUtils
{
    public class AnalyticOfficeUtils
    {
        public Employee BestEmployeeLoad(List<DialogueInfo> dialogues, List<SessionInfo> sessions, DateTime beg, DateTime end)
        {
            return dialogues.Any() ? dialogues
                .Where(p => p.ApplicationUserId != null)
                .GroupBy(p => (Guid)p.ApplicationUserId)
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
        public double? LoadIndex(List<SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg , DateTime end )
        {           
            var sessionHours = sessions.Any() ? sessions.Sum(p =>
                Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;

            var dialoguesHours = dialogues.Any() ? dialogues.Sum(p =>
                Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
            return 100 * LoadIndex( sessionHours, dialoguesHours);
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
        public double? LoadIndex(double? workinHours, double? dialogueHours)
        {
            workinHours = MaxDouble(workinHours, dialogueHours);
            return workinHours != 0 ? (double?)dialogueHours / workinHours : 0;
        }
        public double? MaxDouble(double? x, double? y)
        {
            return x > y ? x : y;
        }
        public double? LoadIndex(List<SessionInfo> sessions, IGrouping<Guid, DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            var sessionsGroup = sessions.Where(p => p.ApplicationUserId == dialogues.Key);
            var sessionHours = sessionsGroup.Any() ? sessionsGroup.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
            var dialoguesHours = dialogues.Any() ? dialogues.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
            return 100 * LoadIndex( sessionHours, dialoguesHours);
        }
        public int DialoguesCount(List<DialogueInfo> dialogues, Guid? applicationUserId = null, DateTime? date = null)
        {
            return dialogues.Any() ? dialogues
                .Where(p => (applicationUserId == null || p.ApplicationUserId == applicationUserId) &&
                    (date == null || p.BegTime.Date == date))
                .Select(p => p.DialogueId).Distinct().Count() : 0;
        }
        public double? SessionAverageHours(List<SessionInfo> sessions, DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            return sessions.Any() ?
                (double?)sessions.GroupBy(p => p.BegTime.Date)
                        .Select(q => 
                            q.Sum(r => Min(r.EndTime, end).Subtract(Max(r.BegTime, beg)).TotalHours) / q.Select(r => r.ApplicationUserId).Distinct().Count()
                        ).Average() : null;
        }
        public double? DialogueAverageDuration(List<DialogueInfo> dialogues, DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            return dialogues.Any() ? dialogues.Average(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
        }
        public double? DialogueTotalDuration(List<DialogueInfo> dialogues, DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            return dialogues.Any() ? dialogues.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
        }

        public bool CheckIfDialogueInWorkingTime(Dialogue  dialogue, WorkingTime [] times)
        {
            var day = times[(int)dialogue.BegTime.DayOfWeek];
            if (day.BegTime == null || day.EndTime == null) return false;
            return dialogue.BegTime.TimeOfDay > ((DateTime)day.BegTime).TimeOfDay && dialogue.EndTime.TimeOfDay < ((DateTime)day.EndTime).TimeOfDay;
        }
        public double? SatisfactionIndex(List<DialogueInfo> dialogues)
        {
            return dialogues.Any() ? dialogues.Where(p => p.SatisfactionScore != null && p.SatisfactionScore != 0).Average(p => p.SatisfactionScore) : null;
        }
        public int EmployeeCount(List<DialogueInfo> dialogues)
        {
            return dialogues.Any() ? dialogues.Where(p => p.ApplicationUserId != null).Select(p => p.ApplicationUserId).Distinct().Count() : 0;
        }

        public int DeviceCount(List<DialogueInfo> dialogues)
        {
            return dialogues.Any() ? dialogues.Select(p => p.DeviceId).Distinct().Count() : 0;
        }
        //public double? DialogueAveragePause(List<SessionInfo> sessions, List<DialogueInfo> dialogues, DateTime beg, DateTime end)
        //{
        //    var sessionHours = sessions.Any() ? sessions.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
        //    var dialoguesHours = dialogues.Any() ? dialogues.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
        //    return dialogues.Any() ? (double?)(sessionHours - dialoguesHours) / dialogues.Select(p => p.DialogueId).Distinct().Count() : null;
        //}
        public List<double> DialogueAvgPauseListInMinutes(WorkingTime [] timeTable, List<DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            List<double> pauses = new List<double>();
            if (!timeTable.Any() || !dialogues.Any()) return null;

            foreach (var dialogue in dialogues.GroupBy(x => x.DeviceId))
            {
                for (var i = beg.Date; i < end.Date; i = i.AddDays(1))
                {
                    try
                    {
                        var endDay = i.AddDays(1);
                        var workingHours = timeTable.Where(x => x.CompanyId == dialogue.First().CompanyId).ToArray()[(int)i.DayOfWeek];
                        if (workingHours.BegTime == null) continue;


                        var dialogInDay = dialogue
                              .Where(p => p.BegTime >= i && p.EndTime <= endDay)
                              .OrderBy(p => p.BegTime).ToArray();

                        List<DateTime> times = new List<DateTime>();
                        var timeStartWorkingDay = i.AddHours(((DateTime)workingHours.BegTime).Hour).AddMinutes(((DateTime)workingHours.BegTime).Minute);
                        var timeEndWorkingDay = i.AddHours(((DateTime)workingHours.EndTime).Hour).AddMinutes(((DateTime)workingHours.EndTime).Minute);
                        if (!dialogInDay.Any())
                        {
                            pauses.Add(timeEndWorkingDay.Subtract(timeStartWorkingDay).TotalMinutes);
                            continue;
                        }


                        times.Add(timeStartWorkingDay);
                        for (var j = 0; j < dialogInDay.Count(); j++)
                        {
                            if (j == 0 || dialogInDay[j].BegTime >= dialogInDay[j - 1].EndTime)
                            {
                                times.Add(dialogInDay[j].BegTime);
                                times.Add(dialogInDay[j].EndTime);
                            }
                        }
                        times.Add(timeEndWorkingDay);

                        for (int j = 0; j < times.Count() - 1; j += 2)
                        {
                            var pause = (times[j + 1].Subtract(times[j])).TotalMinutes;
                            pauses.Add(pause < 0 ? 0 : pause);
                        }
                    }
                    catch { }
                }
            }



            //foreach ( var sessionGrouping in sessions.GroupBy(x => x.ApplicationUserId))
            //{
            //foreach( var ses in sessionGrouping.OrderBy(p => p.BegTime))
            //{
                //var dialogInSession = dialogues
                //        .Where(p => 
                //        p.ApplicationUserId == ses.ApplicationUserId
                //        && p.BegTime >= ses.BegTime
                //        && p.EndTime <= ses.EndTime)
                //        .OrderBy(p => p.BegTime)
                //        .ToArray();
                //List<DateTime> times = new List<DateTime>();
                //    times.Add(ses.BegTime);
                //    for(var i = 0; i < dialogInSession.Count(); i++)
                //    {
                //        if (i == 0 || dialogInSession[i].BegTime >= dialogInSession[i - 1].EndTime)
                //        {
                //            times.Add(dialogInSession[i].BegTime);
                //            times.Add(dialogInSession[i].EndTime);
                //        }
                //    }
                //    times.Add(ses.EndTime);

                //    for (int i = 0; i< times.Count()-1; i+=2)
                //    {
                //        var pause = (times[i + 1].Subtract(times[i])).TotalMinutes ;
                //       // pauseTotalTest2 += pause;
                //        pauses.Add(pause < 0? 0 : pause);
                //    }

                   // pauseTotalTest += Min(ses.EndTime, end).Subtract(Max(ses.BegTime, beg)).TotalMinutes - dialogInSession.Sum(x => Min(x.EndTime, end).Subtract(x.BegTime).TotalMinutes);
            //        counter += dialogInSession.Count();
            //    }
            //}

            //---TODO: there are some mistakes in sessions and dialogues:
            //---1) some dialogues dont belong to any session
            //---2) some dialogues have the same time (or one dialogue begin earler than another dialogue ends - so pause is minus)
            //---so I make some corections into pauses - to have the same LOAD index in result

           
           
            return pauses;
        }
        public double? SessionTotalHours(List<SessionInfo> sessions, DateTime beg, DateTime end)
        {
            return sessions.Any() ?
                (double?)sessions.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
        }

    }
}