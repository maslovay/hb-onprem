using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace UserOperations.Models
{
    /// <summary>
    /// Informations about words in dialogue
    /// </summary>
    public class DialogueWord
    {
        /// <summary>
        /// Dialogue word id
        /// </summary>
        [Key]
        public Guid DialogueWordId { get; set; }
		/// <summary>
        /// Dialogue id
        /// </summary>        
        public Guid? DialogueId { get; set; }
        public  Dialogue Dialogue { get; set; }
		/// <summary>
        /// Is client or employee
        /// </summary>
		public bool IsClient { get; set; }	
        /// <summary>
        /// Words in format: 
        /// ["Word": (text), "BegTime": (beg time of the word), "EndTime": (end time of the word), "PhraseId": (phrase id), "PhraseTypeId": (phrase type id)]
        /// </summary>
        public string Words { get; set; }
    }
}
