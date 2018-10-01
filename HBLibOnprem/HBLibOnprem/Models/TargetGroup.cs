using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class TargetGroup
    {
        public Guid TargetGroupId { get; set; }

        //group name
        public string GroupName { get; set; }

        //client gender 
        public string Gender { get; set; }

        //client age start
        public int BegAge { get; set; }

        //client age end
        public int EndAge { get; set; }
    }
}
