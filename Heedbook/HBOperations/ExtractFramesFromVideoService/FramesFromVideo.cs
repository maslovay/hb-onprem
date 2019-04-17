using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
        private const string fileContainerName = "frames";
        private readonly string _localVideoPath;
        private readonly string _localFramesPath;
        
        public FramesFromVideo(SftpClient client,
            IGenericRepository repository,
            INotificationHandler handler,
            ElasticClient log,
            string localVideoPath,
            string localFramesPath)
        {
            _client = client;
            _repository = repository;
            _handler = handler;
            _log = log;
            _localVideoPath = localVideoPath;
            _localFramesPath = localFramesPath;

            CreateTempFolders();
        }

        public async Task Run(string videoBlobName)
        {
            CleanTempFolders();
            
            _log.Info("Function Extract Frames From Video Started");
            _log.Info("Write blob to memory stream");
 
            var targetLocalVideoPath = Path.Combine(_localVideoPath, Path.GetFileName(videoBlobName));
            var targetVideoFileName = Path.GetFileNameWithoutExtension(targetLocalVideoPath);

            var appUserId = targetVideoFileName.Split(("_"))[0];
            var videoTimestampText = targetVideoFileName.Split(("_"))[1];
            var videoTimeStamp = DateTime.ParseExact(videoTimestampText, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            videoTimeStamp = videoTimeStamp.AddSeconds(2);

            using (var ftpDownloadStream = await _client.DownloadFromFtpAsMemoryStreamAsync(videoBlobName))
                using (var file = File.Create(targetLocalVideoPath))
                    file.Write(ftpDownloadStream.ToArray());

            var filesToUpload = CutVideo(targetLocalVideoPath);

            foreach (var fileName in filesToUpload.OrderBy(s => s).ToArray())
            {
                var newFileName = GenerateFrameFileName(appUserId, videoTimeStamp);
                await _client.UploadAsync(fileName, fileContainerName + Path.DirectorySeparatorChar, newFileName);
                await InsertNewFileFrameToDb(appUserId, newFileName, videoTimeStamp);
                RaiseNewFrameEvent(newFileName);
                
                videoTimeStamp = videoTimeStamp.AddSeconds(3);
            }
            
            CleanTempFolders();
            _log.Info("Function Extract Frames From Video finished");
        }

        private void RaiseNewFrameEvent(string filename)
        {
            var message = new FaceAnalyzeRun
            {
                Path = $"{fileContainerName}{Path.PathSeparator}{filename}"
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
        
        
        private void CreateTempFolders()
        {
            if (!Directory.Exists(_localVideoPath))
                Directory.CreateDirectory(_localVideoPath);

            if (!Directory.Exists(_localFramesPath))
                Directory.CreateDirectory(_localFramesPath);
        }

        private void CleanTempFolders()
        {
            foreach (var file in Directory.GetFiles(_localVideoPath))
            {
                File.Delete(file);
            }
            
            foreach (var file in Directory.GetFiles(_localFramesPath))
            {
                File.Delete(file);
            }
        }
        
        private string[] CutVideo(string localVideoFilePath, int quality = 10)
        {
            var fileName = Path.GetFileNameWithoutExtension(localVideoFilePath);
            
            var psi = new ProcessStartInfo("ffmpeg")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                Arguments = $"-hide_banner -i {localVideoFilePath} -r 1/3 -q:v {quality} " +
                            $"-f image2 {Path.Combine(_localFramesPath, fileName)}_%05d.jpg"
            };

            var process = new Process()
            {
                StartInfo = psi
            };
            
            process.Start();
            process.WaitForExit();
            
            return Directory.GetFiles(_localFramesPath, $"{fileName}*.jpg");
        }

        private string GenerateFrameFileName(string appUserId, DateTime timeStampForFrame)
        {
            var finalTimeStampString =
                timeStampForFrame.Year +
                timeStampForFrame.Month.ToString("D2") +
                timeStampForFrame.Day.ToString("D2") +
                timeStampForFrame.Hour.ToString("D2") +
                timeStampForFrame.Minute.ToString("D2") +
                timeStampForFrame.Second.ToString("D2");

            return $"{appUserId}_{finalTimeStampString}.jpg";
        }
    }
}