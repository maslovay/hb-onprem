using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    /// Information about video file
    /// </summary>
    public class FileVideo
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        public Guid FileVideoId { get; set; }
        /// <summary>
        /// User id
        /// </summary>
        public Guid ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
        /// <summary>
        /// Video file beginnig time
        /// </summary>
        public DateTime BegTime { get; set; }
        /// <summary>
        /// Video file ending time
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>
        /// Video file creation time
        /// </summary>
        public DateTime CreationTime { get; set; }
        /// <summary>
        /// Video file name
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// Video file container
        /// </summary>
        public string FileContainer { get; set; }
        /// <summary>
        /// Is video file exist in folder
        /// </summary>
        public bool FileExist { get; set; }
        /// <summary>
        /// Video file status
        /// </summary>
        public int? StatusId { get; set; }
        public Status Status { get; set; }
        /// <summary>
        /// Video file duration (usually 15 second)
        /// </summary>
        public double? Duration { get; set; }
    }
}
