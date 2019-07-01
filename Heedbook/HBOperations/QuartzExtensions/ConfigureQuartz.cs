using System;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using QuartzExtensions.Jobs;

namespace QuartzExtensions
{
    public static class ConfigureQuartz
    {
        public static void AddDeleteOldFilesQuartz(this IServiceCollection services)
        {
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(DeleteOldFilesJob), ServiceLifetime.Singleton));
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<DeleteOldFilesJob>()
                                                        .WithIdentity("DeleteOldFiles.job", "Files")
                                                        .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("DeleteOldFiles.trigger", "Files")
                                     .StartNow()
                                     .WithSimpleSchedule(s => s.WithIntervalInHours(1).RepeatForever())
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

        public static void AddSendNotMarckedImageCountQuartz(this IServiceCollection services)
        {            
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(SendNotMarckedImageCountJob),
                ServiceLifetime.Singleton));                
                
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();
            services.AddSingleton(provider => JobBuilder.Create<SendNotMarckedImageCountJob>()
                                                        .WithIdentity("SendNotMarckedImageCount.job", "Frames")
                                                        .Build());
                                                        
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("SendNotMarckedImageCount.trigger", "Frames")
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

        public static void AddSendOnlineTuiOfficesJobQuartz(this IServiceCollection services)
        {
            
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(SendOnlineTuiOfficesJob),
                ServiceLifetime.Singleton));   
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();         
            services.AddSingleton(provider => JobBuilder.Create<SendOnlineTuiOfficesJob>()
                                                        .WithIdentity("SendOnlineTuiOfficesJob.job", "Companys")
                                                        .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("SendOnlineTuiOfficesJob.trigger", "Companys")
                                     .StartNow()
                                     .WithCronSchedule("0 20 7 * * ?", a=>a.InTimeZone(TimeZoneInfo.Utc).Build())   
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
        
        public static void AddHeedbookDevelopmentStatisticsJobQuartz(this IServiceCollection services)
        {
            
            services.Add(new ServiceDescriptor(typeof(IJob), typeof(HeedbookDevelopmentStatisticsJob),
                ServiceLifetime.Singleton));   
            services.AddSingleton<IJobFactory, ScheduledJobFactory>();         
            services.AddSingleton(provider => JobBuilder.Create<HeedbookDevelopmentStatisticsJob>()
                                                        .WithIdentity("HeedbookDevelopmentStatisticsJob.job", "SelfStatistic")
                                                        .Build());
            services.AddSingleton(provider =>
            {
                return TriggerBuilder.Create()
                                     .WithIdentity("HeedbookDevelopmentStatisticsJob.trigger", "SelfStatistic")
                                     .StartNow()                                       
                                     .WithCronSchedule("0 00 7 * * ?", a=>a.InTimeZone(TimeZoneInfo.Utc).Build())                                    
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