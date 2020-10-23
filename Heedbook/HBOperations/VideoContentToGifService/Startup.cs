﻿using System;
using System.Diagnostics;
using Configurations;
using HBLib;
using HBLib.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;
using QuartzExtensions;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using VideoToGifService.Hander;

namespace VideoContentToGifService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddTransient<VideoContentToGif>();
            services.AddTransient<VideoContentToGifHandler>();
            services.AddTransient<SftpClient>();
            services.Configure<FFMpegSettings>(Configuration.GetSection(nameof(FFMpegSettings)));
            services.AddTransient<SftpSettings>(p => 
                {
                    var setting = new SftpSettings
                    {
                        Host = Environment.GetEnvironmentVariable("SFTP_CONNECTION_HOST"),
                        Port = Int32.Parse(Environment.GetEnvironmentVariable("SFTP_CONNECTION_PORT")),
                        UserName = Environment.GetEnvironmentVariable("SFTP_CONNECTION_USERNAME"),
                        Password = Environment.GetEnvironmentVariable("SFTP_CONNECTION_PASSWORD"),
                        DestinationPath = Environment.GetEnvironmentVariable("SFTP_CONNECTION_DESTINATIONPATH"),
                        DownloadPath = Environment.GetEnvironmentVariable("SFTP_CONNECTION_DOWNLOADPATH")
                    };
                    Debug.WriteLine($"{JsonConvert.SerializeObject(setting)}");
                    return setting;
                });
            services.AddTransient(provider => provider.GetRequiredService<IOptions<FFMpegSettings>>().Value);
            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddTransient(provider => provider.GetRequiredService<IOptions<ElasticSettings>>().Value);
            services.AddTransient(provider =>
                {
                    var settings = provider.GetRequiredService<IOptions<ElasticSettings>>().Value;
                    return new ElasticClient(settings);
                });
            services.AddTransient<FFMpegWrapper>();
            
            services.AddRabbitMqEventBusConfigFromEnv();
            services.AddDeleteOldFilesQuartz();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IScheduler scheduler)
        {
            var publisher = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            publisher.Subscribe<VideoContentToGifRun, VideoContentToGifHandler>();
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();
            var job = app.ApplicationServices.GetService<IJobDetail>();
            var trigger = app.ApplicationServices.GetService<ITrigger>();
            scheduler.ScheduleJob(job,
                trigger);
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}