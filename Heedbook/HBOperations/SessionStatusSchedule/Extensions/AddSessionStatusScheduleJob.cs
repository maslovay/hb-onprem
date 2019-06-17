using SessionStatusSchedule.QuartzJobs;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;

namespace SessionStatusSchedule.Extensions
{
    public static class AddSessionStatusScheduleJob
    {
        public static void AddSessionStatusScheduleQuartz(this IServiceCollection services)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(CheckSessionStatusScheduleJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<CheckSessionStatusScheduleJob>()
                                                        .WithIdentity("CheckSessionStatusSchedule.job", "Dialogues")
                                                        .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("CheckDSessionStatusSchedule.trigger", "Dialogues")
                                     .StartNow()
                                     .WithSimpleSchedule(s => s.WithIntervalInMinutes(30).RepeatForever())
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