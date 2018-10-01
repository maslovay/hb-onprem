using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class MediaContent
    {
        public Guid MediaContentId { get; set; }

        //file name
        public string FileName { get; set; }

        public virtual ICollection<TargetMediaContent> TargetMediaContent { get; set; }

    }
}
