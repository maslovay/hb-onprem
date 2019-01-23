using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;


namespace HBData.Models
{
    public class DialoguePhraseCount
    {
        [Key]
        public int DialoguePhraseCountId { get; set; }

        //dialogue 
        public Guid? DialogueId { get; set; }
        public  Dialogue Dialogue { get; set; }

        //phrase 
        public int? PhraseTypeId { get; set; }
        public  PhraseType PhrType { get; set; }

        //Phrase Count
        public int? PhraseCount { get; set; }

		//client phrase
		public bool IsClient { get; set; }
    }
}
