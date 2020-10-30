using HBData;
using HBLib;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quartz;
using DetectFaceIdScheduler.Settings;
using DetectFaceIdScheduler.Extensions;
using DetectFaceIdScheduler.Services;
using DetectFaceIdScheduler.Utils;
using System;
using HBLib.Utils;

namespace DetectFaceIdScheduler
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            var schedulerSettings = new JobSettings()
            {
                Period = Configuration.GetSection(nameof(JobSettings)).GetValue<int>("Period")
            };

            services.AddDbContext<RecordsContext>
            (options =>
            {
                var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(HBData)));
            }, ServiceLifetime.Scoped);

            services.AddSingleton(provider => 
                {
                    var elasticSettings = new ElasticSettings
                    {
                        Host = Environment.GetEnvironmentVariable("ELASTIC_SETTINGS_HOST"),
                        Port = Int32.Parse(Environment.GetEnvironmentVariable("ELASTIC_SETTINGS_PORT")),
                        FunctionName = "OnPremUserService"
                    };
                    return elasticSettings;
                });
            services.AddTransient(provider =>
            {
                var settings = provider.GetRequiredService<ElasticSettings>();
                return new ElasticClient(settings);
            });
            
            services.AddSingleton<DetectFaceIdService>();

            services.Configure<DetectFaceIdSettings>(Configuration.GetSection(nameof(DetectFaceIdSettings)));
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<DetectFaceIdSettings>>().Value);

            services.AddDetectFaceIdSchedulerQuartz(schedulerSettings);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IScheduler scheduler)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            scheduler.ScheduleJob(app.ApplicationServices.GetService<IJobDetail>(),
                app.ApplicationServices.GetService<ITrigger>());

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
