using Configurations;
using FaceAnalyzeService.Handler;
using HBData;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using HBMLHttpClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using Serilog;

namespace FaceAnalyzeService
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
            services.AddDbContext<RecordsContext>
            (options =>
            {
                var connectionString = Configuration.GetConnectionString("DefaultConnection");
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(HBData)));
            });
            services.Configure<SftpSettings>(Configuration.GetSection(nameof(SftpSettings)));
            services.Configure<HttpSettings>(Configuration.GetSection(nameof(HttpSettings)));
            services.AddTransient(provider =>
            {
                var settings = provider.GetRequiredService<IOptions<SftpSettings>>().Value;
                return new SftpClient(settings);
            });
            services.AddTransient(provider =>
            {
                var settings = provider.GetRequiredService<IOptions<HttpSettings>>().Value;
                return new HbMlHttpClient(settings);
            });
            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddTransient(provider =>
            {
                var settings = provider.GetRequiredService<IOptions<ElasticSettings>>().Value;
                return new ElasticClient(settings);
            });
            services.AddSingleton<FaceAnalyze>();
            services.AddSingleton<FaceAnalyzeRunHandler>();
            services.AddScoped<IGenericRepository, GenericRepository>();
            services.AddLogging(provider => provider.AddSerilog());
            services.AddRabbitMqEventBus(Configuration);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var service = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            service.Subscribe<FaceAnalyzeRun, FaceAnalyzeRunHandler>();
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}