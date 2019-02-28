using System;
using System.ComponentModel.DataAnnotations;


namespace UserOperations.Models
{
    /// <summary>
    /// Phrase number of each type in dialogue
    /// </summary>
    public class DialoguePhraseCount
    {
        /// <summary>
        /// Phrase count id
        /// </summary>
        [Key]
        public Guid DialoguePhraseCountId { get; set; }
        /// <summary>
        /// Dialogue id
        /// </summary>
        public Guid? DialogueId { get; set; }
        public Dialogue Dialogue { get; set; }
        /// <summary>
        /// Phrase type
        /// </summary>
        public Guid? PhraseTypeId { get; set; }
        public PhraseType PhrType { get; set; }
        /// <summary>
        /// Phrase numbers
        /// </summary>
        public Int32 PhraseCount { get; set; }
        /// <summary>
        /// Is client or employee
        /// </summary>
		public bool IsClient { get; set; }
    }
}
