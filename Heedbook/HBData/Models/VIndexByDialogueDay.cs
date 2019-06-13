using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    ///     View table
    /// </summary>
    public class VIndexByDialogueDay
    {
        [Key]
        public Guid Id { get; set; }
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
        public int DialoguesCount { get; set; }
        public int CrossCount { get; set; }
    }
}