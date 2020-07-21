using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;

namespace RemoveSlideShowSession
{
    public static class RemoveSlideShowScheduleJob
    {
        public static void AddRemoveSlideShowQuartz(this IServiceCollection services)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(RemoveSlideShowSessionJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<RemoveSlideShowSessionJob>()
                                                        .WithIdentity("RemoveSlideShowSession.job", "SlideShowSessions")
                                                        .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("RemoveSlideShowSession.trigger", "SlideShowSessions")
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