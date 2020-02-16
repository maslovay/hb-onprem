using System;
using System.ComponentModel.DataAnnotations;

namespace Old.Models
{
    /// <summary>
    ///     Information about client emotions on each frame during dialogue
    /// </summary>
    public class DialogueFileFrame
    {
        /// <summary>
        ///     Dialogue frame id
        /// </summary>
        [Key]
        public Guid DialogueFileFrameId { get; set; }

        /// <summary>
        ///     Dialogue id
        /// </summary>
        public Guid? DialogueId { get; set; }

        public Dialogue Dialogue { get; set; }

        /// <summary>
        ///     File frame id
        /// </summary>
        public Guid? FileFrameId { get; set; }

        public FileFrame FileFrame { get; set; }
    }
}