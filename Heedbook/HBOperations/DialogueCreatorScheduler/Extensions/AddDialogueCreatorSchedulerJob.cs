using DialogueCreatorScheduler.QuartzJobs;
using DialogueCreatorScheduler.Settings;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;

namespace DialogueCreatorScheduler.Extensions
{
    public static class AddDialogueCreatorSchedulerJob
    {
        public static void AddDialogueCreatorSchedulerQuartz(this IServiceCollection services, JobSettings settings)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(DialogueCreatorSchedulerJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<DialogueCreatorSchedulerJob>()
                                                        .WithIdentity("DialogueCreatorScheduler.job", "Dialogues")
                                                        .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("DialogueCreatorScheduler.trigger", "Dialogues")
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