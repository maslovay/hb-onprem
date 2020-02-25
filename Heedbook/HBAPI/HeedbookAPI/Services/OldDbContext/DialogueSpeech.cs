using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Old.Models
{
    /// <summary>
    ///     Information about speech
    /// </summary>
    public class DialogueSpeech
    {
        /// <summary>
        ///     Speech id
        /// </summary>
        [Key]
        public Guid DialogueSpeechId { get; set; }

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
        ///     Text positive share (Microsoft)
        /// </summary>
        public Double? PositiveShare { get; set; }

        /// <summary>
        ///     Speech speed (sounds per second)
        /// </summary>
        public Double? SpeechSpeed { get; set; }

        /// <summary>
        ///     Silence share
        /// </summary>
        public Double? SilenceShare { get; set; }
    }
}