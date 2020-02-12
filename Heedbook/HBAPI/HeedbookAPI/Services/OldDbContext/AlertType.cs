using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Old.Models
{
    /// <summary>
    ///    Types of alerts wich users can see on dashboard
    /// </summary>
    public class AlertType
    {
        /// <summary>
        ///     AlertType id
        /// </summary>
        [Key]
        public Guid AlertTypeId { get; set; }
        /// <summary>
        ///     Alert type name
        /// </summary>
        
        [Required]
        public String Name { get; set; }
    }
}