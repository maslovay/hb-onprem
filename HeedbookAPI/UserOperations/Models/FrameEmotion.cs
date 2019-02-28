using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;


namespace UserOperations.Models
{
    public class FrameEmotion
    {
        [Key]
        public Guid FrameEmotionId { get; set; }
        public Guid FileFrameId { get; set; }
        public FileFrame FileFrame { get; set; }
        public Double? AngerShare { get; set; }
        public Double? ContemptShare { get; set; }
        public Double? DisgustShare { get; set; }
        public Double? HappinessShare { get; set; }
        public Double? NeutralShare { get; set; }
        public Double? SadnessShare { get; set; }
        public Double? SurpriseShare { get; set; }
        public Double? FearShare { get; set; }
        public Double? YawShare { get; set; }
    }
}
