using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions.Jobs;

namespace QuartzExtensions
{
    public static class ConfigureQuartz
    {
        public static void AddQuartz(this IServiceCollection services)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(CheckAudioRecognizeStatusJob), ServiceLifetime.Transient));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<CheckAudioRecognizeStatusJob>()
                .WithIdentity("Sample.job", "group1")
                .Build());

            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                    .WithIdentity($"Sample.trigger", "group1")
                    .StartNow()
                    .WithSimpleSchedule(s => s.WithIntervalInMinutes(2))
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
