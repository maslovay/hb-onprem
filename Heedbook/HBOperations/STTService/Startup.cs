using System.Collections.Concurrent;
using hb_asr_service.Extensions;
using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Quartz;
using Swashbuckle.AspNetCore.Swagger;

namespace STTService
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

            services.Configure<STTSettings>(Configuration.GetSection(nameof(STTSettings)));
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<STTSettings>>().Value);


            services.Configure<SftpSettings>(Configuration.GetSection(nameof(SftpSettings)));
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<SftpSettings>>().Value);
            services.AddDbContext<RecordsContext>
            (options =>
            {
                var connectionString = Configuration.GetConnectionString("DefaultConnection");
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(HBData)));
            });
            services.AddSingleton<ConcurrentQueue<FileAudioDialogue>>();
            services.AddScoped<IGenericRepository, GenericRepository>();
            services.AddScoped<SftpClient>();
            
            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<ElasticSettings>>().Value);

            services.AddSingleton<ElasticClientFactory>();
            services.AddAudioRecognizeQuartz();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Title = "Asr service Api",
                    Version = "v1"
                });

            });
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IScheduler scheduler)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }
            var job = app.ApplicationServices.GetService<IJobDetail>();
            var trigger = app.ApplicationServices.GetService<ITrigger>();
            scheduler.ScheduleJob(job,
                trigger);

            app.UseSwagger(c =>
            {
                c.RouteTemplate = "asr/swagger/{documentName}/swagger.json";
            });

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/asr/swagger/v1/swagger.json", "Sample API");
                c.RoutePrefix = "asr/swagger";
            });
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
