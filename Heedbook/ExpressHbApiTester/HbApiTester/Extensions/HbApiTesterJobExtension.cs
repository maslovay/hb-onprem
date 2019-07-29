using HbApiTester.QuartzJobs;
using HbApiTester.Settings;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;

namespace AudioAnalyzeScheduler.Extensions
{
    public static class HbApiTesterJobExtension
    {
        public static void AddHbApiTesterJobQuartz(this IServiceCollection services, HbApiTesterSettings settings)
        {
            if (settings.CheckPeriodMin < 0)
                return;
            
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(HbApiTesterJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();

            services.AddSingleton<HbApiTesterJob>();
            services.AddSingleton<DelayedTestResultCheckJob>();

            var delayedTestJob = JobBuilder.Create<DelayedTestResultCheckJob>()
                .WithIdentity("DelayedTestResultCheckJob.job", "DelayedTestResultCheckJob")
                .Build();

            var hbApiTest = JobBuilder.Create<HbApiTesterJob>()
                .WithIdentity("HbApiTester.job", "HbApiTest")
                .Build();

          //  services.AddSingleton(delayedTestJob)
            
            var shortTrigger = TriggerBuilder.Create()
                    .WithIdentity("DelayedTestResultCheckJob.trigger", "DelayedTestResultCheckJob")
                    .StartNow()
                    .WithSimpleSchedule(s => s.WithIntervalInMinutes(5).RepeatForever())
                    .Build();
            
            var longTrigger = TriggerBuilder.Create()
                    .WithIdentity("HbApiTester.trigger", "HbApiTest")
                    .StartNow()
                    .WithSimpleSchedule(s => s.WithIntervalInMinutes(settings.CheckPeriodMin).RepeatForever())
                    .Build();

            services.AddSingleton(provider =>
            {
                var schedulerFactory = new StdSchedulerFactory();
                var scheduler = schedulerFactory.GetScheduler().Result;
                scheduler.JobFactory = provider.GetService<IJobFactory>();
                scheduler.ScheduleJob(hbApiTest, longTrigger);
                scheduler.ScheduleJob(delayedTestJob, shortTrigger);
                scheduler.Start();
                return scheduler;
            });
        }
    }
}