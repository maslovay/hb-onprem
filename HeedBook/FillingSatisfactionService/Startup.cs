﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FillingSatisfactionService.Handler;
using FillingSatisfactionService.Helper;
using HBData;
using HBData.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace FillingSatisfactionService
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
                var connectionString = Configuration.GetConnectionString("DefaultConnection");
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(HBData)));
            });
            services.Configure<CalculationConfig>(Configuration.GetSection(nameof(CalculationConfig)));
            services.AddTransient(provider => provider.GetRequiredService<IOptions<CalculationConfig>>().Value);
            services.AddTransient<Calculations>();
            services.AddTransient<FillingSatisfaction>();
            services.AddTransient<FillingSatisfactionRunHandler>();
            services.AddScoped<IGenericRepository, GenericRepository>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var handlerService = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            handlerService.Subscribe<FillingSatisfactionRun, FillingSatisfactionRunHandler>();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
