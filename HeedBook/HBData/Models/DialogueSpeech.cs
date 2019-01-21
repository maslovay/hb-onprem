using System;
using HBLib.Models;

namespace HBData.Models
{
    public class DialogueSpeech
    {
        public Guid DialogueSpeechId { get; set; }
		
		//dialogue
		public Guid? DialogueId { get; set; }
        public  Dialogue Dialogue { get; set; }
		
		//client or employee
		public bool IsClient { get; set; }
		
        //positive share
        public double? PositiveShare { get; set; }

        //speech speed
        public double? SpeechSpeed { get; set; }

        //Silence share
        public double? SilenceShare { get; set; }

    }
}
