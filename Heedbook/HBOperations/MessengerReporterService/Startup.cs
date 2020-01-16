using System.Collections.Generic;
using Configurations;
using HBLib;
using HBLib.Utils;
using MessengerReporterService.Handler;
using MessengerReporterService.Models;
using MessengerReporterService.Senders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quartz;
using QuartzExtensions;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace MessengerReporterService
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
            var hbApiSchedulerSettings = new HbApiTesterSettings()
            {
                CheckPeriodMin = Configuration.GetSection("HbApiTesterSettings").GetValue<int>("CheckPeriodMin"),
                ApiAddress =  Configuration.GetSection("HbApiTesterSettings").GetValue<string>("ApiAddress"),
                Password =  Configuration.GetSection("HbApiTesterSettings").GetValue<string>("Password"),
                User =  Configuration.GetSection("HbApiTesterSettings").GetValue<string>("User"),
                Tests = Configuration.GetSection("HbApiTesterSettings").GetSection("Tests").Get<string[]>(),
                Handlers = Configuration.GetSection("HbApiTesterSettings").GetSection("Handlers").Get<string[]>(),
                ExternalResources = Configuration.GetSection("HbApiTesterSettings").GetSection("ExternalResources").Get<Dictionary<string, string>>()
            };
            services.AddSingleton(hbApiSchedulerSettings);
            services.AddSingleton<TelegramSender>();
            services.AddOptions();
            services.AddTransient<MessengerReporter>();
            services.AddTransient<MessengerMessageRunHandler>();
            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddScoped(provider => provider.GetRequiredService<IOptions<ElasticSettings>>().Value);
            services.AddScoped<ElasticClientFactory>();
            services.AddRabbitMqEventBus(Configuration);
            services.AddDeleteOldFilesQuartz();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IScheduler scheduler)
        {
            var service = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            service.Subscribe<MessengerMessageRun, MessengerMessageRunHandler>();
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