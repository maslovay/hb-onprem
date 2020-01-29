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
using DetectFaceIdScheduler.Models;
using DetectFaceIdScheduler.Services;
using DetectFaceIdScheduler.Settings;

namespace DetectFaceIdScheduler.QuartzJobs
{
    public class DetectFaceIdJob : IJob
    {
        private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly DetectFaceIdService _detect;
        private readonly DetectFaceIdSettings _settings;

        public DetectFaceIdJob(IServiceScopeFactory factory,
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
                // Get first not marked FileFrame
                var fileFramesEdge = _context.FileFrames
                    .Include(p => p.Device)
                    .Include(p => p.Device.Company)
                    .Where(p => 
                        p.FaceId == null && 
                        p.FaceLength > 0)
                    .GroupBy(p => p.DeviceId)
                    .Select(p => new {
                        DeviceId = p.Key,
                        MinTime = p.Min(q => q.Time).AddSeconds(-_settings.TimeGapRequest)
                    }).ToList();

                _log.Info($"Proceeded file frames - {JsonConvert.SerializeObject(fileFramesEdge)}");

                var frameAttributes = _context.FrameAttributes
                    .Include(p => p.FileFrame)
                    .Where(p => 
                        fileFramesEdge.Select(q => q.DeviceId).Contains(p.FileFrame.DeviceId) &&
                        p.FileFrame.Time >= fileFramesEdge.Where(q => q.DeviceId == p.FileFrame.DeviceId).FirstOrDefault().MinTime)
                    .OrderBy(p => p.FileFrame.Time)
                    .ToList();

                _log.Info($"Total frames -- {frameAttributes.Count()}");

                frameAttributes.Where(p => 
                        JsonConvert.DeserializeObject<Value>(p.Value).Height < _settings.MinHeight || 
                        JsonConvert.DeserializeObject<Value>(p.Value).Width < _settings.MinWidth).ToList()
                    .ForEach(p => p.FileFrame.FaceId = Guid.Empty);
                
                var framesProceed = frameAttributes.Where(p => p.FileFrame.FaceId != null);
                var deviceIds = framesProceed.Select(p => p.FileFrame.DeviceId).Distinct().ToList();
                _log.Info($"Priocessing list of devices - {JsonConvert.SerializeObject(deviceIds)}");

                foreach (var deviceId in deviceIds)
                {
                     _log.Info($"Proceecing {deviceId}");
                    var framesDevice = framesProceed
                        .Where(p => p.FileFrame.DeviceId == deviceId)
                        .OrderBy(p => p.FileFrame.Time)
                        .ToList();

                    _detect.DetectFaceIds(ref framesDevice);
                    _log.Info($"Frames device count - {framesDevice.Count()}");

                }
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