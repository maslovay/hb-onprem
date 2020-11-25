using System;
using System.Text;
using System.Threading;
using Configurations;
using ExtractFramesFromVideo.Handler;
using HBData;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Notifications.Base;
using Quartz;
using QuartzExtensions;
using RabbitMqEventBus;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;
using UnitTestExtensions;

namespace ExtractFramesFromVideo
{
    public class Startup
    {
        private bool isCalledFromUnitTest;
        private DateTime lastTime;
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            isCalledFromUnitTest = Configuration["isCalledFromUnitTest"] != null && bool.Parse(Configuration["isCalledFromUnitTest"]);
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddDbContext<RecordsContext>
            (options =>
            {
                var connectionString = Configuration.GetConnectionString("DefaultConnection");
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(HBData)));
            });
            services.Configure<SftpSettings>(Configuration.GetSection(nameof(SftpSettings)));
            services.AddTransient(provider => provider.GetRequiredService<IOptions<SftpSettings>>().Value);
            services.AddTransient<SftpClient>();
            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddScoped(provider => provider.GetRequiredService<IOptions<ElasticSettings>>().Value);
            services.AddTransient(provider =>
            {
                var settings = provider.GetRequiredService<ElasticSettings>();
                return new ElasticClient(settings);
            });
            
            services.Configure<FFMpegSettings>(Configuration.GetSection(nameof(FFMpegSettings)));
            services.AddTransient(provider => provider.GetService<IOptions<FFMpegSettings>>().Value);
            services.AddTransient<FFMpegWrapper>();
            services.AddDeleteOldFilesQuartz();
            services.AddRabbitMqEventBus(Configuration);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddTransient<FramesFromVideo>();
            services.AddTransient<FramesFromVideoRunHandler>();
            services.Configure<LivenessSettings>(Configuration.GetSection(nameof(LivenessSettings)));
            services.AddTransient(provider => provider.GetService<IOptions<LivenessSettings>>().Value);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IScheduler scheduler)
        {
            var livenessSettings = app.ApplicationServices.GetService<LivenessSettings>();
            HelthTime.SERVICELIVETIMEINMINUTES = livenessSettings.LivenesCheckPeriod;
            app.ApplicationServices.GetRequiredService<INotificationService>();
            var service = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            service.Subscribe<FramesFromVideoRun, FramesFromVideoRunHandler>();
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();
            var job = app.ApplicationServices.GetService<IJobDetail>();
            var trigger = app.ApplicationServices.GetService<ITrigger>();
            scheduler.ScheduleJob(job,
                trigger);
            // app.UseHttpsRedirection();
            app.UseMvc();
            app.Map("/healthz", Healthz);
        }
        private void Healthz(IApplicationBuilder app)
        {
            var rabbitClient = app.ApplicationServices.GetService<IRabbitMqPersistentConnection>();
            var elasticClient = app.ApplicationServices.GetService<ElasticClient>();
            app.Run(async context => 
            {
                var rabbitIsConnected = rabbitClient.IsConnected;
                var SB = new StringBuilder();
                
                if(DateTime.Now.Subtract(HelthTime.Time).Minutes > HelthTime.SERVICELIVETIMEINMINUTES || !rabbitIsConnected)
                {
                    var response = context.Response;
                    response.StatusCode = 503;
                    response.Headers.Add("Custom-Header", "NotAwesome");
                    await response.WriteAsync($"NotAwesome");
                    SB.Append($"StatusCode: {503}\n");
                }
                else
                {
                    var response = context.Response;
                    response.Headers.Add("Custom-Header", "Awesome");
                    await response.WriteAsync($"Awesome");
                    SB.Append($"StatusCode: {200}\n");
                }
                SB.Append($"SERVICELIVETIMEINMINUTES: {HelthTime.SERVICELIVETIMEINMINUTES}\n");
                SB.Append($"curentTime: {DateTime.Now}\n");
                SB.Append($"lastTime: {HelthTime.Time}\n");
                SB.Append($"Subtract in.Minutes: {DateTime.Now.Subtract(HelthTime.Time).Minutes}\n");
                SB.Append($"rabbitIsConnected: {rabbitIsConnected}\n");
                elasticClient.Info(SB.ToString());
            });
        }
    }
}