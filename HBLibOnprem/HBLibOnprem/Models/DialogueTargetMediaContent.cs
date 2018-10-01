using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class DialogueTargetMediaContent
    {
        public Guid DialogueTargetMediaContentId { get; set; }

        //dialogue 
        public Guid? DialogueId { get; set; }
        public virtual Dialogue Dialogue { get; set; }

        //Target Media Content
        public Guid? TargetMediaContentId { get; set; }
        public virtual TargetMediaContent TargetMediaContent { get; set; }

        //start time of demonstration
        public DateTime BegTime { get; set; }

        //start time of demonstration
        public DateTime EndTime { get; set; }

        //reaction
        public string Reaction { get; set; }

        //attention
        public double? AttentionShare { get; set; }
    }
}
