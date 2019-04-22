using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    ///     Information about client profile
    /// </summary>
    public class DialogueClientProfile
    {
        /// <summary>
        ///     Profile id
        /// </summary>
        [Key]
        public Guid DialogueClientProfileId { get; set; }

        /// <summary>
        ///     Dialogue id
        /// </summary>
        public Guid? DialogueId { get; set; }

        public Dialogue Dialogue { get; set; }

        /// <summary>
        ///     Is client or employee (true | false)
        /// </summary>
        public Boolean IsClient { get; set; }

        /// <summary>
        ///     Filename of avatar in storage
        /// </summary>
        public String Avatar { get; set; }

        /// <summary>
        ///     Client age
        /// </summary>
        public Double? Age { get; set; }

        /// <summary>
        ///     Client gender
        /// </summary>
        public String Gender { get; set; }
    }
}