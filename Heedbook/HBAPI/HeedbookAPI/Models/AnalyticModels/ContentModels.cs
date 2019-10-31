using System;
using System.Collections.Generic;

namespace UserOperations.Models.AnalyticModels
{
    public class ContentOneInfo
    {
        public string Content { get; set; }
        public string ContentType { get; set; }
        public string ContentName { get; set; }
        public int AmountOne { get; set; }
        public EmotionAttention EmotionAttention { get; set; }
    }

    public class ContentFullOneInfo
    {
        public string Content { get; set; }
        public string ContentName { get; set; }
        public int AmountViews { get; set; }
        public EmotionAttention EmotionAttention { get; set; }
        public int Male { get; set; }
        public int Female { get; set; }
        public double? Age { get; set; }
    }

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
        }
    }
}