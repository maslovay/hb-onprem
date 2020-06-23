using Configurations;
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
using Microsoft.Extensions.Options;
using Notifications.Base;
using RabbitMqEventBus.Base;
using PersonDetectionAkBarsService.Exceptions;
using PersonDetectionAkBarsService.Handler;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using HBLib.Model;

namespace PersonDetectionAkBarsService
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
       public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
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

            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<ElasticSettings>>().Value);
            services.AddSingleton<ElasticClientFactory>();
            services.Configure<HeedbookSettingsInAkBars>(Configuration.GetSection(nameof(HeedbookSettingsInAkBars)));
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<HeedbookSettingsInAkBars>>().Value);
            services.AddTransient<AkBarsOperations>();

            services.AddTransient<SftpClient>();
            services.Configure<SftpSettings>(Configuration.GetSection(nameof(SftpSettings)));
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<SftpSettings>>().Value);
            services.AddSingleton<DescriptorCalculations>();
            services.AddTransient<PersonDetection>();
            services.AddTransient<PersonDetectionRunHandler>();
            services.AddScoped<IGenericRepository, GenericRepository>();
            services.AddRabbitMqEventBus(Configuration);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var handlerService = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            handlerService.Subscribe<PersonDetectionRun, PersonDetectionRunHandler>();
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
