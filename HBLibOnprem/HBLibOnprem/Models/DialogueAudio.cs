using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class DialogueAudio
    {
        public Guid DialogueAudioId { get; set; }
		
		//dialogue
		public Guid? DialogueId { get; set; }
        public virtual Dialogue Dialogue { get; set; }
		
		//client or employee
		public bool IsClient { get; set; }
		
        //тон голоса
        public double? NeutralityTone { get; set; }

        public double? PositiveTone { get; set; }

        public double? NegativeTone { get; set; }

    }
}
