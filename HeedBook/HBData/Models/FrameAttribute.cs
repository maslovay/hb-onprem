using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;


namespace HBData.Models
{
    public class FrameAttribute
    {
        [Key]
        public Guid FrameAttributeId { get; set; }
        public Guid FileFrameId { get; set; }
        public FileFrame FileFrame { get; set; }
        public string Gender { get; set; }
        public double Age { get; set; }
    }
}
