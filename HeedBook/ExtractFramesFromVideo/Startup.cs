﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Configurations;
using ExtractFramesFromVideo.Handlers;
using HBData;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Base;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace ExtractFramesFromVideo
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
            services.AddDbContext<RecordsContext>(options => options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));
            services.Configure<SftpSettings>(Configuration.GetSection(nameof(SftpSettings)));
            services.AddTransient(provider =>
            {
                var sftpSettings = provider.GetRequiredService<IOptions<SftpSettings>>().Value;
                return new SftpClient(sftpSettings);
            });
            services.AddScoped<IGenericRepository, GenericRepository>();
            services.AddTransient<FramesFromVideo>();
            services.AddRabbitMqEventBus(Configuration);
            services.AddTransient<FramesFromVideoMessageHandler>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.ApplicationServices.GetRequiredService<INotificationService>();
            var service = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            service.Subscribe<FramesFromVideoRun, FramesFromVideoMessageHandler>();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
