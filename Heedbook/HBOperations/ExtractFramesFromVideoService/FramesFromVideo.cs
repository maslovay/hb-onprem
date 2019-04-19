using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Notifications.Base;
using RabbitMqEventBus.Events;

namespace ExtractFramesFromVideo
{
    public class FramesFromVideo
    {
        private readonly SftpClient _client;
        private readonly INotificationHandler _handler;
        private readonly ElasticClient _log;
        private readonly IGenericRepository _repository;
        private const string FrameContainerName = "frames";
        private const string VideoContainerName = "videos";
        private readonly FFMpegSettings _settings;
        private readonly FFMpegWrapper _wrapper;
        
        public FramesFromVideo(SftpClient client,
            IGenericRepository repository,
            INotificationHandler handler,
            ElasticClient log,
            FFMpegSettings settings,
            FFMpegWrapper wrapper)
        {
            _client = client;
            _repository = repository;
            _handler = handler;
            _log = log;
            _settings = settings;
            _wrapper = wrapper;
        }

        public async Task Run(string videoBlobName)
        {
            _log.Info("Function Extract Frames From Video Started");
            _log.Info("Write blob to memory stream");
 
            var targetVideoFileName = Path.GetFileNameWithoutExtension(videoBlobName);

            var appUserId = targetVideoFileName.Split(("_"))[0];
            var videoTimestampText = targetVideoFileName.Split(("_"))[1];
            var videoTimeStamp = DateTime.ParseExact(videoTimestampText, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            videoTimeStamp = videoTimeStamp.AddSeconds(2);

            using (var ftpDownloadStream =
                await _client.DownloadFromFtpAsMemoryStreamAsync(VideoContainerName + "/" + videoBlobName))
            {
                var uploadStreams = await _wrapper.CutVideo(ftpDownloadStream, videoTimeStamp, appUserId, 10, 3);

                List<Task> uploadTasks = new List<Task>();
                
                foreach (var frameFilename in uploadStreams.Keys)
                {
                    uploadStreams[frameFilename].Position = 0;
                    
                    var uploadTask = _client.UploadAsMemoryStreamAsync(uploadStreams[frameFilename],
                        FrameContainerName + "/", frameFilename);

                    uploadTasks.Add(uploadTask);
                    
                    RaiseNewFrameEvent(frameFilename);
                   
                    await InsertNewFileFrameToDb(appUserId, frameFilename, videoTimeStamp);
                    
                    videoTimeStamp = videoTimeStamp.AddSeconds(3);
                }

                Task.WaitAll(uploadTasks.ToArray());
            }

            _log.Info("Function Extract Frames From Video finished");
        }

        private void RaiseNewFrameEvent(string filename)
        {
            var message = new FaceAnalyzeRun
            {
                Path = $"{FrameContainerName}{Path.PathSeparator}{filename}"
            };

            _handler.EventRaised(message);
        }

        private async Task InsertNewFileFrameToDb(string appUserId, string filename, DateTime timeStampForFrame)
        {
            try
            {
                var fileFrame = new FileFrame
                {
                    ApplicationUserId = Guid.Parse(appUserId),
                    FaceLength = 0,
                    FileContainer = "frames",
                    FileExist = true,
                    FileName = filename,
                    IsFacePresent = false,
                    StatusId = 1,
                    StatusNNId = 1,
                    Time = new DateTime(timeStampForFrame.Year,
                        timeStampForFrame.Month,
                        timeStampForFrame.Day,
                        timeStampForFrame.Hour,
                        timeStampForFrame.Minute,
                        timeStampForFrame.Second)
                };

                await _repository.CreateAsync(fileFrame);
                await _repository.SaveAsync();
            }
            catch ( Exception ex )
            {
                _log.Error("Exception was thrown while trying to access to DB: " + ex.Message, ex);
            }
        }
    }
}