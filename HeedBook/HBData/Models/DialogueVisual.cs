using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBData.Models
{
    public class DialogueVisual
    {
        [Key]
        public Guid DialogueVisualId { get; set; }
		
		//dialogue
		public Guid? DialogueId { get; set; }
        public  Dialogue Dialogue { get; set; }
				
        //% внимания
        public double? AttentionShare { get; set; }

        //% эмоций (по названиям)
        public double? HappinessShare { get; set; }

        //% эмоций (по названиям)
        public double? NeutralShare { get; set; }

        //% эмоций (по названиям)
        public double? SurpriseShare { get; set; }

        //% эмоций (по названиям)
        public double? SadnessShare { get; set; }

        //% эмоций (по названиям)
        public double? AngerShare { get; set; }

        //% эмоций (по названиям)
        public double? DisgustShare { get; set; }

        //% эмоций (по названиям)
        public double? ContemptShare { get; set; }

        //% эмоций (по названиям)
        public double? FearShare { get; set; }
		
    }
}
