using System.Collections.Generic;
using Configurations;
using HBLib;
using HBLib.Utils;
using IntegrationAPITestsService;
using IntegrationAPITestsService.CommandHandler;
using IntegrationAPITestsService.Handler;
using IntegrationAPITestsService.Models;
using IntegrationAPITestsService.Publishers;
using IntegrationAPITestsService.sqlite3;
using IntegrationAPITestsService.Tasks;
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
            services.AddSingleton<CommandManager>();
            services.AddSingleton<ApiTestsRunner>();
            services.AddSingleton<DelayedTestsRunner>();
            services.AddSingleton<ExternalResourceTestsRunner>();
            services.AddSingleton<Checker>();
            services.AddSingleton<DbOperations>();
            services.AddSingleton<ResultsPublisher>();
            services.AddSingleton<LogsPublisher>();
            services.AddTransient<IntegrationTests>();
            services.AddTransient<IntegrationTestsRunHandler>();
            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddScoped(provider => provider.GetRequiredService<IOptions<ElasticSettings>>().Value);
            services.AddScoped<ElasticClientFactory>();
            services.AddTransient(provider => provider.GetRequiredService<IOptions<SftpSettings>>().Value);
            services.AddRabbitMqEventBus(Configuration);
            services.AddDeleteOldFilesQuartz();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IScheduler scheduler)
        {
            var publisher = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            publisher.Subscribe<IntegrationTestsRun, IntegrationTestsRunHandler>();
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