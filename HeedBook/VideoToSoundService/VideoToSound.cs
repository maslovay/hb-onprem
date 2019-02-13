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
            var localVideoPath = await _sftpClient.DownloadFromFtpToLocalDiskAsync(path);
            var localAudioPath = $"/home/daniyar/333/{dialogueId}.wav";
            var ffPath = _configuration["FfmpegPath"];
            var ffmpeg = new FFMpegWrapper(_configuration["FfmpegPath"]);
            await ffmpeg.VideoToWavAsync(localVideoPath, localAudioPath);
            var uploadPath = Path.Combine("dialoguevideos", $"{dialogueId}.wav");
            if (File.Exists(localAudioPath))
            {
                await _sftpClient.UploadAsync(localAudioPath, "dialogueaudios", $"{dialogueId}.wav");
                File.Delete(localAudioPath);
                File.Delete(localVideoPath);
                var @event = new AudioAnalyzeRun
                {
                    Path = uploadPath
                };
                _handler.EventRaised(@event);
            }
        }
    }
}