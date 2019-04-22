using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    ///     Information about status
    /// </summary>
    public class Status
    {
        /// <summary>
        ///     1 - Online
        ///     2 - Offline
        ///     3 - Active
        ///     4 - Disabled
        ///     5 - Inactive
        ///     6 - InProgress
        ///     7 - Finished
        ///     8 - Error
        ///     9 - Pending disabled
        ///     10 - Trial  
        ///     11 - AutoActive
        ///     12 - AutoFinished
        ///     13 - AutoError
        /// </summary>
        [Key]
        public Int32 StatusId { get; set; }

        /// <summary>
        ///     Status name
        /// </summary>
        [Required]
        public String StatusName { get; set; }

        /// <summary>
        ///     Link to users
        /// </summary>
        public ICollection<ApplicationUser> ApplicationUser { get; set; }

        /// <summary>
        ///     Link to dialogues
        /// </summary>
        public ICollection<Dialogue> Dialogue { get; set; }
    }
}