using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;

namespace BenchmarkScheduler
{
    public static class AddQuartzJobOnBenchmarkFill
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
}
