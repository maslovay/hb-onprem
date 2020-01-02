using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HBData.Models
{
    /// <summary>
    ///     Information about ad impressions
    /// </summary>
    public class SlideShowSession
    {
        [Key] public Guid SlideShowSessionId { get; set; }

        /// <summary>
        ///     Start of content show
        /// </summary>
        public DateTime BegTime { get; set; }

        /// <summary>
        ///     End of content show
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        ///     Id CampaignContent means the content in partiqular campaign wich shown:
        /// </summary>
        public Guid? CampaignContentId { get; set; }

        public CampaignContent CampaignContent { get; set; }

        /// <summary>
        ///     Content type (media or poll)
        /// </summary>
        public bool IsPoll { get; set; }
        /// <summary>
        ///     Content type (content, localContent, url)
        /// </summary>
        public String ContentType { get; set; }

        /// <summary>
        ///     if content is url save link
        /// </summary>
        public String Url { get; set; }


        /// <summary>
        ///     Place where the content was shown
        /// </summary>
        public Guid DeviceId { get; set; }
        [JsonIgnore] public Device Device { get; set; }

        /// <summary>
        ///     Employee who showed the content
        /// </summary>
        public Guid? ApplicationUserId { get; set; }
        [JsonIgnore] public ApplicationUser ApplicationUser { get; set; }
    }
}