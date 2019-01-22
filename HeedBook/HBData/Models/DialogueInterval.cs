using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBData.Models
{
    public class DialogueInterval
    {
        [Key]
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
