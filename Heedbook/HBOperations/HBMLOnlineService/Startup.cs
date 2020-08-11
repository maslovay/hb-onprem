using Configurations;
using HBData;
using HBLib;
using HBLib.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using HBMLHttpClient;
using RabbitMqEventBus;
using Notifications.Base;
using Swashbuckle.AspNetCore.Swagger;
using HBMLOnlineService.Service;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Newtonsoft.Json.Serialization;

namespace HBMLOnlineService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<RecordsContext>
            (options =>
            {
                var connectionString = Configuration.GetConnectionString("DefaultConnection");
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(HBData)));
            });
            services.AddSwaggerGen(c =>
            {
                c.EnableAnnotations();
                c.SwaggerDoc("v1", new Info
                {
                    Title = "HBML Face Servise",
                    Version = "v1"
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

            services.AddScoped<HBMLOnlineFaceService>();
            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            // services.AddTransient(provider =>
            // {
                // var settings = provider.GetRequiredService<IOptions<ElasticSettings>>().Value;
                // return new ElasticClient(settings);
            // });
            services.AddSingleton(provider => provider.GetRequiredService<IOptions<ElasticSettings>>().Value);
            services.AddSingleton<ElasticClientFactory>();
            services.AddSingleton<ControllerExceptionFilter>();
            services.AddRabbitMqEventBus(Configuration);
           
            services.Configure<HttpSettings>(Configuration.GetSection(nameof(HttpSettings)));
            services.AddScoped(provider =>
            {
                var settings = provider.GetRequiredService<IOptions<HttpSettings>>().Value;
                return new HbMlHttpClient(settings);
            });
            services.Configure<SftpSettings>(Configuration.GetSection(nameof(SftpSettings)));
            services.AddTransient(provider => provider.GetRequiredService<IOptions<SftpSettings>>().Value);
            services.AddTransient<SftpClient>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddMvc()
                .AddJsonOptions(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var service = app.ApplicationServices.GetRequiredService<INotificationService>();
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            app.UseSwagger(c => { c.RouteTemplate = "face/swagger/{documentName}/swagger.json"; });
            var publisher = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/face/swagger/v1/swagger.json", "Sample API");
                c.RoutePrefix = "face/swagger";
            });
            app.UseMvc();
        }
    }
}