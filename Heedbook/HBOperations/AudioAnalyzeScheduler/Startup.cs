using AsrHttpClient;
using AudioAnalyzeScheduler.Extensions;
using AudioAnalyzeScheduler.Settings;
using Configurations;
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
using Quartz;

namespace AudioAnalyzeScheduler
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
            services.Configure<AsrSettings>(Configuration.GetSection(nameof(AsrSettings)));
            //services.Configure<AudioAnalyseSchedulerSettings>(Configuration.GetSection(nameof(AudioAnalyseSchedulerSettings)));

            var schedulerSettings = new AudioAnalyseSchedulerSettings()
            {
                Period = Configuration.GetSection(nameof(AudioAnalyseSchedulerSettings)).GetValue<int>("Period")
            };
            
            services.AddDbContext<RecordsContext>
            (options =>
            {
                var connectionString = Configuration.GetConnectionString("DefaultConnection");
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(HBData)));
            });
            services.AddSingleton(provider => provider.GetService<IOptions<AsrSettings>>().Value);
            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<ElasticSettings>>().Value);
            services.AddTransient<GoogleConnector>();
            services.AddSingleton<ElasticClientFactory>();
            services.AddSingleton<AsrHttpClient.AsrHttpClient>();
            services.AddSingleton<SftpClient>();
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<SftpSettings>>().Value);
            services.AddScoped<IGenericRepository, GenericRepository>();
            services.AddAudioRecognizeQuartz(schedulerSettings);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IScheduler scheduler)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            scheduler.ScheduleJob(app.ApplicationServices.GetService<IJobDetail>(),
                app.ApplicationServices.GetService<ITrigger>());
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}