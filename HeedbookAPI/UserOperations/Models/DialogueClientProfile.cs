using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace UserOperations.Models
{
    /// <summary>
    /// Information about client profile
    /// </summary>
    public class DialogueClientProfile
    {
        /// <summary>
        /// Profile id
        /// </summary>
        [Key]
        public Guid DialogueClientProfileId { get; set; }
		/// <summary>
        /// Dialogue id
        /// </summary>
		public Guid? DialogueId { get; set; }
        public  Dialogue Dialogue { get; set; }
		/// <summary>
        /// Is client or employee (true | false)
        /// </summary>
		public bool IsClient { get; set; }
        /// <summary>
        /// Filename of avatar in storage
        /// </summary>
        public string Avatar { get; set; }
        /// <summary>
        /// Client age
        /// </summary>
        public double? Age { get; set; }
        /// <summary>
        /// Client gender
        /// </summary>
        public string Gender { get; set; }

    }
}
