using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions.Jobs;

namespace QuartzExtensions
{
    public static class ConfigureQuartz
    {
        public static void AddDeleteOldFilesQuartz(this IServiceCollection services)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(DeleteOldFilesJob), ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<DeleteOldFilesJob>()
                                                        .WithIdentity("DeleteOldFiles.job", "Files")
                                                        .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("DeleteOldFiles.trigger", "Files")
                                     .StartNow()
                                     .WithSimpleSchedule(s => s.WithIntervalInHours(1).RepeatForever())
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