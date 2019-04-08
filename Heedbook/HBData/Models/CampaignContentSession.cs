using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace HBData.Models
{
    /// <summary>
    /// Information about ad impressions
    /// </summary>
    public class CampaignContentSession
    {
        [Key]
        public Guid CampaignContentSessionId { get; set; }

        /// <summary>
        /// Start of content show
        /// </summary>
        public DateTime BegTime { get; set; }

        /// <summary>
        /// Id CampaignContent means the content in partiqular campaign wich shown:
        /// </summary>
        public Guid CampaignContentId { get; set; }
        public CampaignContent CampaignContent { get; set; }


        /// <summary>
        /// Place where the content was shown
        /// </summary>
        public Guid? ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser ApplicationUser { get; set; }
    }
}
