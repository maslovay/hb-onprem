using AudioAnalyzeScheduler.QuartzJobs;
using AudioAnalyzeScheduler.Settings;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;

namespace AudioAnalyzeScheduler.Extensions
{
    public static class AddAudioAnalyzeSchedulerJob
    {
        public static void AddAudioRecognizeQuartz(this IServiceCollection services, AudioAnalyseSchedulerSettings settings)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(CheckAudioRecognizeStatusJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<CheckAudioRecognizeStatusJob>()
                                                        .WithIdentity("CheckAudioRecognizeStatus.job", "Audios")
                                                        .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("CheckAudioRecognizeStatus.trigger", "Audios")
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