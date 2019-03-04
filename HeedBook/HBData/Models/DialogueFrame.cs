using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    /// Information about client emotions on each frame during dialogue
    /// </summary>
    public class DialogueFrame
    {
        /// <summary>
        /// Dialogue frame id
        /// </summary>
        [Key]
        public Guid DialogueFrameId { get; set; }
        /// <summary>
        /// Dialogue id
        /// </summary>
        public Guid? DialogueId { get; set; }
        public  Dialogue Dialogue { get; set; }
        /// <summary>
        /// Is client or employee (yes | no)
        /// </summary>
		public bool IsClient { get; set; }
        /// <summary>
        /// Time of emotion recognition
        /// </summary>
        public DateTime Time { get; set; }
        /// <summary>
        /// Happiness share
        /// </summary>
        public double? HappinessShare { get; set; }
        /// <summary>
        /// Neutral share
        /// </summary>
        public double? NeutralShare { get; set; }
        /// <summary>
        /// Surprise share
        /// </summary>
        public double? SurpriseShare { get; set; }
        /// <summary>
        /// Disgust share
        /// </summary>
        public double? SadnessShare { get; set; }
        /// <summary>
        /// Anger share
        /// </summary>
        public double? AngerShare { get; set; }
        /// <summary>
        /// Disgust share
        /// </summary>
        public double? DisgustShare { get; set; }
        /// <summary>
        /// Contempt share
        /// </summary>
        public double? ContemptShare { get; set; }
        /// <summary>
        /// Fear share
        /// </summary>
        public double? FearShare { get; set; }
        /// <summary>
        /// Face yaw share
        /// </summary>
        public double? YawShare { get; set; }

    }
}
