using Configurations;
using HBLib;
using HBLib.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;
using QuartzExtensions;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using VideoToSoundService.Hander;

namespace VideoToSoundService
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
            System.Console.WriteLine("Configure video to sound services");
            services.AddTransient<VideoToSound>();
            services.AddTransient<VideoToSoundRunHandler>();
            
            System.Console.WriteLine("Configure sftp client");
            services.AddTransient<SftpClient>();
            services.Configure<SftpSettings>(Configuration.GetSection(nameof(SftpSettings)));
            services.AddTransient(provider => provider.GetRequiredService<IOptions<SftpSettings>>().Value);

            System.Console.WriteLine("Configure ffmpeg");
            services.Configure<FFMpegSettings>(Configuration.GetSection(nameof(FFMpegSettings)));
            services.AddTransient(provider => provider.GetRequiredService<IOptions<FFMpegSettings>>().Value);

            System.Console.WriteLine("Configure elastic");
            services.Configure<ElasticSettings>(Configuration.GetSection(nameof(ElasticSettings)));
            services.AddScoped(provider => provider.GetRequiredService<IOptions<ElasticSettings>>().Value);
            services.AddScoped<ElasticClientFactory>();
            services.AddTransient<FFMpegWrapper>();
            
            System.Console.WriteLine("Configure rabbitmq");
            services.AddRabbitMqEventBus(Configuration);
            services.AddDeleteOldFilesQuartz();

            System.Console.WriteLine("Inited all services");
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IScheduler scheduler)
        {
            System.Console.WriteLine("Subscribe");
            var publisher = app.ApplicationServices.GetRequiredService<INotificationPublisher>();
            publisher.Subscribe<VideoToSoundRun, VideoToSoundRunHandler>();
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();
            var job = app.ApplicationServices.GetService<IJobDetail>();
            var trigger = app.ApplicationServices.GetService<ITrigger>();
            scheduler.ScheduleJob(job,
                trigger);
            app.UseHttpsRedirection();
            app.UseMvc();
            System.Console.WriteLine("Started video to sound");
        }
    }
}