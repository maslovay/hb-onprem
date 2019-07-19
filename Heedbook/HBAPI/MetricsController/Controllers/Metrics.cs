using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetricsController.Controllers
{
    public class Metrics
    {
        public string VmName { get; set; }
        public DateTime Time { get; set; }        
        public List<Metricsprop> MetricsProp { get; set; }
    }
}
