using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class DialogueFrame
    {
        public Guid DialogueFrameId { get; set; }

        //dialogue
        public Guid? DialogueId { get; set; }
        public virtual Dialogue Dialogue { get; set; }
		
		//client frame
		public bool IsClient { get; set; }

        //время распознавания
        public DateTime Time { get; set; }

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

        //face yaw
        public double? YawShare { get; set; }

    }
}
