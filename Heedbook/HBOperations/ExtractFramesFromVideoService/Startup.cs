using System.Threading;
using Configurations;
using ExtractFramesFromVideo.Handler;
using HBData;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notifications.Base;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using UnitTestExtensions;

namespace ExtractFramesFromVideo
{
    public class Startup
    {
        private bool isCalledFromUnitTest;
        
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
            services.AddScoped<ElasticClientFactory>();
            
            services.Configure<FFMpegSettings>(Configuration.GetSection(nameof(FFMpegSettings)));
            services.AddTransient(provider => provider.GetService<IOptions<FFMpegSettings>>().Value);
            services.AddTransient<FFMpegWrapper>();
            services.AddRabbitMqEventBus(Configuration);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddTransient<FramesFromVideo>();
            services.AddTransient<FramesFromVideoRunHandler>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.ApplicationServices.GetRequiredService<INotificationService>();
            var service = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            service.Subscribe<FramesFromVideoRun, FramesFromVideoRunHandler>();
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}