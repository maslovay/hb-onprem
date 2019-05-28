using System;
using System.Linq.Expressions;
using Configurations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace MemoryCacheService
{
    public class Startup
    {
        private IConfiguration Configuration { get; }
        private bool isCalledFromUnitTest;
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            isCalledFromUnitTest = Configuration["isCalledFromUnitTest"] != null && bool.Parse(Configuration["isCalledFromUnitTest"]);
        }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            var memDbHost = Configuration.GetSection("MemoryCacheDb").GetValue<string>("Host");
                        var memDbPort = Configuration.GetSection("MemoryCacheDb").GetValue<int>("Port");
                        var memAllowAdmin = Configuration.GetSection("MemoryCacheDb").GetValue<bool>("AllowAdmin");
                        var connString = $"{memDbHost}:{memDbPort}, _allowAdmin:{memAllowAdmin}";
                        
            services.AddTransient<IMemoryCache, RedisMemoryCache>( opt => new RedisMemoryCache( connString ));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}