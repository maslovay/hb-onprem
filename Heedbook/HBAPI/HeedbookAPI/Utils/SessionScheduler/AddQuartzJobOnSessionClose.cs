using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using UserOperations.Utils;

namespace UserOperations.Services.Scheduler
{
    public static class AddQuartzJobOnSessionClose
    {
        public static void AddSessionCloseQuartzJob(this IServiceCollection services)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(SessionCloseJob),
                ServiceLifetime.Singleton));

            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<SessionCloseJob>()
                                                        .WithIdentity("SessionCloseJob.job")
                                                        .Build());

            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("SessionCloseJob.trigger")
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
