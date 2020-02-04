using System.Collections.Generic;
using Configurations;
using HBLib;
using HBLib.Utils;
using UnitAPITestsService;
using UnitAPITestsService.CommandHandler;
using UnitAPITestsService.Handler;
using UnitAPITestsService.Tasks;
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

namespace IntegrationAPITestsService
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
            services.AddSingleton<CommandManager>();
            services.AddSingleton<Checker>();
            services.AddTransient<UnitTests>();
            services.AddTransient<UnitAPITestsRunHandler>();
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
            var publisher = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            publisher.Subscribe<UnitAPITestsRun, UnitAPITestsRunHandler>();
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();
            // var job = app.ApplicationServices.GetService<IJobDetail>();
            // var trigger = app.ApplicationServices.GetService<ITrigger>();
            // scheduler.ScheduleJob(job,
            //     trigger);
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}