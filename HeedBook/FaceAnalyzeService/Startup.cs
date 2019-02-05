using System;
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
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FaceAnalyzeService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            var elasticSearchUri =
                new Uri(configuration["ElasticSearch:Host"] + ":" + configuration["ElasticSearch:Port"]);
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(elasticSearchUri))
                .CreateLogger();
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
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
            services.AddTransient<FaceAnalyze>();
            services.AddTransient<FaceAnalyzeMessageHandler>();
            services.AddScoped<IGenericRepository, GenericRepository>();
            services.AddLogging(provider => provider.AddSerilog());
            services.AddRabbitMqEventBus(Configuration);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var service = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            service.Subscribe<FaceAnalyzeRun, FaceAnalyzeMessageHandler>();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();


        }
    }
}
