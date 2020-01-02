using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using UserOperations.Models.AnalyticModels;
using UserOperations.Models.Get.AnalyticRatingController;

namespace UserOperations.Utils.AnalyticRatingUtils
{
    public class AnalyticRatingUtils
    {
        private readonly IConfiguration _config;

        public AnalyticRatingUtils(IConfiguration config)
        {
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

        public double? LoadIndex(double? workinHours, double? dialogueHours)
        {
            workinHours = MaxDouble(workinHours, dialogueHours);
            return workinHours != 0 ? (double?)dialogueHours / workinHours : 0;
        }
        public double? LoadIndex(List<SessionInfo> sessions, IGrouping<Guid?, DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            var sessionsGroup = sessions.Where(p => p.ApplicationUserId == dialogues.Key);
            var sessionHours = sessionsGroup.Any() ? sessionsGroup.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
            var dialoguesHours = dialogues.Any() ? dialogues.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : 0;
            return 100 * LoadIndex(sessionHours, dialoguesHours);
        }
        public double? LoadIndex(List<SessionInfo> sessions, IGrouping<DateTime, DialogueInfo> dialogues,
            Guid? applicationUserId, DateTime? date, DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            var sessionsUser = sessions.Where(p => p.ApplicationUserId == applicationUserId && p.BegTime.Date == date);
            var sessionHours = sessionsUser.Any() ? Convert.ToDouble(sessionsUser.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours)) : 0;
            var dialoguesHours = dialogues.Any() ? Convert.ToDouble(dialogues.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours)) : 0;
            return 100 * LoadIndex(sessionHours, dialoguesHours);
        }

        public double? MaxDouble(double? x, double? y)
        {
            return x > y ? x : y;
        }
        public double? SessionAverageHours(List<SessionInfo> sessions, DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            return sessions.Any() ?
                (double?)sessions.GroupBy(p => p.BegTime.Date)
                        .Select(q => 
                            q.Sum(r => Min(r.EndTime, end).Subtract(Max(r.BegTime, beg)).TotalHours) / q.Select(r => r.ApplicationUserId).Distinct().Count()
                        ).Average() : null;
        }
        public double? SessionAverageHours(List<SessionInfo> sessions, Guid? applicationUserId, DateTime? date, DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            var sessionsUser = sessions.Where(p => p.ApplicationUserId == applicationUserId && p.BegTime.Date == date);
            return sessionsUser.Any() ? (double?)sessionsUser.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : null;
        }

       
     
        public double? DialogueAverageDuration(IGrouping<DateTime, DialogueInfo> dialogues, DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            return dialogues.Any() ? (double?)dialogues.Average(r => Min(r.EndTime, end).Subtract(Max(r.BegTime, beg)).TotalHours) : null;
        }
        public double? DialogueAverageDuration(IGrouping<Guid?, DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            return dialogues.Average(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours);
        }
        public double? DialogueSumDuration(IGrouping<DateTime, DialogueInfo> dialogues, DateTime beg = default(DateTime), DateTime end = default(DateTime))
        {
            return dialogues.Any() ? (double?)dialogues.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours) : null;
        }
       
        public double? SatisfactionIndex(IGrouping<Guid?, DialogueInfo> dialogues)
        {
            return dialogues.Any() ? dialogues.Where(p => p.SatisfactionScore != null && p.SatisfactionScore != 0).Average(p => p.SatisfactionScore) : null;
        }
        public double? CrossIndex(IGrouping<Guid?, DialogueInfo> dialogues)
        {
            var dialoguesCount = dialogues.Any() ? dialogues.Select(p => p.DialogueId).Distinct().Count() : 0;
            var crossDialoguesCount = dialogues.Any() ? dialogues.Sum(p => Math.Min(p.CrossCount, 1)) : 0;
            return dialoguesCount != 0 ? 100 * Convert.ToDouble(crossDialoguesCount) / Convert.ToDouble(dialoguesCount) : 0;
        }
     
        public double? DialogueAveragePause(List<SessionInfo> sessions, IGrouping<Guid?, DialogueInfo> dialogues, DateTime beg, DateTime end)
        {
            var sessionHours = sessions.Where(p => p.CompanyId == dialogues.Key).Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours);
            var dialoguesHours = dialogues.Sum(p => Min(p.EndTime, end).Subtract(Max(p.BegTime, beg)).TotalHours);
            return (sessionHours - dialoguesHours) / dialogues.Select(p => p.DialogueId).Distinct().Count();
        }
   
    }
}