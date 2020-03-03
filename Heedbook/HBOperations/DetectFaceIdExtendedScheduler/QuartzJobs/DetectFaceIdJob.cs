using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using System.Threading.Tasks;
using HBData;
using HBLib;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System;
using DetectFaceIdExtendedScheduler.Models;
using DetectFaceIdExtendedScheduler.Services;
using DetectFaceIdExtendedScheduler.Settings;

namespace DetectFaceIdExtendedScheduler.QuartzJobs
{
    public class DetectFaceIdJob : IJob
    {
        private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly DetectFaceIdService _detect;
        private readonly DetectFaceIdSettings _settings;

        public DetectFaceIdJob(
            IServiceScopeFactory factory,
            ElasticClientFactory elasticClientFactory,
            DetectFaceIdService detect,
            DetectFaceIdSettings settings
            )
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _elasticClientFactory = elasticClientFactory;
            _detect = detect;
            _settings = settings;
        }

         public async Task Execute(IJobExecutionContext context)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            try
            {
                System.Console.WriteLine("Funtion extended started");
                var begTime = DateTime.UtcNow.AddDays(-5);
                var fileFramesEdges = _context.FileFrames
                    .Include(p => p.Device)
                    .Include(p => p.Device.Company)
                    .Where(p => 
                        p.FaceId == null && 
                        p.FaceLength > 0 &&
                        p.Time > begTime &&
                        p.Device.Company.IsExtended)
                    .GroupBy(p => p.DeviceId)
                    .Select(p => new {
                        DeviceId = p.Key,
                        MinTime = p.Min(q => q.Time).AddSeconds(-_settings.TimeGapRequest)
                    }).ToList();

                _log.Info($"Proceeded file frames - {JsonConvert.SerializeObject(fileFramesEdges)}");
                System.Console.WriteLine($"Proceeded file frames - {JsonConvert.SerializeObject(fileFramesEdges)}");

                foreach (var fileFramesEdge in fileFramesEdges)
                {
                    System.Console.WriteLine($"Processing device id {fileFramesEdge.DeviceId}");
                    _log.Info($"Proceecing {fileFramesEdge.DeviceId}");

                    var frameAttributes = _context.FrameAttributes
                        .Include(p => p.FileFrame)
                        .Where(p => 
                            p.FileFrame.DeviceId == fileFramesEdge.DeviceId &&
                            p.FileFrame.Time >= fileFramesEdge.MinTime)
                        .OrderBy(p => p.FileFrame.Time)
                        .ToList();

                    System.Console.WriteLine(frameAttributes.Count());

                    var fileVideos = _context.FileVideos
                        .Where(p => p.DeviceId == fileFramesEdge.DeviceId &&
                            p.BegTime >= fileFramesEdge.MinTime)
                        .OrderBy(p => p.BegTime)
                        .ToList();

                    System.Console.WriteLine(fileVideos.Count());

                    _log.Info($"Total frames -- {frameAttributes.Count()}");

                    frameAttributes.Where(p => 
                            JsonConvert.DeserializeObject<Value>(p.Value).Height < _settings.MinHeight || 
                            JsonConvert.DeserializeObject<Value>(p.Value).Width < _settings.MinWidth).ToList()
                        .ForEach(p => p.FileFrame.FaceId = Guid.Empty);
                
                    var framesProceed = frameAttributes.Where(p => p.FileFrame.FaceId != Guid.Empty).ToList();

                    framesProceed = _detect.DetectFaceIds(framesProceed);
                    _log.Info($"Frames device count - {framesProceed.Count()}");
                    System.Console.WriteLine($"Frames device count - {framesProceed.Count()}");

                    framesProceed = _detect.UpdateFaceIds(framesProceed, fileVideos);

                    System.Console.WriteLine(JsonConvert.SerializeObject(framesProceed.Select(p => p.FileFrame.FaceId).Distinct()));

                    _context.SaveChanges();

                }
                System.Console.WriteLine("Finished");
                _context.SaveChanges();
                _log.Info("Function finished");
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occured {e}");
            }
        }
    }
}