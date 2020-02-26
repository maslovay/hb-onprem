using OldVideoToFrameExtract.QuartzJobs;
using OldVideoToFrameExtract.Settings;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;
using System;

namespace OldVideoToFrameExtract.Extensions
{
    public static class AddOldVideoToFrameExtractScheduleJob
    {
        public static void AddMarkUpQuartz(this IServiceCollection services, OldVideoToFrameExtractSettings settings)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(OldVideoToFrameExtractJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<OldVideoToFrameExtractJob>()
                                                        .WithIdentity("OldVideoToFrameExtract.job", "Dialogues")
                                                        .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("OldVideoToFrameExtract.trigger", "Dialogues")
                                     .StartNow()
                                     //.WithCronSchedule("0 00 5 * * ?", a=>a.InTimeZone(TimeZoneInfo.Utc).Build())  
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