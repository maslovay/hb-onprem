﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBLib;
using HBLib.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Swashbuckle.AspNetCore.Swagger;

namespace TestLoging
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
            services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();

            services.AddSwaggerGen(c =>
            {
                c.EnableAnnotations();
                c.SwaggerDoc("v1", new Info
                {
                    Title = "User Service Api",
                    Version = "v1"
                });
            });
            
            services.AddTransient(provider =>
            {
                var host = Environment.GetEnvironmentVariable("HOST");
                var port = Environment.GetEnvironmentVariable("PORT");
                var functionName = Environment.GetEnvironmentVariable("FUNCTION_NAME");
                var settings = new ElasticSettings{
                    Host = host,
                    Port = Convert.ToInt32(port),
                    FunctionName = functionName
                };
                return new ElasticClient(settings);
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            app.UseSwagger(c => { c.RouteTemplate = "testlog/swagger/{documentName}/swagger.json"; });            
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/testlog/swagger/v1/swagger.json", "Sample API");
                c.RoutePrefix = "testlog/swagger";
            });
            //app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
