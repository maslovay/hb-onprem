using Configurations;
using DialogueVideoMergeService.Handler;
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
using Microsoft.Extensions.Options;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace DialogueVideoMergeService
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
            services.AddDbContext<RecordsContext>
            (options =>
            {
                var connectionString = Configuration.GetConnectionString("DefaultConnection");
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(HBData)));
            });
            services.Configure<SftpSettings>(Configuration.GetSection(nameof(SftpSettings)));
            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddTransient(provider => provider.GetRequiredService<IOptions<SftpSettings>>().Value);
            services.AddTransient(provider => provider.GetRequiredService<IOptions<ElasticSettings>>().Value);
            services.AddTransient<SftpClient>();
            services.AddTransient(provider =>
            {
                var settings = provider.GetService<ElasticSettings>();
                return new ElasticClient(settings);
            });
            services.AddTransient<DialogueVideoMerge>();
            services.AddTransient<DialogueVideoMergeRunHandler>();
            services.AddScoped<IGenericRepository, GenericRepository>();
            services.AddRabbitMqEventBus(Configuration);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var handlerService = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            handlerService.Subscribe<DialogueVideoMergeRun, DialogueVideoMergeRunHandler>();
            
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
