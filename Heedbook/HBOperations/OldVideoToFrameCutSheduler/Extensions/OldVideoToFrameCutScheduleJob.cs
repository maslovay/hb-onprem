using OldVideoToFrameCut.QuartzJobs;
using OldVideoToFrameCut.Settings;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;
using System;

namespace OldVideoToFrameCut.Extensions
{
    public static class AddOldVideoToFrameCutScheduleJob
    {
        public static void AddMarkUpQuartz(this IServiceCollection services, OldVideoToFrameCutSettings settings)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(OldVideoToFrameCutJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<OldVideoToFrameCutJob>()
                                                        .WithIdentity("OldVideoToFrameCut.job", "Dialogues")
                                                        .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("OldVideoToFrameCut.trigger", "Dialogues")
                                     .StartNow()
                                     .WithCronSchedule("0 00 5 * * ?", a=>a.InTimeZone(TimeZoneInfo.Utc).Build())  
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