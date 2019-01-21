using System;
using HBLib.Models;

namespace HBData.Models
{
    public class DialoguePhrasePlace
    {
        public int DialoguePhrasePlaceId { get; set; }

        //dialogue 
        public Guid? DialogueId { get; set; }
        public  Dialogue Dialogue { get; set; }


        //phrase 
        public int? PhraseId { get; set; }
        public  Phrase Phrase { get; set; }

        //WordPosition
        public int? WordPosition { get; set; }

        //Synonyn
        public bool Synonyn { get; set; }

        //Synonyn Text
        public string SynonynText { get; set; }

    }
}
