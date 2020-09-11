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
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using UserOperations.Services;
using HBLib.Utils;
using HBLib;
using UserOperations.Utils;
using UserOperations.Utils.AnalyticHomeUtils;
using UserOperations.Utils.AnalyticContentUtils;
using UserOperations.Utils.AnalyticOfficeUtils;
using UserOperations.Utils.AnalyticRatingUtils;
using UserOperations.Utils.AnalyticReportUtils;
using UserOperations.Utils.AnalyticServiceQualityUtils;
using UserOperations.Utils.AnalyticSpeechController;
using UserOperations.Utils.AnalyticWeeklyReportController;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Http;
using UserOperations.Services.Interfaces;
using UserOperations.Utils.Interfaces;
using HBLib.Utils.Interfaces;
using System;

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
                // var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
            //    var connectionString = "User ID=postgres;Password=annushka123;Host=127.0.0.1;Port=5432;Database=onprem_backup;Pooling=true;Timeout=120;CommandTimeout=0";
                var connectionString = "User ID=test_user;Password=test_password;Host=104.40.181.96;Port=5432;Database=test_db;Pooling=true;Timeout=120;CommandTimeout=0;";
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(UserOperations)));
            });
            services.AddScoped<IGenericRepository, GenericRepository>();
            services.AddScoped<IDBOperations, DBOperations>();
            services.AddScoped<DBOperationsWeeklyReport>();
            services.AddScoped<IRequestFilters, RequestFilters>();
            services.AddScoped<ControllerExceptionFilter>();
            services.AddIdentity<ApplicationUser, ApplicationRole>(p =>
            {
                p.Password.RequireDigit = true;
                p.Password.RequireLowercase = true;
                p.Password.RequireUppercase = true;
                p.Password.RequireNonAlphanumeric = false;
                p.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<RecordsContext>();
            services.AddScoped<IMailSender, MailSender>();

            services.AddScoped<AccountService>();
            services.AddScoped<AnalyticClientProfileService>();
            services.AddScoped<AnalyticContentService>();
            services.AddScoped<AnalyticHomeService>();
            services.AddScoped<AnalyticOfficeService>();
            services.AddScoped<AnalyticRatingService>();
            services.AddScoped<AnalyticReportService>();
            services.AddScoped<AnalyticServiceQualityService>();
            services.AddScoped<AnalyticSpeechService>();
            services.AddScoped<AnalyticWeeklyReportService>();
            services.AddScoped<CampaignContentService>();
            services.AddScoped<CatalogueService>();
            services.AddScoped<ClientNoteService>();
            services.AddScoped<ClientService>();
            services.AddScoped<ICompanyService, CompanyService>();
            services.AddScoped<DemonstrationV2Service>();
            services.AddScoped<DeviceService>();
            services.AddScoped<DialogueService>();
            services.AddScoped<FillingFileFrameService>();
            services.AddScoped<ILoginService, LoginService>();
            services.AddScoped<MediaFileService>();
            services.AddScoped<PhraseService>();
            services.AddScoped<ISalesStageService, SalesStageService>();
            services.AddScoped<SessionService>();
            services.AddScoped<SiteService>();
            services.AddScoped<TabletAppInfoService>();
            services.AddScoped<UserService>();

            services.AddScoped<IAnalyticHomeUtils, AnalyticHomeUtils>();
            services.AddScoped<AnalyticContentUtils>();
            services.AddScoped<IAnalyticOfficeUtils, AnalyticOfficeUtils>();
            services.AddScoped<IAnalyticRatingUtils, AnalyticRatingUtils>();
            services.AddScoped<IAnalyticReportUtils, AnalyticReportUtils>();
            services.AddScoped<IAnalyticServiceQualityUtils, AnalyticServiceQualityUtils>();
            services.AddScoped<IAnalyticSpeechUtils, AnalyticSpeechUtils>();
            services.AddScoped<IAnalyticWeeklyReportUtils, AnalyticWeeklyReportUtils>();
            services.AddScoped<IFileRefUtils, FileRefUtils>();
            services.AddScoped<ISpreadsheetDocumentUtils, SpreadsheetDocumentUtils>();

            services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();

            services.AddSwaggerGen(c =>
            {
                c.EnableAnnotations();
                c.OperationFilter<FileOperationFilter>();
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
                            {"deviceId", new Schema{Type = "string", Format = "uuid"}},
                            {"begTime", new Schema{Type = "string", Format = "date-time"}},
                            {"endTime", new Schema{Type = "string", Format = "date-time"}},
                            {"contentType", new Schema{Type = "string"}},
                            {"url", new Schema{Type = "string"}}
                        }
                });
                c.MapType<CampaignContentAnswer>(() => new Schema
                {
                    Type = "object",
                    Properties = new Dictionary<string, Schema> {
                            {"applicationUserId", new Schema{Type = "string", Format = "uuid"}},
                            {"campaignContentId", new Schema{Type = "string", Format = "uuid"}},
                            {"deviceId", new Schema{Type = "string", Format = "uuid"}},
                            {"answer", new Schema{Type = "string"}},
                            {"time", new Schema{Type = "string", Format = "date-time"}}
                        }
                });

                c.AddSecurityDefinition("Bearer", new ApiKeyScheme()
                {
                    Description = "JWT Authorization header {token}",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });
                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                {
                    { "Bearer", new string[] { } }
                });
            });
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddSingleton(provider => 
                new URLSettings
                {
                    Host = Environment.GetEnvironmentVariable("URL_SETTINGS_HOST")
                }
            );

            services.AddTransient(provider => 
                new SftpSettings
                {
                    Host = Environment.GetEnvironmentVariable("SFTP_CONNECTION_HOST"),
                    Port = Int16.Parse(Environment.GetEnvironmentVariable("SFTP_CONNECTION_PORT")),
                    UserName = Environment.GetEnvironmentVariable("SFTP_CONNECTION_USERNAME"),
                    Password = Environment.GetEnvironmentVariable("SFTP_CONNECTION_PASSWORD"),
                    DestinationPath = Environment.GetEnvironmentVariable("SFTP_CONNECTION_DESTINATIONPATH"),
                    DownloadPath = Environment.GetEnvironmentVariable("SFTP_CONNECTION_DOWNLOADPATH")
                }
            );
            services.AddTransient<ISftpClient, SftpClient>();

            services.AddSingleton<ElasticClientFactory>();
            services.AddSingleton(provider => 
                    new ElasticSettings
                    {
                        Host = Environment.GetEnvironmentVariable("ELASTIC_SETTINGS_HOST"),
                        Port = Int32.Parse(Environment.GetEnvironmentVariable("ELASTIC_SETTINGS_PORT")),
                        FunctionName = Environment.GetEnvironmentVariable("ELASTIC_SETTINGS_FUNCTION_NAME")
                    }
                );
            services.AddSingleton(provider =>
                {
                    var settings = provider.GetRequiredService<IOptions<ElasticSettings>>().Value;
                    return new ElasticClient(settings);
                });
            services.AddSingleton(provider => 
                new SmtpSettings
                {
                    Host = Environment.GetEnvironmentVariable("SMTP_SETTINGS_HOST"),
                    Port = Int32.Parse(Environment.GetEnvironmentVariable("SMTP_SETTINGS_PORT")),
                    FromEmail = Environment.GetEnvironmentVariable("SMTP_SETTINGS_FROM_EMAIL"),
                    Password = Environment.GetEnvironmentVariable("SMTP_SETTINGS_PASSWORD"),
                    DeliveryMethod = Int32.Parse(Environment.GetEnvironmentVariable("SMTP_SETTINGS_DELIVERY_METHOD")),
                    EnableSsl = Boolean.Parse(Environment.GetEnvironmentVariable("SMTP_SETTINGS_ENABLE_SSL")),
                    UseDefaultCredentials = Boolean.Parse(Environment.GetEnvironmentVariable("SMTP_SETTINGS_USE_DEFAULT_CREDENTIALS")),
                    Timeout = Int32.Parse(Environment.GetEnvironmentVariable("SMTP_SETTINGS_TIMEOUT")),
                }
            );
            services.AddSingleton<SmtpClient>();

            //services.AddScoped(typeof(INotificationHandler), typeof(NotificationHandler));
            //services.Configure<HttpSettings>(Configuration.GetSection(nameof(HttpSettings)));
            //services.AddScoped(provider =>
            //{
            //    var settings = provider.GetRequiredService<IOptions<HttpSettings>>().Value;
            //    return new HbMlHttpClient(settings);
            //});
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = Configuration["Tokens:Issuer"],
                    ValidAudience = Configuration["Tokens:Issuer"],
                    RequireSignedTokens = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Tokens:Key"]))
                };
            });
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
            });
            app.UseAuthentication();
            app.UseMvc();

            // add seed
           // BenchmarkRunner.Run<TestRepository>();
        }

    }
}
