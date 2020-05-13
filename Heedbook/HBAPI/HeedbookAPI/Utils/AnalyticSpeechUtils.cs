using System;
using System.Linq;
using UserOperations.Models.AnalyticModels;
using UserOperations.Utils.Interfaces;

namespace UserOperations.Utils.AnalyticSpeechController
{
    public class AnalyticSpeechUtils : IAnalyticSpeechUtils
    {
        public double? CrossIndex(IGrouping<Guid?, DialogueInfo> dialogues)
        {
            var dialoguesCount = dialogues.Any() ? dialogues.Select(p => p.DialogueId).Distinct().Count() : 0;
            var crossDialoguesCount = dialogues.Any() ? dialogues.Sum(p => Math.Min(p.CrossCount, 1)) : 0;
            return dialoguesCount != 0 ? 100 * Convert.ToDouble(crossDialoguesCount) / Convert.ToDouble(dialoguesCount) : 0;
        }
        public double? AlertIndex(IGrouping<Guid?, DialogueInfo> dialogues)
        {
            var dialoguesCount = dialogues.Any() ? dialogues.Select(p => p.DialogueId).Distinct().Count() : 0;
            var alertDialoguesCount = dialogues.Any() ? dialogues.Sum(p => Math.Min(p.AlertCount, 1)) : 0;
            return dialoguesCount != 0 ? 100 * Convert.ToDouble(alertDialoguesCount) / Convert.ToDouble(dialoguesCount) : 0;
        }
    }
}