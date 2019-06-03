using AsrHttpClient;
using AudioAnalyzeScheduler.Handler;
using Configurations;
using HBData;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using MemoryDbEventBus;
using MemoryDbEventBus.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
            services.AddDbContext<RecordsContext>
            (options =>
            {
                var connectionString = Configuration.GetConnectionString("DefaultConnection");
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(HBData)));
            });
            services.AddSingleton(provider => provider.GetService<IOptions<AsrSettings>>().Value);
            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddSingleton(provider =>
            {
                var settings = provider.GetRequiredService<IOptions<ElasticSettings>>().Value;
                return new ElasticClient(settings);
            });
            services.AddSingleton<AsrHttpClient.AsrHttpClient>();
            services.AddSingleton<SftpClient>();
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<SftpSettings>>().Value);
           
            services.AddScoped<IGenericRepository, GenericRepository>();
            services.AddMemoryDbEventBus(Configuration);
            
            services.AddScoped<CheckAudioRecognizeStatus>();
            services.AddScoped<AudioAnalyzeSchedulerHandler>();
            
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();
            
            
            var service = app.ApplicationServices.GetRequiredService<IMemoryDbPublisher>();
            service.Subscribe<FileAudioDialogueCreatedEvent, AudioAnalyzeSchedulerHandler>();
            
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}