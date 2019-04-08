using System;
using System.Collections.Generic;

namespace FillingHintService.Model
{
    public class HintCondition
    {
        public List<String> Indexes { get; set; }
        public List<Condition> Condition { get; set; }
        public String Table { get; set; }
        public String Type { get; set; }
        public Boolean IsPositive { get; set; }
        public String Operation { get; set; }
        public Double Min { get; set; }
        public Double Max { get; set; }
    }
}