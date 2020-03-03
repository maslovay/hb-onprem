using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Old.Models
{
    /// <summary>
    ///     Dialogue phrase info
    /// </summary>
    public class DialoguePhrase
    {
        /// <summary>
        ///     Phrase count id
        /// </summary>
        [Key]
        public Guid DialoguePhraseId { get; set; }

        /// <summary>
        ///     Dialogue id
        /// </summary>
        public Guid? DialogueId { get; set; }
        [JsonIgnore]
        public Dialogue Dialogue { get; set; }

        /// <summary>
        ///     Phrase type
        /// </summary>
        public Guid? PhraseTypeId { get; set; }

        public PhraseType PhraseType { get; set; }

        /// <summary>
        public Guid? PhraseId { get; set; }

        public Phrase Phrase { get; set; }

        /// <summary>
        ///     Is client or employee
        /// </summary>
        public Boolean IsClient { get; set; }
    }
}