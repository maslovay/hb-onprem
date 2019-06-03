using MemoryQueueSynchronizer.QuartzJobs;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;

namespace MemoryQueueSynchronizer.Extensions
{
    public static class AddMemoryQueueCompareJob
    {
        public static void AddMemoryQueueCompareQuartz(this IServiceCollection services)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(CheckMemoryAndDbConsistenceJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<CheckMemoryAndDbConsistenceJob>()
                                                        .WithIdentity("CheckMemoryAndDbConsistence.Job", "Check")
                                                        .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("CheckMemoryAndDbConsistence.trigger", "Check")
                                     .StartNow()
                                     .WithSimpleSchedule(s => s.WithIntervalInSeconds(50).RepeatForever())
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