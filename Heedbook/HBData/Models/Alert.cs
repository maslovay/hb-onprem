using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace HBData.Models
{
    /// <summary>
    ///     Information about alerts
    /// </summary>
    public class Alert
    {
        /// <summary>
        ///     Alert id
        /// </summary>
        [Key]
        public Guid AlertId { get; set; }
     
        /// <summary>
        ///     Alert type id
        /// </summary>
        public Guid AlertTypeId { get; set; }
        [JsonIgnore]
        [ForeignKey("AlertTypeId")]
        public AlertType AlertType { get; set; }

        /// <summary>
        ///     Alert creation date
        /// </summary>
        public DateTime CreationDate { get; set; }

        /// <summary>
        ///     User id
        /// </summary>
        public Guid ApplicationUserId { get; set; }
        [JsonIgnore]
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser ApplicationUser { get; set; }
        /// <summary>
        ///     Device id
        /// </summary>
        public Guid DeviceId { get; set; }
        [JsonIgnore]
        [ForeignKey("DeviceId")]
        public Device Device { get; set; }
    }
}