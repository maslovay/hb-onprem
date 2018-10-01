using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class DialoguePhrasePlace
    {
        public int DialoguePhrasePlaceId { get; set; }

        //dialogue 
        public Guid? DialogueId { get; set; }
        public virtual Dialogue Dialogue { get; set; }


        //phrase 
        public int? PhraseId { get; set; }
        public virtual Phrase Phrase { get; set; }

        //WordPosition
        public int? WordPosition { get; set; }

        //Synonyn
        public bool Synonyn { get; set; }

        //Synonyn Text
        public string SynonynText { get; set; }

    }
}
