using Configurations;
using Microsoft.AspNetCore.Builder;
using DialoguesRecalculateScheduler.Extensions;
using DialoguesRecalculateScheduler.Settings;
using HBData;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quartz;
using Notifications.Base;
using RabbitMqEventBus.Base;
using System;

namespace DialoguesRecalculateScheduler
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddTransient<SftpSettings>(p => new SftpSettings
                {
                    Host = Environment.GetEnvironmentVariable("SFTP_CONNECTION_HOST"),
                    Port = Int32.Parse(Environment.GetEnvironmentVariable("SFTP_CONNECTION_PORT")),
                    UserName = Environment.GetEnvironmentVariable("SFTP_CONNECTION_USERNAME"),
                    Password = Environment.GetEnvironmentVariable("SFTP_CONNECTION_PASSWORD"),
                    DestinationPath = Environment.GetEnvironmentVariable("SFTP_CONNECTION_DESTINATIONPATH"),
                    DownloadPath = Environment.GetEnvironmentVariable("SFTP_CONNECTION_DOWNLOADPATH")
                });
            services.AddTransient<SftpClient>();

            var schedulerSettings = new DialoguesRecalculateSchedulerSettings()
            {
                Period = Configuration.GetSection(nameof(DialoguesRecalculateSchedulerSettings)).GetValue<int>("Period"),
                DialoguePacketSize = Configuration.GetSection(nameof(DialoguesRecalculateSchedulerSettings)).GetValue<int>("DialoguePacketSize"),       
                Pause = Configuration.GetSection(nameof(DialoguesRecalculateSchedulerSettings)).GetValue<int>("Pause"),
                CheckDeepnessInDays = Configuration.GetSection(nameof(DialoguesRecalculateSchedulerSettings)).GetValue<int>("CheckDeepnessInDays")
            };
            
            services.AddDbContext<RecordsContext>
            (options =>
            {
                var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(HBData)));
            });

            services.AddSingleton(schedulerSettings);
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
            services.AddRabbitMqEventBusConfigFromEnv();
            services.AddDialoguesRecalculateScheduler(schedulerSettings);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IScheduler scheduler)
        {
            var service = app.ApplicationServices.GetRequiredService<INotificationService>();
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            scheduler.ScheduleJob(app.ApplicationServices.GetService<IJobDetail>(),
                app.ApplicationServices.GetService<ITrigger>());
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}