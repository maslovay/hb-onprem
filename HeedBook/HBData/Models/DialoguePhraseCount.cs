using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;


namespace HBData.Models
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
        public  Dialogue Dialogue { get; set; }
        /// <summary>
        /// Phrase type
        /// </summary>
        public Guid? PhraseTypeId { get; set; }
        public  PhraseType PhrType { get; set; }
        /// <summary>
        /// Phrase numbers
        /// </summary>
        public Guid? PhraseCount { get; set; }
        /// <summary>
        /// Is client or employee
        /// </summary>
		public bool IsClient { get; set; }
    }
}
