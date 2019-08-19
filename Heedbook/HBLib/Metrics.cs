using System;
using System.Collections.Generic;

namespace HBLib
{
    public class Metrics
    {
        public string VmName { get; set; }
        public DateTime Time { get; set; }    
        
        public List<MetricValue> MetricValues { get; set; }
    }
}
