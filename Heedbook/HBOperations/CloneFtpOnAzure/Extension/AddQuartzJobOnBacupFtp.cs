using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions;

namespace CloneFtpOnAzure.Extension
{
    public static class AddQuartzJobOnBacupFtp
    {
        public static void AddFtpFileOnQuartzJob(this IServiceCollection services)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(FtpJob),
                ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<FtpJob>()
                .WithIdentity("FtpJob.job", "FtpFile")
                .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                    .WithIdentity("FtpJob.trigger", "FtpFile")
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