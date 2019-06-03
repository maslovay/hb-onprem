using Configurations;
using HBData;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using MemoryDbEventBus;
using MemoryDbEventBus.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DialogueStatusCheckerScheduler
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
            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddSingleton(provider =>
            {
                var settings = provider.GetRequiredService<IOptions<ElasticSettings>>().Value;
                return new ElasticClient(settings);
            });
            services.AddScoped<IGenericRepository, GenericRepository>();
            services.AddScoped<DialogueStatusChecker>();
            services.AddScoped<Handler.DialogueStatusCheckerSchedulerHandler>();

            services.AddRabbitMqEventBus(Configuration);
            services.AddMemoryDbEventBus(Configuration);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            var service = app.ApplicationServices.GetRequiredService<IMemoryDbPublisher>();
            service.Subscribe<DialogueCreatedEvent, Handler.DialogueStatusCheckerSchedulerHandler>();
            
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}