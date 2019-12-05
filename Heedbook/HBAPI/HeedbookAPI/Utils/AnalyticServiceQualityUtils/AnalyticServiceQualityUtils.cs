using System;
using System.Collections.Generic;
using System.Linq;
using UserOperations.Models.Get.AnalyticServiceQualityController;

namespace UserOperations.Utils.AnalyticServiceQualityUtils
{
    public class AnalyticServiceQualityUtils
    {
        public double? LoyaltyIndex(List<ComponentsDialogueInfo> dialogues)
        {
            var dialoguesCount = dialogues.Any() ? dialogues.Select(p => p.DialogueId).Distinct().Count() : 0;
            var crossDialoguesCount = dialogues.Any() ? dialogues.Sum(p => Math.Min(p.Loyalty, 1)) : 0;
            return dialoguesCount != 0 ? 100 * Convert.ToDouble(crossDialoguesCount) / Convert.ToDouble(dialoguesCount) : 0;
        }
        public double? SatisfactionIndex(List<DialogueInfo> dialogues)
        {
            return dialogues.Any() ? dialogues.Where(p => p.SatisfactionScore != null && p.SatisfactionScore != 0).Average(p => p.SatisfactionScore) : null;
        }
        public int DialoguesCount(List<DialogueInfo> dialogues, Guid? applicationUserId = null, DateTime? date = null)
        {
            return dialogues.Any() ? dialogues
                .Where(p => (applicationUserId == null || p.ApplicationUserId == applicationUserId) &&
                    (date == null || p.BegTime.Date == date))
                .Select(p => p.DialogueId).Distinct().Count() : 0;
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
        public double? LoyaltyIndex(IGrouping<string, RatingDialogueInfo> dialogues)
        {
            var dialoguesCount = dialogues.Any() ? dialogues.Select(p => p.DialogueId).Distinct().Count() : 0;
            var loyaltyDialoguesCount = dialogues.Any() ? dialogues.Sum(p => Math.Min(p.LoyaltyCount, 1)) : 0;
            return dialoguesCount != 0 ? 100 * Convert.ToDouble(loyaltyDialoguesCount) / Convert.ToDouble(dialoguesCount) : 0;
        }
    }
} 