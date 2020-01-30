using DetectFaceIdScheduler.Settings;
using DetectFaceIdScheduler.QuartzJobs;
using DetectFaceIdScheduler.Settings;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;

namespace DetectFaceIdScheduler.Extensions
{
    public static class AddDetectFaceIdSchedulerJob
    {
        public static void AddDetectFaceIdSchedulerQuartz(this IServiceCollection services, JobSettings settings)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(DetectFaceIdJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<DetectFaceIdJob>()
                                                        .WithIdentity("DetectFaceId.job", "Dialogues")
                                                        .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("DetectFaceId.trigger", "Dialogues")
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