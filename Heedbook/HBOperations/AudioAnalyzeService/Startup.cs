using AudioAnalyzeService.Handler;
using Configurations;
using HBData;
using HBData.Repository;
using HBLib;
using HBLib.Model;
using HBLib.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace AudioAnalyzeService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<SftpSettings>(Configuration.GetSection(nameof(SftpSettings)));
            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddScoped(provider => provider.GetRequiredService<IOptions<ElasticSettings>>().Value);
            services.AddScoped<ElasticClientFactory>();
            services.AddDbContext<RecordsContext>
            (options =>
            {
                var connectionString = Configuration.GetConnectionString("DefaultConnection");
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(HBData)));
            });
            services.Configure<FFMpegSettings>(Configuration.GetSection(nameof(FFMpegSettings)));
            services.AddTransient<GoogleConnector>();
            services.AddTransient(provider => provider.GetService<IOptions<FFMpegSettings>>().Value);
            services.AddTransient<FFMpegWrapper>();
            services.AddTransient(provider =>
                provider.GetRequiredService<IOptions<SftpSettings>>().Value);
            services.AddTransient<SftpClient>();
            services.AddScoped<IGenericRepository, GenericRepository>();
            services.Configure<AsrSettings>(Configuration.GetSection(nameof(AsrSettings)));
            services.AddTransient(provider => provider.GetService<IOptions<AsrSettings>>().Value);
            services.AddTransient<AsrHttpClient>();
            services.AddTransient<AudioAnalyze>();

            services.AddTransient<AudioAnalyzeRunHandler>();

            services.AddRabbitMqEventBus(Configuration);
            
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var service = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            service.Subscribe<AudioAnalyzeRun, AudioAnalyzeRunHandler>();
            
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}