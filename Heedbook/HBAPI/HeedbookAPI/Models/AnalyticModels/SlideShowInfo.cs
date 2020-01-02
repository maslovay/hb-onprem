using System;
using System.Collections.Generic;
using HBData.Models;

namespace UserOperations.Models.AnalyticModels
{
    public class SlideShowInfo
    {
        public DateTime BegTime { get; set; }
        public Guid? ContentId { get; set; }
        public Guid? CampaignContentId { get; set; }
        public Campaign Campaign { get; set; }
        public string ContentType { get; set; }
        public string ContentName { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsPoll { get; set; }
        public string Url { get; set; }
        public Guid? ApplicationUserId { get; set; }
        public Guid DeviceId { get; set; }
        public Guid? DialogueId { get; set; }
        public List<DialogueFrame> DialogueFrames { get; set; }
        public double? Age { get; set; }
        public string Gender { get; set; }
        public EmotionAttention EmotionAttention { get; set; }
    }

}