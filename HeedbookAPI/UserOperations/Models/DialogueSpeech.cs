using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace UserOperations.Models
{
    /// <summary>
    /// Information about speech
    /// </summary>
    public class DialogueSpeech
    {
        /// <summary>
        /// Speech id
        /// </summary>
        [Key]
        public Guid DialogueSpeechId { get; set; }
		/// <summary>
        /// Dialogue id
        /// </summary>
		public Guid? DialogueId { get; set; }
        public  Dialogue Dialogue { get; set; }
        /// <summary>
        /// Is client or employee
        /// </summary>
		public bool IsClient { get; set; }
		/// <summary>
        /// Text positive share (Microsoft)
        /// </summary>
        public double? PositiveShare { get; set; }
        /// <summary>
        /// Speech speed (sounds per second)
        /// </summary>
        public double? SpeechSpeed { get; set; }
        /// <summary>
        /// Silence share
        /// </summary>
        public double? SilenceShare { get; set; }

    }
}
