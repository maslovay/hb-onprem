﻿using System.Collections.Generic;
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
using UserOperations.Services.Scheduler;
using Quartz;
using UserOperations.Providers;
using UserOperations.Providers.Interfaces;
using UserOperations.Providers.Realizations;
using UserOperations.Utils.AnalyticHomeUtils;
using UserOperations.Utils.AnalyticContentUtils;
using UserOperations.Utils.AnalyticOfficeUtils;
using UserOperations.Utils.AnalyticRatingUtils;
using UserOperations.Utils.AnalyticReportUtils;
using UserOperations.Utils.AnalyticServiceQualityUtils;
using UserOperations.Utils.AnalyticSpeechController;
using UserOperations.Utils.AnalyticWeeklyReportController;

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
               // var connectionString = "User ID=postgres;Password=annushka123;Host=127.0.0.1;Port=5432;Database=onprem_backup;Pooling=true;Timeout=120;CommandTimeout=0";
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(UserOperations)));
            });
            services.AddScoped<IGenericRepository, GenericRepository>();
            services.AddScoped<IDBOperations, Utils.DBOperations>();
            services.AddScoped<Utils.DBOperationsWeeklyReport>();
            services.AddScoped<IRequestFilters, RequestFilters>();
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
            services.AddScoped<IMailSender, MailSender>();
            services.AddScoped<IAnalyticClientProfileProvider, AnalyticClientProfileProvider>();
            services.AddScoped<IAnalyticContentProvider, AnalyticContentProvider>();
            services.AddScoped<IAnalyticCommonProvider, AnalyticCommonProvider>();
            services.AddScoped<IAnalyticHomeProvider, AnalyticHomeProvider>();
            services.AddScoped<IAnalyticOfficeProvider, AnalyticOfficeProvider>();
            services.AddScoped<IAnalyticRatingProvider, AnalyticRatingProvider>();
            services.AddScoped<IAnalyticServiceQualityProvider, AnalyticServiceQualityProvider>();
            services.AddScoped<IAnalyticSpeechProvider, AnalyticSpeechProvider>();
            services.AddScoped<IAnalyticWeeklyReportProvider, AnalyticWeeklyReportProvider>();
            services.AddScoped<IAccountProvider, AccountProvider>();
            services.AddScoped<IHelpProvider, HelpProvider>();
            services.AddScoped<IUserProvider, UserProvider>();
            services.AddScoped<IPhraseProvider, PhraseProvider>();            
            services.AddScoped(typeof(IAnalyticReportProvider), typeof(AnalyticReportProvider));

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

            services.AddScoped<AnalyticHomeUtils>();
            services.AddScoped<AnalyticContentUtils>();
            services.AddScoped<AnalyticOfficeUtils>();
            services.AddScoped<AnalyticRatingUtils>();
            services.AddScoped<AnalyticReportUtils>();
            services.AddScoped<AnalyticServiceQualityUtils>();
            services.AddScoped<AnalyticSpeechUtils>();
            services.AddScoped<AnalyticWeeklyReportUtils>();

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
                            {"applicationUserId", new Schema{Type = "string", Format = "uuid"}},
                            {"begTime", new Schema{Type = "string", Format = "date-time"}},
                            {"endTime", new Schema{Type = "string", Format = "date-time"}},
                            {"contentType", new Schema{Type = "string"}}

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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.Configure<SftpSettings>(Configuration.GetSection(nameof(SftpSettings)));
            services.AddTransient(provider => provider.GetRequiredService<IOptions<SftpSettings>>().Value);
            services.AddTransient<SftpClient>();

            services.AddSingleton<ElasticClientFactory>();
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

            //services.AddScoped(typeof(INotificationHandler), typeof(NotificationHandler));
            //services.Configure<HttpSettings>(Configuration.GetSection(nameof(HttpSettings)));
            //services.AddScoped(provider =>
            //{
            //    var settings = provider.GetRequiredService<IOptions<HttpSettings>>().Value;
            //    return new HbMlHttpClient(settings);
            //});

            services.AddBenchmarkFillQuartzJob(); //-----------
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IScheduler scheduler)
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
            // app.UseCors(MyAllowSpecificOrigins);
            // app.UseHttpsRedirection();
            app.UseMvc();

            scheduler.ScheduleJob(app.ApplicationServices.GetService<IJobDetail>(),
             app.ApplicationServices.GetService<ITrigger>());

            // add seed
           // BenchmarkRunner.Run<TestRepository>();
        }

    }
}
