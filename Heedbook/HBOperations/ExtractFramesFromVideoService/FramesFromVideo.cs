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
        private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        private const string FrameContainerName = "frames";
        private readonly FFMpegSettings _settings;
        private readonly FFMpegWrapper _wrapper;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        
        public FramesFromVideo(
            INotificationHandler handler,
            ElasticClient log,
            FFMpegSettings settings,
            FFMpegWrapper wrapper,
            SftpClient sftpClient,
            SftpSettings sftpSettings,
            RecordsContext context
            )
        {
            _handler = handler;
            _log = log;
            _settings = settings;
            _wrapper = wrapper;
            _sftpClient = sftpClient;
            _sftpSettings = sftpSettings;
            _context = context;

        }

        public async Task Run(string videoBlobRelativePath)
        {
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
                var frames = GetLocalFilesInformation(applicationUserId, sessionDir, videoTimeStamp);
                var tasks = frames.Select(p => {
                    return Task.Run(async() => 
                    {
                        await _sftpClient.UploadAsync(p.FramePath, "frames", p.FrameName);
                    });
                });
                await Task.WhenAll(tasks);

                _log.Info($"Processing frames {JsonConvert.SerializeObject(frames)}");
                foreach (var frame in frames)
                {
                    var fileFrame = await CreateFileFrameAsync(applicationUserId, frame.FrameTime, frame.FrameName);
                    _context.FileFrames.Add(fileFrame);
                    _context.SaveChanges();
                    _log.Info($"Creating frame - {frame.FrameName}");
                    RaiseNewFrameEvent(frame.FrameName);
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

        private async Task<FileFrame> CreateFileFrameAsync(string applicationUserId, string frameTime, string fileName)
        {
            var fileFrame = new FileFrame {
                FileFrameId = Guid.NewGuid(),
                ApplicationUserId = Guid.Parse(applicationUserId),
                FaceLength = 0,
                FileContainer = "frames",
                FileExist = await _sftpClient.IsFileExistsAsync($"frames/{fileName}"),
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
                .OrderBy(p => p)
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


        // public async Task Run(string videoBlobRelativePath)
        // {
        //     try
        //     {
        //         _log.Info("Function Extract Frames From Video Started");
        //         _log.Info("Write blob to memory stream");


        //         var targetVideoFileName = Path.GetFileNameWithoutExtension(videoBlobRelativePath);

        //         var appUserId = targetVideoFileName.Split(("_"))[0];
        //         var videoTimestampText = targetVideoFileName.Split(("_"))[1];
        //         var videoTimeStamp =
        //             DateTime.ParseExact(videoTimestampText, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        //         videoTimeStamp = videoTimeStamp.AddSeconds(2);

        //         using (var ftpDownloadStream =
        //             await _client.DownloadFromFtpAsMemoryStreamAsync(videoBlobRelativePath))
        //         {
        //             _log.Info($"File length - {ftpDownloadStream.Length}, File time stamp - {videoTimeStamp}");
        //             var uploadStreams = await _wrapper.CutVideo(ftpDownloadStream, videoTimeStamp, appUserId, 10, 3);
        //             _log.Info($"Keys - {JsonConvert.SerializeObject(uploadStreams.Keys)}");

        //             var uploadTasks = new List<Task>();
        //             var insertToDbTasks = new List<Task>();
        //             foreach (var frameFilename in uploadStreams.Keys)
        //             {
        //                 uploadStreams[frameFilename].Position = 0;
        //                 var uploadTask = _client.UploadAsMemoryStreamAsync(uploadStreams[frameFilename],
        //                     FrameContainerName + "/", frameFilename);

        //                 _log.Info($"Creating file {frameFilename}");

        //                 uploadTasks.Add(uploadTask);

        //                 RaiseNewFrameEvent(frameFilename);

        //                 insertToDbTasks.Add(InsertNewFileFrameToDb(appUserId, frameFilename, videoTimeStamp));

        //                 videoTimeStamp = videoTimeStamp.AddSeconds(3);
        //             }

        //             await Task.WhenAll(uploadTasks);
        //             await Task.WhenAll(insertToDbTasks);
        //         }

        //         _log.Info("Function Extract Frames From Video finished");
        //     }
        //     catch (SftpPathNotFoundException e)
        //     {
        //         Console.WriteLine("Path not found");
        //         _log.Fatal($"{e}");
        //     }
        //     catch (Exception e)
        //     {
        //         Console.WriteLine(e);
        //         throw;
        //     }
        // }

        private void RaiseNewFrameEvent(string filename)
        {
            var message = new FaceAnalyzeRun
            {
                Path = $"{FrameContainerName}/{filename}"
            };

            _handler.EventRaised(message);
        }

        // private async Task InsertNewFileFrameToDb(string appUserId, string filename, DateTime timeStampForFrame)
        // {
        //     Monitor.Enter(_repository);
        //     try
        //     {
        //         var fileFrame = new FileFrame
        //         {
        //             ApplicationUserId = Guid.Parse(appUserId),
        //             FaceLength = 0,
        //             FileContainer = "frames",
        //             FileExist = true,
        //             FileName = filename,
        //             IsFacePresent = false,
        //             StatusId = 6,
        //             StatusNNId = 6,
        //             Time = new DateTime(timeStampForFrame.Year,
        //                 timeStampForFrame.Month,
        //                 timeStampForFrame.Day,
        //                 timeStampForFrame.Hour,
        //                 timeStampForFrame.Minute,
        //                 timeStampForFrame.Second)
        //         };

        //         await _repository.CreateAsync(fileFrame);
        //         _repository.Save();
        //     }
        //     catch (Exception ex)
        //     {
        //         _log.Error("Exception was thrown while trying to access to DB: " + ex.Message, ex);
        //     }
        //     finally
        //     {
        //         Monitor.Exit(_repository);
        //     }
        // }
    }

    public class FrameInfo
    {
        public string FramePath;
        public string FrameTime;
        public string FrameName;
    }
}