using MetricsController.QuartzJob;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuartzExtensions;


namespace MetricsController.Extensions
{
    public static class AddGetMetricsJob
    {
        public static void AddGetMetricsQuartz(this IServiceCollection services)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(GetMetricsJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<GetMetricsJob>()
                .WithIdentity("GetMetrics.job", "Metrics")
                .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                    .WithIdentity("GetMetrics.trigger", "Metrics")
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
