using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class TargetMediaContent
    {
        public Guid TargetMediaContentId { get; set; }

        //group of target by age and gender 
        public virtual TargetGroup TargetGroup { get; set; }

        //media content
        public Guid? MediaContentId { get; set; }
        public virtual  MediaContent MediaContent { get; set; }

        //company
        public int CompanyId { get; set; }
        public virtual Company Company { get; set; }

        //sequence 
        public int SequenceNumber { get; set; }

        //start date-time (showing time period)
        public DateTime BegDate { get; set; }

        //end date-time (showing time period)
        public DateTime EndDate { get; set; }

        //interface
        public Guid? TargetMediaContentInterfaceId { get; set; }
        public virtual TargetMediaContentInterface TargetMediaContentInterface { get; set; }


        public virtual ICollection<DialogueTargetMediaContent> DialogueTargetMediaContent { get; set; }

    }
}
