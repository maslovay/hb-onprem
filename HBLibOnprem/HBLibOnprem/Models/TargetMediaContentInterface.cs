using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HBLib.Models
{
    public class TargetMediaContentInterface
    {
        public Guid TargetMediaContentInterfaceId { get; set; }

        //message in interface
        public string Message { get; set; }

        //interactive button|link
        public string Button { get; set; }

        //screen place 1 of 6 ()
        public int Place { get; set; }

    }
}
