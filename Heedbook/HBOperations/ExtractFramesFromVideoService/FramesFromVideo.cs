using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Newtonsoft.Json;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExtractFramesFromVideo
{
    public class FramesFromVideo
    {
        private readonly INotificationHandler _handler;
        private readonly ElasticClientFactory _clientFactory;
        private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        private const string FrameContainerName = "frames";
        private readonly FFMpegSettings _settings;
        private readonly FFMpegWrapper _wrapper;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        
        public FramesFromVideo(
            INotificationHandler handler,
            ElasticClientFactory clientFactory,
            FFMpegSettings settings,
            FFMpegWrapper wrapper,
            SftpClient sftpClient,
            SftpSettings sftpSettings,
            RecordsContext context
            )
        {
            _handler = handler;
            _clientFactory = clientFactory;
            _settings = settings;
            _wrapper = wrapper;
            _sftpClient = sftpClient;
            _sftpSettings = sftpSettings;
            _context = context;
            _log = _clientFactory.GetElasticClient();
        }

        public async Task Run(string videoBlobRelativePath, Guid deviceId)
        {
            _log.SetFormat("{Path}");
            _log.SetArgs(videoBlobRelativePath);

            try
            {
                _log.Info("Function Extract Frames From Video Started");            
                
                var fileName = Path.GetFileNameWithoutExtension(videoBlobRelativePath);
                var applicationUserId = fileName.Split(("_"))[0];
                var videoTimeStamp =
                    DateTime.ParseExact(fileName.Split(("_"))[1], "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                
                    var pathClient = new PathClient();
                var sessionDir = Path.GetFullPath(pathClient.GenLocalDir(pathClient.GenSessionId()));

                var ffmpeg = new FFMpegWrapper(
                    new FFMpegSettings
                    {
                        FFMpegPath = Path.Combine(pathClient.BinPath(), "ffmpeg.exe")
                    });
                
                await _sftpClient.DownloadFromFtpToLocalDiskAsync(
                        $"{_sftpSettings.DestinationPath}{videoBlobRelativePath}", sessionDir);
                var localFilePath = Path.Combine(sessionDir, Path.GetFileName(videoBlobRelativePath));

                var splitRes = ffmpeg.SplitToFrames(localFilePath, sessionDir);
                List<FrameInfo> frames = GetLocalFilesInformation(applicationUserId, sessionDir, videoTimeStamp);
                System.Console.WriteLine($"Frames info - {JsonConvert.SerializeObject(frames)}");
                var tasks = frames.Select(p => {
                    return Task.Run(async() => 
                    {
                        await _sftpClient.UploadAsync(p.FramePath, "frames", p.FrameName);
                    });
                });
                await Task.WhenAll(tasks);

                _log.Info($"Processing frames {JsonConvert.SerializeObject(frames)}");
                var existedFrames = _context.FileFrames.Where(p => p.ApplicationUserId == Guid.Parse(applicationUserId))
                    .ToList();
                var fileFrames = new List<FileFrame>();
                foreach (var frame in frames)
                {
                    var existedFrame = existedFrames?.FirstOrDefault(p => p.FileName == frame.FrameName);
                    if(existedFrame == null)
                    {
                        var fileFrame = await CreateFileFrameAsync(applicationUserId, deviceId, frame.FrameTime, frame.FrameName);
                        fileFrames.Add(fileFrame);
                        _log.Info($"Creating frame - {frame.FrameName}");
                        RaiseNewFrameEvent(frame.FrameName);
                    }
                }
                _log.Info($"Frames for adding - {JsonConvert.SerializeObject(fileFrames)}");
                if (fileFrames.Any())
                {
                    lock (_context)
                    {
                        _context.FileFrames.AddRange(fileFrames);
                        _context.SaveChanges();
                    }
                }
                _log.Info("Deleting local files");
                Directory.Delete(sessionDir, true);

                System.Console.WriteLine("Function finished");
                _log.Info("Function finished");

            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occured {e}");
                System.Console.WriteLine($"{e}");
            }   
        }

        private async Task<FileFrame> CreateFileFrameAsync(string applicationUserId, Guid deviceId, string frameTime, string fileName)
        {
            Guid? userId = Guid.Parse(applicationUserId);
            if (userId == Guid.Empty) userId = null;
            var fileFrame = new FileFrame {
                FileFrameId = Guid.NewGuid(),
                ApplicationUserId = userId,
                DeviceId = deviceId,
                FaceLength = 0,
                FileContainer = "frames",
                FileExist = true,
                FileName = fileName,
                IsFacePresent = false,
                StatusId = 6,
                StatusNNId = 6,
                Time = DateTime.ParseExact(frameTime, "yyyyMMddHHmmss", CultureInfo.InvariantCulture)
            };
            return fileFrame;
        }

        private List<FrameInfo> GetLocalFilesInformation(string applicationUserId, string sessionDir, DateTime videoTimeStamp)
        {
            var frames = Directory.GetFiles(sessionDir, "*.jpg")
                .OrderBy(p => Convert.ToInt32((Path.GetFileNameWithoutExtension(p))))
                .Select(p => new FrameInfo 
                {
                    FramePath = p,
                })
                .ToList();
            for (int i = 0; i< frames.Count(); i++)
            {
                frames[i].FrameTime =  videoTimeStamp.AddSeconds(i * 3).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                frames[i].FrameName = $"{applicationUserId}_{frames[i].FrameTime}.jpg";
            }
            return frames;
        }

        private void RaiseNewFrameEvent(string filename)
        {
            var message = new FaceAnalyzeRun
            {
                Path = $"{FrameContainerName}/{filename}"
            };

            _handler.EventRaised(message);
        }
    }

    public class FrameInfo
    {
        public string FramePath;
        public string FrameTime;
        public string FrameName;
    }
}