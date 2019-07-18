using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetricsController.QuartzJob
{
    public class GetMetricsJob : IJob
    {
        private AzureConnector _connector;
        public GetMetricsJob(AzureConnector connector)
        {
            _connector = connector;
        }
        public void Execute(IJobExecutionContext context)
        {
            _connector.GetMetrics();
        }
    }
}
