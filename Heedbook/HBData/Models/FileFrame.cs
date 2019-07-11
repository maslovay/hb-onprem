using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    ///     Information about frames
    /// </summary>
    public class FileFrame
    {
        /// <summary>
        ///     Id
        /// </summary>
        [Key]
        public Guid FileFrameId { get; set; }

        /// <summary>
        ///     User id
        /// </summary>
        public Guid ApplicationUserId { get; set; }

        public ApplicationUser ApplicationUser { get; set; }

        /// <summary>
        ///     Is file exist
        /// </summary>
        public Boolean FileExist { get; set; }

        /// <summary>
        ///     Filename
        /// </summary>
        public String FileName { get; set; }

        /// <summary>
        ///     File folder
        /// </summary>
        public String FileContainer { get; set; }

        /// <summary>
        ///     Teacher
        /// </summary>
        public Int32? StatusId { get; set; }

        public Status Status { get; set; }

        /// <summary>
        ///     Status
        /// </summary>
        public Int32? StatusNNId { get; set; }

        public Status StatusNN { get; set; }

        /// <summary>
        ///     Frame time
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        ///     Face id in group of faces
        /// </summary>
        public Guid? FaceId {get; set;}

        /// <summary>
        ///     Is face present on frame
        /// </summary>
        public Boolean IsFacePresent { get; set; }

        /// <summary>
        ///     Number of faces on frame
        /// </summary>
        public Int32? FaceLength { get; set; }

        public ICollection<FrameEmotion> FrameEmotion { get; set; }

        public ICollection<FrameAttribute> FrameAttribute { get; set; }
    }
}