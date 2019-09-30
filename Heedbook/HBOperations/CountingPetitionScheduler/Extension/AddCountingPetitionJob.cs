using CountingPetitionScheduler.QuartzJob;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;

namespace CountingPetitionScheduler.Extension
{
    public static class AddCountingPetitionJob
    {
        public static void AddCountingPetitionOnQuartzJob(this IServiceCollection services)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(CountingPetitionJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<CountingPetitionJob>()
                .WithIdentity("CountingPetition.job", "CountingPetition")
                .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                    .WithIdentity("CountingPetitionJob.trigger", "CountingPetition")
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