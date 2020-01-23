using System;
using System.Collections.Generic;

namespace UserOperations.Models.AnalyticModels
{
    public class AnswerInfo
    {
        public string Content { get; set; }
        public int AnswersAmount { get; set; }
        public string ContentName { get; set; }
        public int AmountViews { get; set; }
        public double Conversion { get; set; }
        public List<AnswerOne> Answers { get; set; }
        public class AnswerOne
        {
            public string Answer { get; set; }
            public DateTime Time { get; set; }
            public Guid? DialogueId { get; set; }
            public Guid? ContentId { get; set; }
        }
    }
}