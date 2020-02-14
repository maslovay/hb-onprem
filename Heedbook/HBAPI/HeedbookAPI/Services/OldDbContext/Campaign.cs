using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Old.Models
{
    /// <summary>
    ///     Parameters and information about advertising campaign
    /// </summary>
    public class Campaign
    {
        /// <summary>
        ///     Campaign id
        /// </summary>
        [Key]
        public Guid CampaignId { get; set; }

        /// <summary>
        ///     Naming of campaign
        /// </summary>
        public String Name { get; set; }

        /// <summary>
        ///     Type of campaign :
        ///     <list type="bullet">
        ///         <item>
        ///             <para>"splash" of "advert" for advertisements</para>
        ///         </item>
        ///     </list>
        /// </summary>
        public Boolean IsSplash { get; set; }

        /// <summary>
        ///     Both, male or female (0,1,2)
        /// </summary>
        public Int32 GenderId { get; set; }

        /// <summary>
        ///     Age Range
        /// </summary>
        public Int32? BegAge { get; set; }

        public Int32? EndAge { get; set; }

        /// <summary>
        ///     Start date-time (showing time period)
        /// </summary>
        public DateTime? BegDate { get; set; }

        /// <summary>
        ///     End date-time (showing time period)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        ///     Creation date
        /// </summary>
        public DateTime? CreationDate { get; set; }

        /// <summary>
        ///     Company to differentiate access
        /// </summary>
        public Guid CompanyId { get; set; }
        [JsonIgnore] public Company Company { get; set; }

        /// <summary>
        ///     Status
        /// </summary>
        public Int32? StatusId { get; set; }
        [JsonIgnore] public Status Status { get; set; }

        public ICollection<CampaignContent> CampaignContents { get; set; }
    }
}