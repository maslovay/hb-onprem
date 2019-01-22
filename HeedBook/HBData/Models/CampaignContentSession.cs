using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HBData.Models
{
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
        public Guid ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }
    }
}
