﻿using CognitiveService.Handlers;
using CognitiveService.Legacy;
using Configurations;
using HBLib;
using HBLib.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Base;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using RabbitMqEventBus.Models;

namespace CognitiveService
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
            services.Configure<SftpSettings>(Configuration.GetSection(nameof(SftpSettings)));
            services.AddTransient<SftpClient>( provider =>
            {
                var opt = provider.GetRequiredService<SftpSettings>();
                return new SftpClient(opt);
            });
            services.AddRabbitMqEventBus(Configuration);
            services.AddTransient<FrameSubFaceReq>();
            services.AddTransient<FaceRecognitionMessageHandler>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }
        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            SubscribeEvents(app);
            app.UseHttpsRedirection();
            app.UseMvc();
        }

        private void SubscribeEvents(IApplicationBuilder app)
        {
            var service = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            service.Subscribe<FaceRecognitionRun, FaceRecognitionMessageHandler>();
        }
    }
}