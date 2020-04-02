using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HBData.Models;

namespace UserOperations.Utils.AnalyticWeeklyReportController
{
    public class AnalyticWeeklyReportUtils
    {
        public double? TotalAvg(List<VWeeklyUserReport> dialogues, string property)
        {
            if (dialogues == null || dialogues.Count() == 0) return 0;
            Type dialogueType = dialogues.First().GetType();
            PropertyInfo prop = dialogueType.GetProperty(property);
            return dialogues.Sum(p => (double?)prop.GetValue(p)) / dialogues.Count();
        }
        public Dictionary<DateTime, double> AvgPerDay(List<VWeeklyUserReport> dialogues, string property, DateTime reportBegTime)
        {
            DateTime begTime = reportBegTime.AddDays(-6);
            DateTime endTime = reportBegTime;
            Type dialogueType = null;
            PropertyInfo prop = null;
            if (dialogues != null && dialogues.Count() != 0)
            {
                dialogueType = dialogues.First().GetType();
                prop = dialogueType.GetProperty(property);
            }
            Dictionary<DateTime, double> avgPerDay = new Dictionary<DateTime, double>();
            for (var day = begTime; day <= endTime; day = day.AddDays(1))
            {
                var dialogue = dialogues.Where(x => x.Day.Date == day.Date).FirstOrDefault();
                avgPerDay.Add(day.Date, dialogue != null ? ((double?)(prop.GetValue(dialogue)) ?? 0) : 0);
            }
            return avgPerDay;
        }
        public int? OfficeRatingSatisfactionPlace(List<VWeeklyUserReport> dialogues, Guid userId)
        {
            if (dialogues == null || dialogues.Count() == 0) return 0;
            var OrderedBySatisf = dialogues
                        .GroupBy(p => p.AspNetUserId)
                        .Select(p => new { satisf = p.Average(x => x.Satisfaction), p.Key })
                        .OrderByDescending(s => s.satisf)
                        .Select((s, i) => new { place = i, userId = s.Key });
            return OrderedBySatisf.Where(p => p.userId == userId).FirstOrDefault()?.place ?? -1;
        }
        public int? OfficeRatingPositiveEmotPlace(List<VWeeklyUserReport> dialogues, Guid userId)
        {
            if (dialogues == null || dialogues.Count() == 0) return 0;
            var OrderedByPositive = dialogues
                       .GroupBy(p => p.AspNetUserId)
                       .Select(p => new { positive = p.Average(x => x.PositiveEmotions), p.Key })
                       .OrderByDescending(s => s.positive)
                       .Select((s, i) => new { place = i, userId = s.Key });
            return OrderedByPositive.Where(p => p.userId == userId).FirstOrDefault()?.place ?? -1;
        }
        public int? OfficeRatingPositiveIntonationPlace(List<VWeeklyUserReport> dialogues, Guid userId)
        {
            if (dialogues == null || dialogues.Count() == 0) return 0;
            var OrderedByPositive = dialogues
                       .GroupBy(p => p.AspNetUserId)
                       .Select(p => new { positive = p.Average(x => x.PositiveTone), p.Key })
                       .OrderByDescending(s => s.positive)
                       .Select((s, i) => new { place = i, userId = s.Key });
            return OrderedByPositive.Where(p => p.userId == userId).FirstOrDefault()?.place ?? -1;
        }
        public int? OfficeRatingSpeechEmotPlace(List<VWeeklyUserReport> dialogues, Guid userId)
        {
            if (dialogues == null || dialogues.Count() == 0) return 0;
            var OrderedByPositive = dialogues
                       .GroupBy(p => p.AspNetUserId)
                       .Select(p => new { positive = p.Average(x => (double?)x.SpeekEmotions), p.Key })
                       .OrderByDescending(s => s.positive)
                       .Select((s, i) => new { place = i, userId = s.Key });
            return OrderedByPositive.Where(p => p.userId == userId).FirstOrDefault()?.place ?? -1;
        }
        public Dictionary<DateTime, double> AvgNumberOfDialoguesPerDay(List<VWeeklyUserReport> dialogues, DateTime reportBegTime)
        {
            DateTime begTime = reportBegTime.AddDays(-6);
            DateTime endTime = reportBegTime;
            Dictionary<DateTime, double> result = new Dictionary<DateTime, double>();
            for (var day = begTime; day <= endTime; day = day.AddDays(1))
            {
                result.Add(day.Date, dialogues
                     .FirstOrDefault(x => x.Day.Date == day.Date) != null ?
                     (double)dialogues.FirstOrDefault(x => x.Day.Date == day.Date).Dialogues
                     : 0);
            }
            return result;
        }
        public int? OfficeRatingDialoguesAmount(List<VWeeklyUserReport> dialogues, Guid userId)
        {
            var ordered = dialogues?.GroupBy(p => p.AspNetUserId)
                .OrderByDescending(x => x.Sum(r => r.Dialogues)).Select((x, i) => new { Place = i, UserId = x.Key });
            return ordered?.Where(x => x.UserId == userId).FirstOrDefault()?.Place ?? -1;
        }
        public Dictionary<DateTime, double> AvgWorkingHoursPerDay(List<VSessionUserWeeklyReport> sessions, DateTime reportBegTime)
        {
            DateTime begTime = reportBegTime.AddDays(-6);
            DateTime endTime = reportBegTime;
            Dictionary<DateTime, double> result = new Dictionary<DateTime, double>();
            for (var day = begTime; day <= endTime; day = day.AddDays(1))
            {
                result.Add(day.Date, sessions
                     .FirstOrDefault(x => x.Day.Date == day.Date) != null ?
                     (double)sessions.FirstOrDefault(x => x.Day.Date == day.Date).SessionsHours
                     : 0);
            }
            return result;
        }
        public int? OfficeRatingWorkingHours(List<VSessionUserWeeklyReport> sessions, Guid userId)
        {
            var ordered = sessions?.GroupBy(p => p.AspNetUserId)
                .OrderByDescending(x => x.Sum(r => r.SessionsHours)).Select((x, i) => new { Place = i, UserId = x.Key });
            return ordered?.Where(x => x.UserId == userId).FirstOrDefault()?.Place ?? -1;
        }
        public double AvgDialogueTimeTotal(List<VWeeklyUserReport> dialogues)
        {
            if (dialogues == null || dialogues.Count() == 0) return 0;
            return dialogues.Sum(p => p.DialogueHours) / dialogues.Sum(p => p.Dialogues) ?? 0;
        }
        public Dictionary<DateTime, double> AvgDialogueTimePerDay(List<VWeeklyUserReport> dialogues, DateTime reportBegTime)
        {
            DateTime begTime = reportBegTime.AddDays(-6);
            DateTime endTime = reportBegTime;
            Dictionary<DateTime, double> result = new Dictionary<DateTime, double>();
            for (var day = begTime; day <= endTime; day = day.AddDays(1))
            {
                var dialogue = dialogues.FirstOrDefault(x => x.Day.Date == day.Date);
                result.Add(day.Date, dialogue != null ?
                    (double)dialogue.DialogueHours / dialogue.Dialogues : 0);
            }
            return result;
        }
        public int? OfficeRatingDialogueTime(List<VWeeklyUserReport> dialogues, Guid userId)
        {
            var ordered = dialogues?.GroupBy(p => p.AspNetUserId)
                .OrderByDescending(x => x.Sum(r => r.DialogueHours / r.Dialogues)).Select((x, i) => new { Place = i, UserId = x.Key });
            return ordered?.Where(x => x.UserId == userId).FirstOrDefault()?.Place ?? -1;
        }
        public double WorkloadTotal(List<VWeeklyUserReport> dialogues, List<VSessionUserWeeklyReport> sessions)
        {
            if (sessions == null || sessions.Count() == 0) return 0;
            var workload = dialogues.Sum(p => p.DialogueHours) / sessions.Sum(p => p.SessionsHours) ?? 0;
            return workload > 1 ? 1 : workload;
        }
        public Dictionary<DateTime, double> AvgWorkloadPerDay(List<VWeeklyUserReport> dialogues, List<VSessionUserWeeklyReport> sessions, DateTime reportBegTime)//---for one user
        {
            DateTime begTime = reportBegTime.AddDays(-6);
            DateTime endTime = reportBegTime;
            var workload = sessions
                .Select(s => new
                {
                    Workload = (sessions != null && sessions.Count() != 0) ?
                        (double)(100 * (double?)dialogues.Where(d => s.Day.Date == d.Day.Date).FirstOrDefault()?.DialogueHours ?? 0 / (double)s.SessionsHours)
                        : 0,
                    s.Day
                });

            Dictionary<DateTime, double> result = new Dictionary<DateTime, double>();
            for (var day = begTime; day <= endTime; day = day.AddDays(1))
            {
                var workloadPerDay = workload.FirstOrDefault(x => x.Day.Date == day.Date) != null ? workload.FirstOrDefault(x => x.Day.Date == day.Date).Workload : 0;
                result.Add(day.Date, workloadPerDay > 100 ? 100 : workloadPerDay);
            }
            return result;
        }
        public int? OfficeRatingWorkload(List<VWeeklyUserReport> dialogues, List<VSessionUserWeeklyReport> sessions, Guid userId)
        {
            var workloadPerUser = sessions?
                .GroupBy(p => p.AspNetUserId)
                .Select(s => new
                {
                    UserId = s.Key,
                    Workload = dialogues.Where(d => d.AspNetUserId == s.Key).Sum(d => d.DialogueHours) / s.Sum(x => x.SessionsHours)
                });
            var orderedWorkload = workloadPerUser?.OrderByDescending(x => x.Workload).Select((x, i) => new { Place = i, x.UserId });
            return orderedWorkload?.Where(x => x.UserId == userId).FirstOrDefault()?.Place ?? -1;
        }
        public double? PhraseTotalAvg(List<VWeeklyUserReport> dialogues, string property)
        {
            if (dialogues == null || dialogues.Count() == 0 || dialogues.Sum(p => p.Dialogues) == 0) return 0;
            Type dialogueType = dialogues.First().GetType();
            PropertyInfo prop = dialogueType.GetProperty(property);
            return dialogues.Sum(p => Convert.ToDouble(prop.GetValue(p))) / dialogues.Sum(p => (double?)p.Dialogues) ?? 0;
        }
        public Dictionary<DateTime, double> PhraseAvgPerDay(List<VWeeklyUserReport> dialogues, string property, DateTime reportBegTime)
        {
            DateTime begTime = reportBegTime.AddDays(-6);
            DateTime endTime = reportBegTime;
            Type dialogueType = null;
            PropertyInfo prop = null;
            if (dialogues != null && dialogues.Count() != 0)
            {
                dialogueType = dialogues.First().GetType();
                prop = dialogueType.GetProperty(property);
            }
            Dictionary<DateTime, double> avgPerDay = new Dictionary<DateTime, double>();
            for (var day = begTime; day <= endTime; day = day.AddDays(1))
            {
                var dialogue = dialogues.Where(x => x.Day.Date == day.Date).FirstOrDefault();
                avgPerDay.Add(day.Date, dialogue != null && dialogue.Dialogues != 0 ? Convert.ToDouble(prop.GetValue(dialogue) ?? 0) / dialogue.Dialogues : 0);
            }
            return avgPerDay;
        }
        public int? OfficeRating(List<VWeeklyUserReport> dialogues, Guid userId, string property)
        {
            if (dialogues == null || dialogues.Count() == 0 || dialogues.Sum(p => p.Dialogues) == 0) return 0;
            Type dialogueType = dialogues.First().GetType();
            PropertyInfo prop = dialogueType.GetProperty(property);
            var ordered = dialogues.GroupBy(p => p.AspNetUserId)
                .OrderByDescending(x => x.Sum(r => Convert.ToDouble(prop.GetValue(r)??0) / r.Dialogues)).Select((x, i) => new { Place = i, UserId = x.Key });
            return ordered?.Where(x => x.UserId == userId).FirstOrDefault()?.Place ?? -1;
        }
    }
}