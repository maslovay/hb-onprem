﻿using HBLib;
using HBLib.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quartz;
using Configurations;
using RabbitMqEventBus;

namespace ErrorKibanaScheduler
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
            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<ElasticSettings>>().Value);
            services.Configure<MessengerSettings>(Configuration.GetSection(nameof(MessengerSettings)));
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<MessengerSettings>>().Value);
            services.Configure<UriPathOnKibana>(Configuration.GetSection(nameof(UriPathOnKibana)));
            services.AddSingleton(provider=> provider.GetRequiredService<IOptions<UriPathOnKibana>>().Value);
            services.AddSingleton<ElasticClientFactory>();
            services.AddSingleton<MessengerClient>();
            services.AddErrorMessageOnQuartzJob();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddRabbitMqEventBus(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env,IScheduler scheduler)
        {
            var service = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            scheduler.ScheduleJob(app.ApplicationServices.GetService<IJobDetail>(),
                app.ApplicationServices.GetService<ITrigger>());
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
