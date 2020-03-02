using DialogueAndSessionsNested.QuartzJobs;
using DialogueAndSessionsNested.Settings;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;

namespace DialogueAndSessionsNested.Extensions
{
    public static class AddDialogueMarkUpScheduleJob
    {
        public static void AddMarkUpQuartz(this IServiceCollection services, DialogueAndSessionsNestedSettings settings)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(DialogueAndSessionsNestedJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<DialogueAndSessionsNestedJob>()
                                                        .WithIdentity("DialogueAndSessionsNested.job", "Dialogues")
                                                        .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("DialogueAndSessionsNested.trigger", "Dialogues")
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