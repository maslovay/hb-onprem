using System;
using System.Collections.Generic;
using AlarmSender;
using AudioAnalyzeScheduler.Extensions;
using HbApiTester.Settings;
using HbApiTester.sqlite3;
using HbApiTester.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Swashbuckle.AspNetCore.Swagger;
using ILogger = NLog.ILogger;

namespace HbApiTester
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
            => Configuration = configuration;
        
        private IConfiguration Configuration { get; }

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
            
           
            var logger = NLog.LogManager.GetLogger("HbApiTester");

            services.AddSingleton<ILogger>(logger);
            services.AddSingleton(hbApiSchedulerSettings);
            //services.AddSingleton(delayedSchedulerSettings);
            services.AddSingleton<ApiTestsRunner>();
            services.AddSingleton<DelayedTestsRunner>();
            services.AddSingleton<ExternalResourceTestsRunner>();
            services.AddSingleton<CommandManager>();
            services.AddSingleton<ResultsPublisher>();
            services.AddSingleton<LogsPublisher>();
            services.AddSingleton<Checker>();
            services.AddSingleton<DbOperations>();
            services.AddSingleton<TelegramSender>();
            services.AddSingleton<ResultsPublisher>();
            
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Title = "User Service Api",
                    Version = "v1"
                });
            });
            
            services.AddHbApiTesterJobQuartz(hbApiSchedulerSettings);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IScheduler scheduler)
        {
            scheduler.ScheduleJob(app.ApplicationServices.GetService<IJobDetail>(),
                app.ApplicationServices.GetService<ITrigger>());
            app.ApplicationServices.GetService<CommandManager>().Start();

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();
           
            app.UseSwagger(c => { c.RouteTemplate = "api/swagger/{documentName}/swagger.json"; });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/api/swagger/v1/swagger.json", "ExpressTest API");
                c.RoutePrefix = String.Empty;
            });
            
            app.UseCors();
            app.UseMvc();
        }
    }
}