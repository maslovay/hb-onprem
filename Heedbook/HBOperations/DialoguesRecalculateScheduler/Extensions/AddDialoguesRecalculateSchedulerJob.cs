using DialoguesRecalculateScheduler.QuartzJobs;
using DialoguesRecalculateScheduler.Settings;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;

namespace DialoguesRecalculateScheduler.Extensions
{
    public static class AddDialoguesRecalculateSchedulerJob
    {
        public static void AddDialoguesRecalculateScheduler(this IServiceCollection services, DialoguesRecalculateSchedulerSettings settings)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(DialoguesRecalculateSchedulerJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<DialoguesRecalculateSchedulerJob>()
                                                        .WithIdentity("DialoguesRecalculateScheduler.job", "Dialogues")
                                                        .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("DialoguesRecalculateScheduler.trigger", "Dialogues")
                                     .StartNow()
                                     .WithSimpleSchedule(s => s.WithIntervalInSeconds(settings.Period).RepeatForever())
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