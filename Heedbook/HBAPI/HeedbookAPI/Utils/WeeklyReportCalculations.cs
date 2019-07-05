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
    public class DBOperationsWeeklyReport
    {
        private readonly RecordsContext _context;

        public DBOperationsWeeklyReport(RecordsContext context, IConfiguration config)
        {
            _context = context;
        }
        public double? TotalAvg (List<VWeeklyUserReport> dialogues, string property)
        {
           if ( dialogues == null || dialogues.Count() == 0 ) return 0;
           Type dialogueType = dialogues.First().GetType();
           PropertyInfo prop = dialogueType.GetProperty(property);
           return dialogues.Sum(p => (double?)prop.GetValue(p)) / dialogues.Count();
        }

        public Dictionary<DateTime, double> AvgPerDay (List<VWeeklyUserReport> dialogues, string property)
        {
           if ( dialogues == null || dialogues.Count() == 0 ) return new Dictionary<DateTime, double>();
           Type dialogueType = dialogues.First().GetType();
           PropertyInfo prop = dialogueType.GetProperty(property);
           return dialogues.ToDictionary(x => x.Day, i => ((double?)prop.GetValue(i))??0);
        }

        public double AvgDialogueTimeTotal(List<VWeeklyUserReport> dialogues)
        {
            if ( dialogues == null || dialogues.Count() == 0 ) return 0;
            return dialogues.Sum(p => p.DialogueHours) / dialogues.Sum(p => p.Dialogues)?? 0;
        }

        public double WorkloadTotal(List<VWeeklyUserReport> dialogues, List<VSessionUserWeeklyReport> sessions)
        {
            if ( sessions == null || sessions.Count() == 0 ) return 0;
            return dialogues.Sum(p => p.DialogueHours) / sessions.Sum(p => p.SessionsHours)?? 0;
        }

        public int? OfficeRatingSatisfactionPlace(List<VWeeklyUserReport> dialogues, Guid userId)
        {
            if ( dialogues == null || dialogues.Count() == 0 ) return 0;
            var OrderedBySatisf = dialogues
                        .GroupBy(p => p.AspNetUserId)
                        .Select(p => new { satisf = p.Average(x => x.Satisfaction), p.Key })
                        .OrderByDescending(s => s.satisf)
                        .Select((s, i) => new { place = i, userId = s.Key });
            return OrderedBySatisf.Where(p => p.userId == userId).FirstOrDefault()?.place?? 0;
        }

        public int? OfficeRatingPositiveEmotPlace(List<VWeeklyUserReport> dialogues, Guid userId)
        {
            if ( dialogues == null || dialogues.Count() == 0 ) return 0;
            var OrderedByPositive = dialogues
                       .GroupBy(p => p.AspNetUserId)
                       .Select(p => new { positive = p.Average(x => x.PositiveEmotions), p.Key })
                       .OrderByDescending(s => s.positive)
                       .Select((s, i) => new { place = i, userId = s.Key });
            return OrderedByPositive.Where(p => p.userId == userId).FirstOrDefault()?.place?? 0;
        }

        public int? OfficeRatingPositiveIntonationPlace(List<VWeeklyUserReport> dialogues, Guid userId)
        {
            if ( dialogues == null || dialogues.Count() == 0 ) return 0;
            var OrderedByPositive = dialogues
                       .GroupBy(p => p.AspNetUserId)
                       .Select(p => new { positive = p.Average(x => x.PositiveTone), p.Key })
                       .OrderByDescending(s => s.positive)
                       .Select((s, i) => new { place = i, userId = s.Key });
            return OrderedByPositive.Where(p => p.userId == userId).FirstOrDefault()?.place?? 0;
        }

        public int? OfficeRatingSpeechEmotPlace(List<VWeeklyUserReport> dialogues, Guid userId)
        {
            if ( dialogues == null || dialogues.Count() == 0 ) return 0;
            var OrderedByPositive = dialogues
                       .GroupBy(p => p.AspNetUserId)
                       .Select(p => new { positive = p.Average(x => (double?)x.SpeekEmotions), p.Key })
                       .OrderByDescending(s => s.positive)
                       .Select((s, i) => new { place = i, userId = s.Key });
            return OrderedByPositive.Where(p => p.userId == userId).FirstOrDefault()?.place?? 0;
        }
        public Dictionary<DateTime, double> AvgWorkloadPerDay(List<VWeeklyUserReport> dialogues, List<VSessionUserWeeklyReport> sessions)//---for one user
        {
            if(sessions == null || sessions.Count() == 0) return new Dictionary<DateTime, double>();
            return sessions
                .Select(s => new
                {
                    Workload = (double) (100 * (double?)dialogues.Where(d => s.Day == d.Day).FirstOrDefault()?.DialogueHours?? 0 / (double)s.SessionsHours),
                    Day = s.Day
                }).OrderByDescending(s => s.Day).ToDictionary(x => x.Day, i => i.Workload);
        }
        public int? OfficeRatingWorkload(List<VWeeklyUserReport> dialogues, List<VSessionUserWeeklyReport> sessions, Guid userId)
        {
            var workloadPerUser = sessions
                .GroupBy(p => p.AspNetUserId)
                .Select(s => new
                {
                    UserId = s.Key,
                    Workload = dialogues.Where(d => d.AspNetUserId == s.Key).Sum(d => d.DialogueHours) / s.Sum(x => x.SessionsHours)
                });
            var orderedWorkload = workloadPerUser.OrderByDescending(x => x.Workload).Select((x, i) => new { Place = i, x.UserId });
            return orderedWorkload.Where(x => x.UserId == userId).FirstOrDefault()?.Place?? 0;
        }
        public int? OfficeRatingWorkingHours(List<VSessionUserWeeklyReport> sessions, Guid userId)
        {
            var ordered = sessions.GroupBy(p => p.AspNetUserId)
                .OrderByDescending(x => x.Sum(r => r.SessionsHours)).Select((x, i) => new { Place = i, UserId = x.Key });
            return ordered.Where(x => x.UserId == userId).FirstOrDefault()?.Place?? 0;
        }
        public int? OfficeRatingDialogueTime(List<VWeeklyUserReport> dialogues, Guid userId)
        {
            var ordered = dialogues.GroupBy(p => p.AspNetUserId)
                .OrderByDescending(x => x.Sum(r => r.DialogueHours / r.Dialogues)).Select((x, i) => new { Place = i, UserId = x.Key });
            return ordered.Where(x => x.UserId == userId).FirstOrDefault()?.Place?? 0;
        }     
        public int? OfficeRatingDialoguesAmount(List<VWeeklyUserReport> dialogues, Guid userId)
        {
            var ordered = dialogues.GroupBy(p => p.AspNetUserId)
                .OrderByDescending(x => x.Sum(r => r.Dialogues)).Select((x, i) => new { Place = i, UserId = x.Key });
            return ordered.Where(x => x.UserId == userId).FirstOrDefault()?.Place?? 0;
        }      
        public int? OfficeRating(List<VWeeklyUserReport> dialogues, Guid userId, string property)
        {
            if ( dialogues == null || dialogues.Count() == 0 || dialogues.Sum(p => p.Dialogues) == 0) return 0;        
            Type dialogueType = dialogues.First().GetType();
            PropertyInfo prop = dialogueType.GetProperty(property);
            var ordered = dialogues.GroupBy(p => p.AspNetUserId)
                .OrderByDescending(x => x.Sum(r => Convert.ToDouble(prop.GetValue(r)) / r.Dialogues)).Select((x, i) => new { Place = i, UserId = x.Key });
            return ordered.Where(x => x.UserId == userId).FirstOrDefault()?.Place?? 0;
        }      

        public double? PhraseTotalAvg(List<VWeeklyUserReport> dialogues, string property)
        {     
            if ( dialogues == null || dialogues.Count() == 0 || dialogues.Sum(p => p.Dialogues) == 0) return 0;        
            Type dialogueType = dialogues.First().GetType();
            PropertyInfo prop = dialogueType.GetProperty(property);
            return dialogues.Sum(p => Convert.ToDouble(prop.GetValue(p)))/ dialogues.Sum(p => (double?)p.Dialogues)?? 0;
        }
    }
}