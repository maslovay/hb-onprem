using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlarmSender;
using AudioAnalyzeScheduler.Extensions;
using HbApiTester.Settings;
using HbApiTester.sqlite3;
using HbApiTester.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
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
            services.AddSingleton<Checker>();
            services.AddSingleton<DbOperations>();
            services.AddSingleton<TelegramSender>();
            
            services.AddHbApiTesterJobQuartz(hbApiSchedulerSettings);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IScheduler scheduler)
        {
            scheduler.ScheduleJob(app.ApplicationServices.GetService<IJobDetail>(),
                app.ApplicationServices.GetService<ITrigger>());
            app.ApplicationServices.GetService<CommandManager>().Start();

            app.UseSwagger(c => { c.RouteTemplate = "api/swagger/{documentName}/swagger.json"; });
            app.UseCors();
            app.UseMvc();
        }
    }
}