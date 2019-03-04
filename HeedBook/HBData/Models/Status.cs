using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    /// Information about status
    /// </summary>
    public class Status
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        public int StatusId { get; set; }
        /// <summary>
        /// Status name
        /// </summary>
        [Required]
        public string StatusName { get; set; }
        /// <summary>
        /// Link to users
        /// </summary>
        public  ICollection<ApplicationUser> ApplicationUser { get; set; }
        /// <summary>
        /// Link to dialogues
        /// </summary>
        public  ICollection<Dialogue> Dialogue { get; set; }

    }
}
