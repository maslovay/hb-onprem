using System;
using Quartz;
using Quartz.Spi;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using static TabletLoadTest.TabletLoad;

namespace TabletLoadTest
{
    public class RequestJobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public RequestJobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            var jobDetail = bundle.JobDetail;
            var job = (IJob)_serviceProvider.GetService(jobDetail.JobType);
            return job;
        }
        public void ReturnJob(IJob job)
        {
        }
    }
}