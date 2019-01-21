using System;
using HBLib.Models;

namespace HBData.Models
{
    public class DialoguePhrase
    {
        public int DialoguePhraseId { get; set; }

        //dialogue 
        public Guid? DialogueId { get; set; }
        public  Dialogue Dialogue { get; set; }

        //phrase 
        public int? PhraseId { get; set; }
        public  Phrase Phrase { get; set; }
		
		//client phrase
		public bool IsClient { get; set; }
    }
}
