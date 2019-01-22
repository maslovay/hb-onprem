using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBData.Models
{
    public class DialogueAudio
    {
        [Key]
        public Guid DialogueAudioId { get; set; }
		
		//dialogue
		public Guid? DialogueId { get; set; }
        public  Dialogue Dialogue { get; set; }
		
		//client or employee
		public bool IsClient { get; set; }
		
        //тон голоса
        public double? NeutralityTone { get; set; }

        public double? PositiveTone { get; set; }

        public double? NegativeTone { get; set; }

    }
}
