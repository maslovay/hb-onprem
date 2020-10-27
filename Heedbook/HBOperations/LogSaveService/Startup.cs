
using System.Threading;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Base;
using RabbitMqEventBus;
using RabbitMqEventBus.Base;
using Serilog;using System;
using Swashbuckle.AspNetCore.Swagger;
using UnitTestExtensions;
using Notifications.Services;

namespace LogSave
{
    public class Startup
    {
        private readonly String MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
        private bool isCalledFromUnitTest;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            isCalledFromUnitTest = Configuration["isCalledFromUnitTest"] != null && bool.Parse(Configuration["isCalledFromUnitTest"]);
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(MyAllowSpecificOrigins,
                    builder =>
                    {
                        builder.WithOrigins("http://localhost:3000", "https://hbreactapp.azurewebsites.net");
                    });
            });
            services.AddOptions();
            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

#if DEBUG
            services.AddLogging(loggingBuilder => loggingBuilder.AddConsole());
            services.AddLogging(loggingBuilder => loggingBuilder.AddDebug());
            services.AddLogging(loggingBuilder => loggingBuilder.AddEventSourceLogger());
#endif
            
            services.AddDbContext<RecordsContext>
            (options =>
            {
                var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(HBData)));
            });
            services.AddScoped<IGenericRepository, GenericRepository>();

            services.AddSwaggerGen(c =>
            {
                c.EnableAnnotations();
                c.SwaggerDoc("v1", new Info
                {
                    Title = "User Service Api",
                    Version = "v1"
                });
                c.OperationFilter<FormFileSwaggerFilter>();
            });

            services.AddSingleton(provider => 
                {
                    var elasticSettings = new ElasticSettings
                    {
                        Host = Environment.GetEnvironmentVariable("ELASTIC_SETTINGS_HOST"),
                        Port = Int32.Parse(Environment.GetEnvironmentVariable("ELASTIC_SETTINGS_PORT")),
                        FunctionName = "LogSaveService"
                    };
                    return elasticSettings;
                });
            services.AddTransient(provider =>
            {
                var settings = provider.GetRequiredService<ElasticSettings>();
                return new ElasticClient(settings);
            });
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
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            app.UseSwagger(c => { c.RouteTemplate = "logs/swagger/{documentName}/swagger.json"; });
            
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/logs/swagger/v1/swagger.json", "Sample API");
                c.RoutePrefix = "logs/swagger";
            });
            app.UseCors(MyAllowSpecificOrigins);
            //app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}