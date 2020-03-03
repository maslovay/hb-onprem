using DetectFaceIdExtendedScheduler.Settings;
using DetectFaceIdExtendedScheduler.QuartzJobs;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;

namespace DetectFaceIdExtendedScheduler.Extensions
{
    public static class AddDetectFaceIdExtendedSchedulerJob
    {
        public static void AddDetectFaceIdExtendedSchedulerQuartz(this IServiceCollection services, JobSettings settings)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(DetectFaceIdJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<DetectFaceIdJob>()
                                                        .WithIdentity("DetectFaceIdExtended.job", "Dialogues")
                                                        .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("DetectFaceIdExtended.trigger", "Dialogues")
                                     .StartNow()
                                     .WithSimpleSchedule(s => s.WithIntervalInSeconds(settings.Period).RepeatForever())
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