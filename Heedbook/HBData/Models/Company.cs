using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

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
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid CompanyId { get; set; }
        /// <summary>
        ///     Company name
        /// </summary>
        
        [Required]
        public String CompanyName { get; set; }

        public bool IsExtended { get; set; }

        /// <summary>
        ///     Id of company industry
        /// </summary>
        public Guid? CompanyIndustryId { get; set; }
        [JsonIgnore] public CompanyIndustry CompanyIndustry { get; set; }

        /// <summary>
        ///     Company creation date
        /// </summary>
        public DateTime CreationDate { get; set; }
        public string TimeZoneName { get; set; }

        /// <summary>
        ///     Company language id
        /// </summary>
        public Int32? LanguageId { get; set; }
        [JsonIgnore] public Language Language { get; set; }

        /// <summary>
        ///     Company country id
        /// </summary>
        public Guid? CountryId { get; set; }
        [JsonIgnore] public Country Country { get; set; }

        /// <summary>
        ///     Company status id
        /// </summary>
        public Int32? StatusId { get; set; }
        [JsonIgnore] public Status Status { get; set; }
        /// <summary>
        ///     Link to application users
        /// </summary>
        public ICollection<ApplicationUser> ApplicationUser { get; set; }

        /// <summary>
        ///     Link to company devices
        /// </summary>
        public ICollection<Device> Devices { get; set; }

        /// <summary>
        ///     Link to payments
        /// </summary>
        [JsonIgnore] public ICollection<Payment> Payment { get; set; }

        /// <summary>
        ///     Company corporation id
        /// </summary>
        public Guid? CorporationId { get; set; }
        [JsonIgnore] public Corporation Corporation { get; set; }
    }
}