using System;

namespace ApiPerformance.Models
{
    public class CampaignContentAnswerModel
    {
        public string Answer { get; set; }
        public string AnswerText { get; set; }
        public Guid CampaignContentId { get; set; }
        public Guid DeviceId { get; set; }
        public Guid? ApplicationUserId { get; set; }
        public DateTime Time { get; set; }
    }
}