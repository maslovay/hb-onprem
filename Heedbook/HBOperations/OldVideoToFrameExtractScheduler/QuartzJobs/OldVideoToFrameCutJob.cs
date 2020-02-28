using HBData.Models;
using HBData.Repository;
using HBLib.Model;
using HBLib.Utils;
using LemmaSharp;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Quartz;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBLib;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Net;
using Notifications.Base;

namespace OldVideoToFrameExtract.QuartzJobs
{
    public class OldVideoToFrameExtractJob : IJob
    {
        private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        private readonly INotificationPublisher _publisher;
        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly INotificationHandler _handler;

        public OldVideoToFrameExtractJob(IServiceScopeFactory factory,
            INotificationPublisher publisher,
            ElasticClientFactory elasticClientFactory,
            INotificationHandler handler
            
            )
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _publisher = publisher;
            _elasticClientFactory = elasticClientFactory;
            _log = _elasticClientFactory.GetElasticClient();  
            _handler = handler;  
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var frames = _context.FileFrames
                .Where(p => p.Time.Date >= DateTime.Now.AddDays(-7).Date
                    && p.Time.Date <= DateTime.Now.AddDays(-2))                
                .ToList();
            var videos = _context.FileVideos
                .Where(p => p.BegTime.Date >= DateTime.Now.AddDays(-7).Date
                    && p.EndTime.Date <= DateTime.Now.AddDays(-2))
                .ToList();
            
            
            var counter =0;
            foreach(var v in videos)
            {
                var videoHaveFrames = frames.Where(f => f.DeviceId == v.DeviceId
                        && f.Time >= v.BegTime
                        && f.Time <= v.EndTime)
                    .ToList()
                    .Any();

                if(!videoHaveFrames)
                {                 
                    if(v.FileExist)
                    {
                        ExtractFramesFromOldVideo(v);
                        counter++;     
                    }                                 
                }
            }
            System.Console.WriteLine($"{counter} videos senе for extract frames");
            
        } 
        private void ExtractFramesFromOldVideo(FileVideo video)
        {
            var message = new FramesFromVideoRun();
            message.Path = $"videos/{video.FileName}";
            _log.Info($"{video.FileVideoId} send for extract frames");
            _handler.EventRaised(message);
        }        
    }
    public class ExtractFrames
    {
        public string path;
        public int retryCount;
        public int deliveryTag;
    }
}