using System;
using HBLib.Models;

namespace HBData.Models
{
    public class DialogueInterval
    {
        public Guid DialogueIntervalId { get; set; }

        //dialogue
        public Guid? DialogueId { get; set; }
        public  Dialogue Dialogue { get; set; }
		
		//client frame
		public bool IsClient { get; set; }

        //begining time of interval
        public DateTime BegTime { get; set; }
		
		//ending time of interval
        public DateTime EndTime { get; set; }
		
		//эмоция тона речи
		public double? NeutralityTone { get; set; }
        
        public double? HappinessTone { get; set; }
        
        public double? SadnessTone { get; set; }

        public double? AngerTone { get; set; }

        public double? FearTone { get; set; }
    }
}
