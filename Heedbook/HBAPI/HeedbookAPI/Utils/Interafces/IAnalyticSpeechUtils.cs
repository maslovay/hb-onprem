using System;
using System.Linq;
using UserOperations.Models.AnalyticModels;

namespace UserOperations.Utils.Interfaces
{
    public interface IAnalyticSpeechUtils
    {
        double? AlertIndex(IGrouping<Guid?, DialogueInfo> dialogues);
        double? CrossIndex(IGrouping<Guid?, DialogueInfo> dialogues);
    }
}