using System.Threading;
using DialogueStatusCheckerScheduler.Settings;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;
using QuartzExtensions.Jobs;

namespace DialogueStatusCheckerScheduler.Extensions
{
    public static class AddDialogueStatusCheckerScheduler
    {
        public static void AddDialogueStatusCheckerQuartz(this IServiceCollection services, DialogueStatusCheckerSchedulerSettings settings)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(DialogueStatusCheckerJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<DialogueStatusCheckerJob>()
                                                        .WithIdentity("DialogueStatusChecker.job", "Dialogues")
                                                        .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("DialogueStatusChecker.trigger", "Dialogues")
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