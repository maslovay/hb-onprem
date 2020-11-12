using hb_asr_service.QuartzJob;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;

namespace hb_asr_service.Extensions
{
    public static class AddAudioRecognizeJob
    {
        public static void AddAudioRecognizeQuartz(this IServiceCollection services)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(AudioRecognizeJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();

            services.AddSingleton(provider => JobBuilder.Create<AudioRecognizeJob>()
                .WithIdentity("CheckAudioRecognizeStatus.job", "Audios")
                .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                    .WithIdentity("CheckAudioRecognizeStatus.trigger", "Audios")
                    .StartNow()
                    .WithSimpleSchedule(s => s.WithIntervalInMinutes(1).RepeatForever())
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