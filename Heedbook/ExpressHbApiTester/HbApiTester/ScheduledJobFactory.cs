using System;
using HbApiTester.QuartzJobs;
using Quartz;
using Quartz.Spi;

namespace QuartzExtensions
{
    public class ScheduledJobFactory : IJobFactory
    {
        private readonly IServiceProvider serviceProvider;

        public ScheduledJobFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            if ( bundle.JobDetail.Key.Name == "DelayedTestResultCheckJob.job" )
                return serviceProvider.GetService(typeof(DelayedTestResultCheckJob)) as IJob;
            
            return serviceProvider.GetService(typeof(HbApiTesterJob)) as IJob;
        }

        public void ReturnJob(IJob job)
        {
            var disposable = job as IDisposable;
            if (disposable != null) disposable.Dispose();
        }
    }
}