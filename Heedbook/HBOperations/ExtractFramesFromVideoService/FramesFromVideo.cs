using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ExtractFramesFromVideo
{
    public class FramesFromVideo
    {
        private readonly SftpClient _client;
        private readonly INotificationHandler _handler;
        private readonly ElasticClient _log;
        private readonly IGenericRepository _repository;
        private const string FrameContainerName = "frames";
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

        public async Task Run(string videoBlobRelativePath)
        {
            try
            {
                _log.Info("Function Extract Frames From Video Started");
                _log.Info("Write blob to memory stream");


                var targetVideoFileName = Path.GetFileNameWithoutExtension(videoBlobRelativePath);

                var appUserId = targetVideoFileName.Split(("_"))[0];
                var videoTimestampText = targetVideoFileName.Split(("_"))[1];
                var videoTimeStamp =
                    DateTime.ParseExact(videoTimestampText, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                videoTimeStamp = videoTimeStamp.AddSeconds(2);

                using (var ftpDownloadStream =
                    await _client.DownloadFromFtpAsMemoryStreamAsync(videoBlobRelativePath))
                {
                    var uploadStreams = await _wrapper.CutVideo(ftpDownloadStream, videoTimeStamp, appUserId, 10, 3);

                    var uploadTasks = new List<Task>();
                    var insertToDbTasks = new List<Task>();
                    foreach (var frameFilename in uploadStreams.Keys)
                    {
                        uploadStreams[frameFilename].Position = 0;
1
                        var uploadTask = _client.UploadAsMemoryStreamAsync(uploadStreams[frameFilename],
                            FrameContainerName + "/", frameFilename);

                        uploadTasks.Add(uploadTask);

                        RaiseNewFrameEvent(frameFilename);

                        insertToDbTasks.Add(InsertNewFileFrameToDb(appUserId, frameFilename, videoTimeStamp));

                        videoTimeStamp = videoTimeStamp.AddSeconds(3);
                    }

                    await Task.WhenAll(uploadTasks);
                    await Task.WhenAll(insertToDbTasks);
                }

                _log.Info("Function Extract Frames From Video finished");
            }
            catch (SftpPathNotFoundException)
            {
                Console.WriteLine("Path not found");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //throw;
            }
        }

        private void RaiseNewFrameEvent(string filename)
        {
            var message = new FaceAnalyzeRun
            {
                Path = $"{FrameContainerName}/{filename}"
            };

            _handler.EventRaised(message);
        }

        private async Task InsertNewFileFrameToDb(string appUserId, string filename, DateTime timeStampForFrame)
        {
            Monitor.Enter(_repository);
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
                    StatusId = 6,
                    StatusNNId = 6,
                    Time = new DateTime(timeStampForFrame.Year,
                        timeStampForFrame.Month,
                        timeStampForFrame.Day,
                        timeStampForFrame.Hour,
                        timeStampForFrame.Minute,
                        timeStampForFrame.Second)
                };

                await _repository.CreateAsync(fileFrame);
                _repository.Save();
            }
            catch (Exception ex)
            {
                _log.Error("Exception was thrown while trying to access to DB: " + ex.Message, ex);
            }
            finally
            {
                Monitor.Exit(_repository);
            }
        }
    }
}