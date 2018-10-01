using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class DialoguePhrase
    {
        public int DialoguePhraseId { get; set; }

        //dialogue 
        public Guid? DialogueId { get; set; }
        public virtual Dialogue Dialogue { get; set; }

        //phrase 
        public int? PhraseId { get; set; }
        public virtual Phrase Phrase { get; set; }
		
		//client phrase
		public bool IsClient { get; set; }
    }
}
