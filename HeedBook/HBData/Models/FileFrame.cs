using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;


namespace HBData.Models
{
    /// <summary>
    /// Information about frames
    /// </summary>
    public class FileFrame
    {
        /// <summary>
        /// Id
        /// </summary>
        [Key]
        public Guid FileFrameId { get; set; }
        /// <summary>
        /// User id
        /// </summary>
        public Guid ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
        /// <summary>
        /// Is file exist
        /// </summary>
        public bool FileExist { get; set; }
        /// <summary>
        /// Filename
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// File folder
        /// </summary>
        public string FileContainer { get; set; }
        /// <summary>
        /// Teacher
        /// </summary>
        public int? StatusId { get; set; }
        public Status Status { get; set; }
        /// <summary>
        /// Status
        /// </summary>
        public int? StatusNNId { get; set; }
        public Status StatusNN { get; set; }
        /// <summary>
        /// Frame time
        /// </summary>
        public DateTime Time { get; set; }
        /// <summary>
        /// Is face present on frame
        /// </summary>
        public bool IsFacePresent { get; set; }
        /// <summary>
        /// Number of faces on frame
        /// </summary>
        public int? FaceLength { get; set; }

    }
}
