using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    ///     Information about teacher mark up
    /// </summary>
    public class DialogueMarkup
    {
        /// <summary>
        ///     Markup id
        /// </summary>
        [Key]
        public Guid DialogueMarkUpId { get; set; }

        /// <summary>
        ///     User id
        /// </summary>
        public Guid? ApplicationUserId { get; set; }

        public ApplicationUser ApplicationUser { get; set; }

        /// <summary>
        ///     Beginning time of frames list
        /// </summary>
        public DateTime BegTime { get; set; }

        /// <summary>
        ///     Creation time of markup
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        ///     Ending time of frames list
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        ///     Mark up beginning time
        /// </summary>
        public DateTime BegTimeMarkup { get; set; }

        /// <summary>
        ///     Mark up ending time
        /// </summary>
        public DateTime EndTimeMarkup { get; set; }

        /// <summary>
        ///     Is dialogue or part of dialogue
        /// </summary>
        public Boolean IsDialogue { get; set; }

        /// <summary>
        ///     Status
        /// </summary>
        public Int32? StatusId { get; set; }

        public Status Status { get; set; }

        /// <summary>
        ///     Teacher id
        /// </summary>
        public String TeacherId { get; set; }
    }
}