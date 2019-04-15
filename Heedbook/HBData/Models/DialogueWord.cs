using System;
using System.ComponentModel.DataAnnotations;

namespace HBData.Models
{
    /// <summary>
    ///     Informations about words in dialogue
    /// </summary>
    public class DialogueWord
    {
        /// <summary>
        ///     Dialogue word id
        /// </summary>
        [Key]
        public Guid DialogueWordId { get; set; }

        /// <summary>
        ///     Dialogue id
        /// </summary>
        public Guid? DialogueId { get; set; }

        public Dialogue Dialogue { get; set; }

        /// <summary>
        ///     Is client or employee
        /// </summary>
        public Boolean IsClient { get; set; }

        /// <summary>
        ///     Words in format:
        ///     ["Word": (text), "BegTime": (beg time of the word), "EndTime": (end time of the word), "PhraseId": (phrase id),
        ///     "PhraseTypeId": (phrase type id)]
        /// </summary>
        public String Words { get; set; }
    }
}