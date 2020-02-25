using HBData.Models;
using HBData.Repository;
using HBLib.Model;
using HBLib.Utils;
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
using DialogueCreatorScheduler.Services;
using DialogueCreatorScheduler.Service;

namespace DialogueCreatorScheduler.QuartzJobs
{
    public class DialogueCreatorSchedulerJob : IJob
    {
        private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly FaceIntervalsService _intervalCalc;
        private readonly DialogueCreatorService _dialogueCreator;
        private readonly DialogueSavingService _publisher;

        public DialogueCreatorSchedulerJob(IServiceScopeFactory factory,
            ElasticClientFactory elasticClientFactory,
            DialogueCreatorService dialogueCreator,
            DialogueSavingService publisher,
            FaceIntervalsService intervalCalc
            )
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _elasticClientFactory = elasticClientFactory;
            _intervalCalc =intervalCalc;
            _dialogueCreator = dialogueCreator;
            _publisher = publisher;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            System.Console.WriteLine("Function started");
            var _log = _elasticClientFactory.GetElasticClient();
            
            try
            {
                var fileFrames = _context.FileFrames
                    .Include(p => p.FrameAttribute)
                    .Include(p => p.Device)
                    .Include(p => p.Device.Company)
                    .Where(p => p.StatusNNId == 6 && 
                        p.FaceId != null && 
                        p.FaceId != Guid.Empty &&
                        !p.Device.Company.IsExtended 
                        ) 
                    .ToList();

                var videos = _context.FileVideos
                    .Include(p => p.Device)
                    .Include(p => p.Device.Company)
                    .Where(p => !p.Device.Company.IsExtended &&
                        p.BegTime >= fileFrames.Min(q => q.Time))
                    .ToList();

                var clients = _context.Clients
                    .Where(p => fileFrames.Select(q => q.Device.CompanyId).Contains( p.CompanyId))
                    .ToList();

                var devideIds = fileFrames.Select(p => p.DeviceId).Distinct();
                foreach(var deviceId in devideIds)
                {
                    _log.Info($"Processing device - {deviceId}");
                    var deviceFrames = fileFrames
                        .Where(p => p.DeviceId == deviceId)
                        .OrderBy(p => p.Time)
                        .ToList();
                    
                    var deviceVideos = videos.Where(p => p.DeviceId == deviceId).ToList(); 
                    var deviceClients = clients.Where(p => p.CompanyId == deviceFrames.FirstOrDefault().Device.CompanyId).ToList();

                    var intervals = _intervalCalc.CreateFaceIntervals(deviceFrames);
                    _log.Info($"Creating intervals for device {deviceId} - {JsonConvert.SerializeObject(intervals)}");
                    
                    var updatedIntervals = _intervalCalc.UpdateFaceIntervals(intervals);
                    _log.Info($"Updatetd intervals for device {deviceId} - {JsonConvert.SerializeObject(updatedIntervals)}");

                    var mergedIntervals = _intervalCalc.MergeFaceIntervals(updatedIntervals);
                    _log.Info($"Merged intervals for device {deviceId}- {JsonConvert.SerializeObject(mergedIntervals)}");

                    var updatedDatesIntervals = _intervalCalc.UpdateLastDate(mergedIntervals, deviceVideos);
                    _log.Info($"Updated last date {deviceId} - {JsonConvert.SerializeObject(updatedDatesIntervals)}");


                    var dialogues = _dialogueCreator.Dialogues(updatedDatesIntervals, ref deviceFrames, deviceClients);
                    dialogues = dialogues.Where(p => p.EndTime.Subtract(p.BegTime).TotalSeconds > 40).ToList();
                    _log.Info($"Created dialogues for device {deviceId} - {JsonConvert.SerializeObject(dialogues)}");

                    if (dialogues.Any())
                    {
                        _context.Dialogues.AddRange(dialogues);
                        _context.SaveChanges();

                        _publisher.Publish(dialogues);
                        dialogues.ForEach(p => p.Comment = null);
                    }
                    _context.SaveChanges();
                    
                }
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occured {e}");
            }
        }

        


    }
}