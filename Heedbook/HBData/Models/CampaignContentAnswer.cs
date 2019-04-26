using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HBData.Models
{
    /// <summary>
    ///     Content for company campaign
    /// </summary>
    public class CampaignContentAnswer
    {
        [Key] public Guid CampaignContentAnswerId { get; set; }

        /// <summary>
        ///     Answer
        /// </summary>
        public string Answer { get; set; }

        /// <summary>
        ///     Campaign content link
        /// </summary>
        public Guid CampaignContentId { get; set; }

        [JsonIgnore] public CampaignContent CampaignContent { get; set; }

        
        /// <summary>
        ///     Time of answer
        /// </summary>
        public DateTime Time { get; set; }
    }
}