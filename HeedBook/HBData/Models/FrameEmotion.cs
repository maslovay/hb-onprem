using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;


namespace HBData.Models
{
    public class FrameEmotion
    {
        [Key]
        public Guid FrameEmotionId { get; set; }
        public Guid FileFrameId { get; set; }
        public FileFrame FileFrame { get; set; }
        public double? AngerShare { get; set; }
        public double? ContemptShare { get; set; }
        public double? DisgustShare { get; set; }
        public double? HappinessShare { get; set; }
        public double? NeutralShare { get; set; }
        public double? SadnessShare { get; set; }
        public double? SurpriseShare { get; set; }
        public double? FearShare { get; set; }
        public double? YawShare { get; set; }
    }
}
