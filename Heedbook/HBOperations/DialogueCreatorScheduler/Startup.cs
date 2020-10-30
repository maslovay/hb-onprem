 
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
using Notifications.Base;
using DialogueCreatorScheduler.Settings;
using DialogueCreatorScheduler.Extensions;
using Quartz;
using HBLib.Utils;
using DialogueCreatorScheduler.Services;
using Configurations;
using DialogueCreatorScheduler.Service;
using DialogueCreatorScheduler.Models;
using System;

namespace DialogueCreatorScheduler
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

            services.Configure<DialogueSettings>(Configuration.GetSection(nameof(DialogueSettings)));
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<DialogueSettings>>().Value);

            services.AddSingleton<PersonDetectionService>();
            services.AddSingleton<DialogueCreatorService>();
            services.AddSingleton<FaceIntervalsService>();
            services.AddSingleton<DialogueSavingService>();

            services.AddRabbitMqEventBusConfigFromEnv();

            services.AddDialogueCreatorSchedulerQuartz(schedulerSettings);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddSingleton<DescriptorCalculations>();
        }

        

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IScheduler scheduler)
        {
            var service = app.ApplicationServices.GetRequiredService<INotificationService>();
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
