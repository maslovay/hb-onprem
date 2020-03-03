using DeleteOldLogsOnElasticScheduler.QuartzJob;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;

namespace DeleteOldLogsOnElasticScheduler.Extension
{
    public static class AddDeleteOldLogsJob
    {
        public static void AddDeleteOldLogsOnQuartzJob(this IServiceCollection services)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(DeleteOldLogsJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<DeleteOldLogsJob>()
                .WithIdentity("DeleteOldLogsJob.job", "DeleteOldLogs")
                .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                    .WithIdentity("DeleteOldLogsJob.trigger", "DeleteOldLogs")
                    .StartNow()
                    .WithSimpleSchedule(s => s.WithIntervalInHours(24).RepeatForever())
                    .Build();
            });

            services.AddSingleton(provider =>
            {
                var schedulerFactory = new StdSchedulerFactory();
                var scheduler = schedulerFactory.GetScheduler().Result;
                scheduler.JobFactory = provider.GetService<IJobFactory>();
                scheduler.Start();
                return scheduler;
            });
        }
    }
}