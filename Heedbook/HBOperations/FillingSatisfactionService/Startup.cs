using Configurations;
using FillingSatisfactionService.Handler;
using FillingSatisfactionService.Models;
using FillingSatisfactionService.Utils.ScoreCalculations;
using FillingSatisfactionService.Utils;
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
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using FillingSatisfactionService.Services;

namespace FillingSatisfactionService
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
            services.Configure<WeightCalculationModel>(Configuration.GetSection(nameof(WeightCalculationModel)));
            services.AddTransient(provider => provider.GetRequiredService<IOptions<WeightCalculationModel>>().Value);
            services.Configure<LinearRegressionWeightModel>(Configuration.GetSection(nameof(LinearRegressionWeightModel)));
            services.AddTransient(provider => provider.GetRequiredService<IOptions<LinearRegressionWeightModel>>().Value);

            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddScoped(provider => provider.GetRequiredService<IOptions<ElasticSettings>>().Value);
            services.AddScoped<ElasticClientFactory>();
            services.AddTransient<AudioCalculations>();
            services.AddTransient<VisualCalculations>();
            services.AddTransient<SpeechCalculations>();
            services.AddTransient<ClientCalculations>();
            services.AddTransient<TotalScoreCalculations>();
            services.AddTransient<TotalScoreRecalculations>();
            services.AddTransient<FillingSatisfactionServiceCalculation>();
            services.AddTransient<FillingSatisfaction>();
            services.AddTransient<FillingSatisfactionRunHandler>();
            services.AddRabbitMqEventBus(Configuration);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var handlerService = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            handlerService.Subscribe<FillingSatisfactionRun, FillingSatisfactionRunHandler>();
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}