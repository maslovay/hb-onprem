using System;
using HBLib.Models;

namespace HBData.Models
{
    public class Campaign
    {
        public Guid CampaignId { get; set; }

        /// <summary>
        /// Naming of campaign
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of campaign :
        /// <list type="bullet">
        /// <item><para>"splash" of "advert" for advertisements</para></item>
        /// </list>
        /// </summary>
        public bool IsSplash { get; set; }

        /// <summary>
        /// both, male or female (0,1,2)
        /// </summary>
        public int GenderId { get; set; }

        /// <summary>
        /// Age Range
        /// </summary>
        public int? BegAge { get; set; }
        public int? EndAge { get; set; }

        /// <summary>
        /// start date-time (showing time period)
        /// </summary>
        public DateTime? BegDate { get; set; }

        /// <summary>
        /// end date-time (showing time period)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime? CreationDate { get; set; }

        /// <summary>
        /// Company to differentiate access
        /// </summary>
        public int CompanyId { get; set; }
        public Company Company { get; set; }

        /// <summary>
        /// status
        /// </summary>
        public int? StatusId { get; set; }
        public virtual Status Status { get; set; }
        


    }
}
