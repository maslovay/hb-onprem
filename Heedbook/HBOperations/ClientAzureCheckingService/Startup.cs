using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientAzureCheckingService.Handler;
using Configurations;
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
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace ClientAzureCheckingService
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
            services.AddDbContext<RecordsContext>
            (options =>
            {
                var connectionString = Configuration.GetConnectionString("DefaultConnection");
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(HBData)));
            }, ServiceLifetime.Scoped);

            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<ElasticSettings>>().Value);
            services.AddSingleton<ElasticClientFactory>();

            services.Configure<AzureFaceClientSettings>(Configuration.GetSection(nameof(AzureFaceClientSettings)));
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<AzureFaceClientSettings>>().Value);

            services.AddSingleton<AzureClient>();
            // services.AddSingleton<DialogueCreatorService>();
            // services.AddSingleton<FaceIntervalsService>();
            // services.AddSingleton<DialogueSavingService>();

            services.AddRabbitMqEventBus(Configuration);

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IScheduler scheduler)
        {
            var service = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            service.Subscribe<ClientAzureCheckingRun, ClientAzureCheckingRunHandler>();
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();
            var job = app.ApplicationServices.GetService<IJobDetail>();
            var trigger = app.ApplicationServices.GetService<ITrigger>();
            scheduler.ScheduleJob(job,
                trigger);
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
