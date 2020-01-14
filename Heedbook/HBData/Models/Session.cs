using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HBData.Models
{
    /// <summary>
    ///     Information about user session
    /// </summary>
    public class Session
    {
        /// <summary>
        ///     Id
        /// </summary>
        [Key]
        public Guid SessionId { get; set; }

        /// <summary>
        ///     User id
        /// </summary>
        public Guid ApplicationUserId { get; set; }
        [JsonIgnore] public ApplicationUser ApplicationUser { get; set; }

        /// <summary>
        ///     Device id
        /// </summary>
        public Guid DeviceId { get; set; }//!!!required
        [JsonIgnore] public Device Device { get; set; }

        /// <summary>
        ///     Session beginning time
        /// </summary>
        public DateTime BegTime { get; set; }

        /// <summary>
        ///     Session ending time
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        ///     Session status
        /// </summary>
        public Int32? StatusId { get; set; }

        public Status Status { get; set; }

        /// <summary>
        ///     Is desktop session or mobile
        /// </summary>
        public Boolean IsDesktop { get; set; }
    }
}