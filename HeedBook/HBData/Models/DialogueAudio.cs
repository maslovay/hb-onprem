using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    /// Information about average tone emotions during dialogue
    /// </summary>
    public class DialogueAudio
    {
        /// <summary>
        /// Dialogue audio id
        /// </summary>
        [Key]
        public Guid DialogueAudioId { get; set; }
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
        /// Value of average neutral tone
        /// </summary>
        public double? NeutralityTone { get; set; }
        /// <summary>
        /// Value of average positive tone
        /// </summary>
        public double? PositiveTone { get; set; }
        /// <summary>
        /// Value of average negative tone
        /// </summary>
        public double? NegativeTone { get; set; }

    }
}
