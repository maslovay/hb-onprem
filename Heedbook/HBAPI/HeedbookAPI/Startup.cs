using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserOperations;
using UserOperations.Models;
using HBData.Repository;
using HBData;
using HBData.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using UserOperations.Services;
using Microsoft.AspNetCore.Identity;
using HBLib.Utils;
using HBLib;

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
            services.AddIdentity<ApplicationUser, ApplicationRole>(p => {
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
                c.MapType<SlideShowSession>(() => new Schema{ 
                        Type = "object",
                        Properties = new Dictionary<string, Schema> {
                            {"campaignContentId", new Schema{Type = "string", Format = "uuid"}},
                            {"applicationUserId", new Schema{Type = "string", Format = "uuid"}},
                            {"begTime", new Schema{Type = "string", Format = "date-time"}},
                            {"endTime", new Schema{Type = "string", Format = "date-time"}}
                        }} );
                c.MapType<CampaignContentAnswer>(() => new Schema{ 
                        Type = "object",
                        Properties = new Dictionary<string, Schema> {
                            {"campaignContentId", new Schema{Type = "string", Format = "uuid"}},
                            {"answer", new Schema{Type = "string"}},
                            {"time", new Schema{Type = "string", Format = "date-time"}}
                        }} );
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.Configure<SftpSettings>(Configuration.GetSection(nameof(SftpSettings)));
            services.AddTransient(provider => provider.GetRequiredService<IOptions<SftpSettings>>().Value);
            services.AddTransient<SftpClient>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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

            app.UseHttpsRedirection();
            app.UseCors(MyAllowSpecificOrigins); 
            app.UseMvc();
        }
        
    }
}