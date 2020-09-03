using ErrorKibanaScheduler.QuartzJob;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;
using System;

namespace ErrorKibanaScheduler
{
    public static class KibanaScheduler
    {
        public static void AddErrorMessageOnQuartzJob(this IServiceCollection services)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(KibanaErrorJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<KibanaErrorJob>()
                .WithIdentity("KibanaErrorJob.job", "KibanaError")
                .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                    .WithIdentity("KibanaErrorJob.trigger", "KibanaError")
                    .StartNow()
                    .WithSimpleSchedule(s => s.WithIntervalInHours(12).RepeatForever())
                   // .WithCronSchedule("0 0 10 * * ?", a => a.InTimeZone(TimeZoneInfo.Utc).Build())
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