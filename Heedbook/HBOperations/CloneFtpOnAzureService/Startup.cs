using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloneFtpOnAzureService.Extension;
using HBData;
using HBLib;
using HBLib.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;

namespace CloneFtpOnAzureService
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
                var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(HBData)));
            });
            
            services.AddSingleton(provider => 
                {
                    var elasticSettings = new ElasticSettings
                    {
                        Host = Environment.GetEnvironmentVariable("ELASTIC_SETTINGS_HOST"),
                        Port = Int32.Parse(Environment.GetEnvironmentVariable("ELASTIC_SETTINGS_PORT")),
                        FunctionName = "OnPremUserService"
                    };
                    return elasticSettings;
                });
            services.AddTransient(provider =>
            {
                var settings = provider.GetRequiredService<ElasticSettings>();
                return new ElasticClient(settings);
            });
            services.Configure<BlobSettings>(Configuration.GetSection(nameof(BlobSettings)));
            services.AddSingleton(provider=> provider.GetRequiredService<IOptions<BlobSettings>>().Value);
            services.AddSingleton<BlobClient>();
            services.AddTransient<SftpSettings>(p => new SftpSettings
                {
                    Host = Environment.GetEnvironmentVariable("SFTP_CONNECTION_HOST"),
                    Port = Int32.Parse(Environment.GetEnvironmentVariable("SFTP_CONNECTION_PORT")),
                    UserName = Environment.GetEnvironmentVariable("SFTP_CONNECTION_USERNAME"),
                    Password = Environment.GetEnvironmentVariable("SFTP_CONNECTION_PASSWORD"),
                    DestinationPath = Environment.GetEnvironmentVariable("SFTP_CONNECTION_DESTINATIONPATH"),
                    DownloadPath = Environment.GetEnvironmentVariable("SFTP_CONNECTION_DOWNLOADPATH")
                });
            services.AddTransient<SftpClient>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddFtpFileOnQuartzJob();
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
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            scheduler.ScheduleJob(app.ApplicationServices.GetService<IJobDetail>(),
                app.ApplicationServices.GetService<ITrigger>());
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
