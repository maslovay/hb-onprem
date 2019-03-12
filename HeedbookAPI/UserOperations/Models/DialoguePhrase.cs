using System;
using System.ComponentModel.DataAnnotations;


namespace UserOperations.Models
{
    /// <summary>
    /// Dialogue phrase info
    /// </summary>
    public class DialoguePhrase
    {
        /// <summary>
        /// Phrase count id
        /// </summary>
        [Key]
        public Guid DialoguePhraseId { get; set; }
        /// <summary>
        /// Dialogue id
        /// </summary>
        public Guid? DialogueId { get; set; }
        public Dialogue Dialogue { get; set; }
        /// <summary>
        /// Phrase type
        /// </summary>
        public Guid? PhraseTypeId { get; set; }
        public PhraseType PhraseType { get; set; }
        /// <summary>
        public Guid? PhraseId {get; set;}
        public Phrase Phrase {get; set;}

        /// <summary>
        /// Is client or employee
        /// </summary>
		public bool IsClient { get; set; }
    }
}
