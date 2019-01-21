using System;
using HBLib.Models;

namespace HBData.Models
{
    /// <summary>
    /// Content for company campaign
    /// </summary>
    public class CampaignContent
    {
        public Guid CampaignContentId { get; set; }

        /// <summary>
        /// Place in queue
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// Reference to HTML code with buttons, messages, videos, images and links to it
        /// </summary>
        public Guid? ContentId { get; set; }
        public Content Content { get; set; }


        /// <summary>
        /// Link to campaign
        /// </summary>
        public Guid CampaignId { get; set; }
        public Campaign Campaign { get; set; }

    }

}
