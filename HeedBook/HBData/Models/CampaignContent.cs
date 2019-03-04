using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HBData.Models
{
    /// <summary>
    /// Content for company campaign
    /// </summary>
    public class CampaignContent
    {
        [Key]
        public Guid CampaignContentId { get; set; }

        /// <summary>
        /// Place in queue
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// Reference to HTML code with buttons, messages, videos, images and links to it
        /// </summary>
        public Guid? ContentId { get; set; }
        [JsonIgnore]
        public Content Content { get; set; }


        /// <summary>
        /// Link to campaign
        /// </summary>
        public Guid CampaignId { get; set; }
        [JsonIgnore]
        public Campaign Campaign { get; set; }

    }

}
