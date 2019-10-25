using System;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace UserOperations.Services.Scheduler
{
    public static class AddQuartzJobOnBenchmarksFill
    {
        public static void AddBenchmarkFillQuartzJob(this IServiceCollection services)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(BenchmarkJob),
                ServiceLifetime.Singleton));

            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<BenchmarkJob>()
                                                        .WithIdentity("BenchmarkJob.job")
                                                        .Build());

            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("BenchmarkJob.trigger")
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


    public class ScheduledJobFactory : IJobFactory
    {
        private readonly IServiceProvider serviceProvider;

        public ScheduledJobFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return serviceProvider.GetService(typeof(IJob)) as IJob;
        }

        public void ReturnJob(IJob job)
        {
            var disposable = job as IDisposable;
            if (disposable != null) disposable.Dispose();
        }
    }
}
