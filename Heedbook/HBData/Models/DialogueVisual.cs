using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HBData.Models
{
    /// <summary>
    ///     Average dialogue client emotions
    /// </summary>
    public class DialogueVisual
    {
        /// <summary>
        ///     Dialogue visual id
        /// </summary>
        [Key]
        public Guid DialogueVisualId { get; set; }

        /// <summary>
        ///     Dialogue id
        /// </summary>
        public Guid? DialogueId { get; set; }
        [JsonIgnore]
        public Dialogue Dialogue { get; set; }

        /// <summary>
        ///     Average attention share
        /// </summary>
        public Double? AttentionShare { get; set; }

        /// <summary>
        ///     Average dialogue happiness share
        /// </summary>
        public Double? HappinessShare { get; set; }

        /// <summary>
        ///     Average dialogue neutral share
        /// </summary>
        public Double? NeutralShare { get; set; }

        /// <summary>
        ///     Average dialogue surprise share
        /// </summary>
        public Double? SurpriseShare { get; set; }

        /// <summary>
        ///     Average dialogue sadness share
        /// </summary>
        public Double? SadnessShare { get; set; }

        /// <summary>
        ///     Average dialogue anger share
        /// </summary>
        public Double? AngerShare { get; set; }

        /// <summary>
        ///     Average dialogue disgust share
        /// </summary>
        public Double? DisgustShare { get; set; }

        /// <summary>
        ///     Average dialogue disgust share
        /// </summary>
        public Double? ContemptShare { get; set; }

        /// <summary>
        ///     Average dialogue fear share
        /// </summary>
        public Double? FearShare { get; set; }
    }
}