using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;


namespace UserOperations.Models
{
    /// <summary>
    /// Information about employee audios in storage
    /// </summary>
    public class FileAudioEmployee
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        public Guid FileAudioEmployeeId { get; set; }
        /// <summary>
        /// User id
        /// </summary>
        public Guid ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
        /// <summary>
        /// Audio beginning time
        /// </summary>
        public DateTime BegTime { get; set; }
        /// <summary>
        /// Audio ending time
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>
        /// Audio creation time
        /// </summary>
        public DateTime CreationTime {get; set;}
        /// <summary>
        /// Audio filename
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// Audio file folder
        /// </summary>
        public string FileContainer { get; set; }
        /// <summary>
        /// Is file exist in storage
        /// </summary>
        public bool FileExist { get; set; }
        /// <summary>
        /// Audio status
        /// </summary>
        public int? StatusId { get; set; }
        public Status Status { get; set; }
        /// <summary>
        /// Audio duration
        /// </summary>
        public double? Duration { get; set; }
    }
}
