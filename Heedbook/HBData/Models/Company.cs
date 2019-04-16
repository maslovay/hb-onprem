using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    ///     Information about company and company parameters
    /// </summary>
    public class Company
    {
        /// <summary>
        ///     Company id
        /// </summary>
        [Key]
        public Guid CompanyId { get; set; }

        /// <summary>
        ///     Company name
        /// </summary>
        [Required]
        public String CompanyName { get; set; }

        /// <summary>
        ///     Id of company industry
        /// </summary>
        public Guid? CompanyIndustryId { get; set; }

        public CompanyIndustry CompanyIndustry { get; set; }

        /// <summary>
        ///     Company creation date
        /// </summary>
        public DateTime CreationDate { get; set; }

        /// <summary>
        ///     Company language id
        /// </summary>
        public Int32? LanguageId { get; set; }

        public Language Language { get; set; }

        /// <summary>
        ///     Company country id
        /// </summary>
        public Guid? CountryId { get; set; }

        public Country Country { get; set; }

        /// <summary>
        ///     Company status id
        /// </summary>
        public Int32? StatusId { get; set; }

        public Status Status { get; set; }

        /// <summary>
        ///     Link to application users
        /// </summary>
        public ICollection<ApplicationUser> ApplicationUser { get; set; }

        /// <summary>
        ///     Link to payments
        /// </summary>
        public ICollection<Payment> Payment { get; set; }

        /// <summary>
        ///     Company corporation id
        /// </summary>
        public Guid? CorporationId { get; set; }

        public Corporation Corporation { get; set; }
    }
}