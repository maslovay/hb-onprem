using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

using Configurations;
using HBData;
using HBLib;
using HBLib.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using PersonOnlineDetectionService.Handler;
using Microsoft.AspNetCore.Mvc;
using PersonOnlineDetectionService.Utils;
using HBLib.Model;

namespace PersonOnlineDetectionService
{
    public class Startup
    {
        public IConfiguration Configuration;
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            
            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddScoped(provider => provider.GetRequiredService<IOptions<ElasticSettings>>().Value);
            services.AddScoped<ElasticClientFactory>();
            services.Configure<HeedbookSettingsInAkBars>(Configuration.GetSection(nameof(HeedbookSettingsInAkBars)));
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<HeedbookSettingsInAkBars>>().Value);
            services.AddTransient<AkBarsOperations>();
            
            services.AddDbContext<RecordsContext>
            (options =>
            {
                var connectionString = Configuration.GetConnectionString("DefaultConnection");
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(HBData)));
            });

            services.Configure<SftpSettings>(Configuration.GetSection(nameof(SftpSettings)));
            services.AddTransient(provider =>
                provider.GetRequiredService<IOptions<SftpSettings>>().Value);
            services.AddTransient<SftpClient>();
            services.AddTransient<PersonOnlineDetection>();
            services.AddTransient<PersonOnlineDetectionHandler>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddScoped<PersonDetectionUtils>();
            services.AddScoped<WebSocketIoUtils>();
            services.AddScoped<CreateAvatarUtils>();
            services.AddSingleton<DescriptorCalculations>();

            services.AddRabbitMqEventBus(Configuration);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var service = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            service.Subscribe<PersonOnlineDetectionRun, PersonOnlineDetectionHandler>();
            
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
