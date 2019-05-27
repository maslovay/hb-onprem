using DialogueMarkUp.QuartzJobs;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;

namespace DialogueMarkUp.Extensions
{
    public static class AddDialogueMarkUpScheduleJob
    {
        public static void AddMarkUpQuartz(this IServiceCollection services)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(CheckDialogueMarkUpJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<CheckDialogueMarkUpJob>()
                                                        .WithIdentity("CheckDialogueMarkUp.job", "Dialogues")
                                                        .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("CheckDialogueMarkUp.trigger", "Dialogues")
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