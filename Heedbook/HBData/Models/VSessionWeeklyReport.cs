using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    ///     View table
    /// </summary>
    public class VSessionWeeklyReport
    {
        [Key]
        public Guid Id { get; set; }
        public Guid AspNetUserId { get; set; }
        /// <summary>
        ///     Period in days
        /// </summary>
        public DateTime Day { get; set; }

        /// <summary>
        ///     Company id
        /// </summary>
        public Guid CompanyId { get; set; }

       // public Company Company { get; set; }

        /// <summary>
        ///     Industry
        /// </summary>
        public Guid CompanyIndustryId { get; set; }
        public double SessionsHours { get; set; }
        public int SessionsAmount {get; set;}
    }
}