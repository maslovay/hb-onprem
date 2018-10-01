using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class DialoguePhraseCount
    {
        public int DialoguePhraseCountId { get; set; }

        //dialogue 
        public Guid? DialogueId { get; set; }
        public virtual Dialogue Dialogue { get; set; }

        //phrase 
        public int? PhraseTypeId { get; set; }
        public virtual PhraseType PhrType { get; set; }

        //Phrase Count
        public int? PhraseCount { get; set; }

		//client phrase
		public bool IsClient { get; set; }
    }
}
