using System.Collections.Generic;
using HBData.Repository;
using HBData;
using HBData.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using UserOperations.Services;
using HBLib.Utils;
using HBLib;
using UserOperations.Utils;
using BenchmarkDotNet.Running;
using UserOperations.Controllers.Test;

namespace UserOperations
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        readonly string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddDbContext<RecordsContext>
            (options =>
            {
                var connectionString = Configuration.GetConnectionString("DefaultConnection");
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(UserOperations)));
            });
            services.AddScoped<IGenericRepository, GenericRepository>();
            services.AddScoped<Utils.DBOperations>();
            services.AddScoped<Utils.DBOperationsWeeklyReport>();
            services.AddScoped<RequestFilters>();
            services.AddScoped<IndexesProvider>();       
            services.AddIdentity<ApplicationUser, ApplicationRole>(p =>
            {
                p.Password.RequireDigit = true;
                p.Password.RequireLowercase = true;
                p.Password.RequireUppercase = true;
                p.Password.RequireNonAlphanumeric = false;
                p.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<RecordsContext>();
            services.AddScoped(typeof(ILoginService), typeof(LoginService));
      
            services.AddSwaggerGen(c =>
            {
                c.EnableAnnotations();
                c.SwaggerDoc("v1", new Info
                {
                    Title = "User Service Api",
                    Version = "v1"
                });
                c.MapType<SlideShowSession>(() => new Schema
                {
                    Type = "object",
                    Properties = new Dictionary<string, Schema> {
                            {"campaignContentId", new Schema{Type = "string", Format = "uuid"}},
                            {"applicationUserId", new Schema{Type = "string", Format = "uuid"}},
                            {"begTime", new Schema{Type = "string", Format = "date-time"}},
                            {"endTime", new Schema{Type = "string", Format = "date-time"}}
                        }
                });
                c.MapType<CampaignContentAnswer>(() => new Schema
                {
                    Type = "object",
                    Properties = new Dictionary<string, Schema> {
                            {"campaignContentId", new Schema{Type = "string", Format = "uuid"}},
                            {"answer", new Schema{Type = "string"}},
                            {"time", new Schema{Type = "string", Format = "date-time"}}
                        }
                });
            });
            services.AddCors(options =>
            {
                options.AddPolicy(MyAllowSpecificOrigins,
                    builder =>
                    {
                        builder.WithOrigins("http://localhost:3000",
                                    "https://hbreactapp.azurewebsites.net",
                                    "http://hbserviceplan-onprem.azurewebsites.net")
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });
            });
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.Configure<SftpSettings>(Configuration.GetSection(nameof(SftpSettings)));
            services.AddTransient(provider => provider.GetRequiredService<IOptions<SftpSettings>>().Value);
            services.AddTransient<SftpClient>();

            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<ElasticSettings>>().Value);
            services.AddSingleton(provider =>
                       {
                           var settings = provider.GetRequiredService<IOptions<ElasticSettings>>().Value;
                           return new ElasticClient(settings);
                       });

            services.Configure<SmtpSettings>(Configuration.GetSection(nameof(SmtpSettings)));
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<SmtpSettings>>().Value);
            services.AddSingleton<SmtpClient>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseSwagger(c =>
            {
                c.RouteTemplate = "api/swagger/{documentName}/swagger.json";
            });

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/api/swagger/v1/swagger.json", "Sample API");
                c.RoutePrefix = "api/swagger";
                // c.DisplayOperationId();
            });
            app.UseAuthentication();
            app.UseCors(MyAllowSpecificOrigins);
            app.UseMvc();

          //  BenchmarkRunner.Run<TestAnalyticClientProfile>();
        }

    }
}
