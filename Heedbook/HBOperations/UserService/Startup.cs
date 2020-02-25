using System;
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
using Serilog;
using Swashbuckle.AspNetCore.Swagger;
using UnitTestExtensions;
using HBMLHttpClient;

namespace UserService
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
                var connectionString = Configuration.GetConnectionString("DefaultConnection");
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
            });

            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddTransient(provider =>
            {
                var settings = provider.GetRequiredService<IOptions<ElasticSettings>>().Value;
                return new ElasticClient(settings);
            });

            // (!isCalledFromUnitTest)
                services.AddRabbitMqEventBus(Configuration);
//            else
//            {
//                StartupExtensions.MockRabbitPublisher(services);
//                StartupExtensions.MockNotificationService(services);
//                StartupExtensions.MockNotificationHandler(services);
//                StartupExtensions.MockTransmissionEnvironment<IntegrationEvent>(services);                
//            }
            services.Configure<HttpSettings>(Configuration.GetSection(nameof(HttpSettings)));
            services.AddScoped(provider =>
            {
                var settings = provider.GetRequiredService<IOptions<HttpSettings>>().Value;
                return new HbMlHttpClient(settings);
            });
            services.Configure<SftpSettings>(Configuration.GetSection(nameof(SftpSettings)));
            services.AddTransient(provider => provider.GetRequiredService<IOptions<SftpSettings>>().Value);
            services.AddTransient<SftpClient>();

            services.Configure<FFMpegSettings>(Configuration.GetSection(nameof(FFMpegSettings)));
            services.AddTransient(provider => provider.GetRequiredService<IOptions<FFMpegSettings>>().Value);
            services.AddTransient<FFMpegWrapper>();
            
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var service = app.ApplicationServices.GetRequiredService<INotificationService>();
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            app.UseSwagger(c => { c.RouteTemplate = "user/swagger/{documentName}/swagger.json"; });
            var publisher = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/user/swagger/v1/swagger.json", "Sample API");
                c.RoutePrefix = "user/swagger";
            });
            app.UseCors(MyAllowSpecificOrigins);
            //app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}