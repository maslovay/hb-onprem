using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBLib.Utils;
using Microsoft.Extensions.Configuration;
using Notifications.Base;
using RabbitMqEventBus.Events;

namespace VideoToSoundService
{
    public class VideoToSound
    {
        private readonly IConfiguration _configuration;
        private readonly SftpClient _sftpClient;
        private readonly INotificationHandler _handler;

        public VideoToSound(IConfiguration configuration, 
            SftpClient sftpClient,
            INotificationHandler handler)
        {
            _configuration = configuration;
            _sftpClient = sftpClient;
            _handler = handler;
        }
        
        public async Task Run(String path)
        {
            var dialogueId = Path.GetFileNameWithoutExtension(path.Split('/').Last());
            var localVideoStream = await _sftpClient.DownloadFromFtpAsMemoryStreamAsync(path);
            var ffmpeg = new FFMpegWrapper(_configuration["FfmpegPath"]);
            var streamForUpload = await ffmpeg.VideoToWavAsync(localVideoStream);
            var uploadPath = Path.Combine("dialoguevideos", $"{dialogueId}.wav");
            if (streamForUpload != null)
            {
                await _sftpClient.UploadAsMemoryStreamAsync(streamForUpload, "dialoguevideos", $"{dialogueId}.wav");
            }
            var @event = new AudioAnalyzeRun
            {
                Path = uploadPath
            };
            _handler.EventRaised(@event);
        }
    }
}