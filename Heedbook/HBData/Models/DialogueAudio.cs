using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HBData.Models
{
    /// <summary>
    ///     Information about average tone emotions during dialogue
    /// </summary>
    public class DialogueAudio
    {
        /// <summary>
        ///     Dialogue audio id
        /// </summary>
        [Key]
        public Guid DialogueAudioId { get; set; }

        /// <summary>
        ///     Dialogue id
        /// </summary>
        public Guid? DialogueId { get; set; }

        [JsonIgnore] 
        public Dialogue Dialogue { get; set; }

        /// <summary>
        ///     Is client or employee
        /// </summary>
        public Boolean IsClient { get; set; }

        /// <summary>
        ///     Value of average neutral tone
        /// </summary>
        public Double? NeutralityTone { get; set; }

        /// <summary>
        ///     Value of average positive tone
        /// </summary>
        public Double? PositiveTone { get; set; }

        /// <summary>
        ///     Value of average negative tone
        /// </summary>
        public Double? NegativeTone { get; set; }
    }
}