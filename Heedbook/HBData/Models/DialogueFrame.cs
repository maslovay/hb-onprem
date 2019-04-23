using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HBData.Models
{
    /// <summary>
    ///     Information about client emotions on each frame during dialogue
    /// </summary>
    public class DialogueFrame
    {
        /// <summary>
        ///     Dialogue frame id
        /// </summary>
        [Key]
        public Guid DialogueFrameId { get; set; }

        /// <summary>
        ///     Dialogue id
        /// </summary>
        public Guid? DialogueId { get; set; }
        [JsonIgnore]
        public Dialogue Dialogue { get; set; }

        /// <summary>
        ///     Is client or employee (yes | no)
        /// </summary>
        public Boolean IsClient { get; set; }

        /// <summary>
        ///     Time of emotion recognition
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        ///     Happiness share
        /// </summary>
        public Double? HappinessShare { get; set; }

        /// <summary>
        ///     Neutral share
        /// </summary>
        public Double? NeutralShare { get; set; }

        /// <summary>
        ///     Surprise share
        /// </summary>
        public Double? SurpriseShare { get; set; }

        /// <summary>
        ///     Disgust share
        /// </summary>
        public Double? SadnessShare { get; set; }

        /// <summary>
        ///     Anger share
        /// </summary>
        public Double? AngerShare { get; set; }

        /// <summary>
        ///     Disgust share
        /// </summary>
        public Double? DisgustShare { get; set; }

        /// <summary>
        ///     Contempt share
        /// </summary>
        public Double? ContemptShare { get; set; }

        /// <summary>
        ///     Fear share
        /// </summary>
        public Double? FearShare { get; set; }

        /// <summary>
        ///     Face yaw share
        /// </summary>
        public Double? YawShare { get; set; }
    }
}