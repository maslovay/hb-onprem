using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using ExtractFramesFromVideo.Utils;
using System.Text;
using System.Threading;
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
        private byte[] jpegBegin = {0xFF, 0xD8};
        private byte[] jpegEnd = {0xFF, 0xD9};
        private const int bufferSize = 4096;
        
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
        }

        public async Task Run(string videoBlobName)
        {
            _log.Info("Function Extract Frames From Video Started");
            _log.Info("Write blob to memory stream");
 
            var targetLocalVideoPath = Path.Combine(_localVideoPath, Path.GetFileName(videoBlobName));
            var targetVideoFileName = Path.GetFileNameWithoutExtension(targetLocalVideoPath);

            var appUserId = targetVideoFileName.Split(("_"))[0];
            var videoTimestampText = targetVideoFileName.Split(("_"))[1];
            var videoTimeStamp = DateTime.ParseExact(videoTimestampText, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            videoTimeStamp = videoTimeStamp.AddSeconds(2);
     
            using (var ftpDownloadStream = await _client.DownloadFromFtpAsMemoryStreamAsync("videos/" + videoBlobName))
            {
                CutVideo(ftpDownloadStream, new MemoryStream(), videoTimeStamp, appUserId, 5 );                
            }

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
        
        private async void CutVideo(MemoryStream sourceStream, 
            MemoryStream uploadStream,
            DateTime dateTime,
            string appUserId,
            int quality = 10)
        {
            var psi = new ProcessStartInfo("ffmpeg")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                Arguments = $"-hide_banner -i pipe:0 -r 1/3 -q:v {quality} -f image2 -update 1 pipe:1"
            };

            var process = new Process()
            {
                StartInfo = psi
            };
            
            process.Start();

            
            using (var inputStream = process.StandardInput.BaseStream)
            {
                var tt = sourceStream.ToArray();
                sourceStream.Position = 0;
                
                sourceStream.Write(tt, 0, tt.Length);
            }

            using (var outputStream = new BufferedStream(process.StandardOutput.BaseStream, bufferSize))
            {
                using (BinaryReader br = new BinaryReader(outputStream, Encoding.ASCII))
                {
                    var shortBuffer = new byte[2];
                    int len = 0;

                    do
                    {
                        len = br.Read(shortBuffer);

                        if (len == 0)
                            break;
                        
                        while (shortBuffer[0] != jpegBegin[0] && shortBuffer[1] != jpegBegin[1]) 
                            continue;

                        uploadStream.Flush();
                        uploadStream.SetLength(0);
                        uploadStream.Seek(0, SeekOrigin.Begin);
                        uploadStream.Write(shortBuffer);

                        do
                        {
                            len = br.Read(shortBuffer);
                            uploadStream.Write(shortBuffer);
                        } while (shortBuffer[0] != jpegEnd[0] && shortBuffer[1] != jpegEnd[1] && len > 0);
                        
                        if (uploadStream.Length > 0)
                            await _client.UploadAsMemoryStreamAsync(uploadStream, "videos/",
                                GenerateFrameFileName(appUserId, dateTime));

                        dateTime = dateTime.AddSeconds(3);
                    } while (len > 0);
                }
            }
            
            
            process.WaitForExit();
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