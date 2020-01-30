 
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
                var connectionString = Configuration.GetConnectionString("DefaultConnection");
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(HBData)));
            }, ServiceLifetime.Scoped);

            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<ElasticSettings>>().Value);
            services.AddSingleton<ElasticClientFactory>();
            services.AddSingleton<PersonDetectionService>();
            services.AddSingleton<DialogueCreatorService>();
            // services.AddRabbitMqEventBus(Configuration);

            services.AddDialogueCreatorSchedulerQuartz(schedulerSettings);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddSingleton<DescriptorCalculations>();
        }

        

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IScheduler scheduler)
        {
            // var service = app.ApplicationServices.GetRequiredService<INotificationService>();
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
