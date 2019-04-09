using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBLib;
using HBLib.Utils;
using Microsoft.Extensions.Configuration;
using Notifications.Base;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace VideoToSoundService
{
    public class VideoToSound
    {
        private readonly IConfiguration _configuration;
        private readonly SftpClient _sftpClient;
        private readonly INotificationPublisher _publisher;
        private readonly SftpSettings _sftpSettings;
        private readonly ElasticClient _log;

        public VideoToSound(IConfiguration configuration,
            SftpClient sftpClient,
            INotificationPublisher publisher,
            SftpSettings sftpSettings,
            ElasticClient log)
        {
            _configuration = configuration;
            _sftpClient = sftpClient;
            _publisher = publisher;
            _sftpSettings = sftpSettings;
            _log = log;
        }

        public async Task Run(String path)
        {
            try
            {
                var dialogueId = Path.GetFileNameWithoutExtension(path.Split('/').Last());
                var localVideoPath = await _sftpClient.DownloadFromFtpToLocalDiskAsync(path);
                var localAudioPath = $"{_sftpSettings.DownloadPath}{dialogueId}.wav";
                var ffmpeg = new FFMpegWrapper(_configuration["FfmpegPath"]);
                await ffmpeg.VideoToWavAsync(localVideoPath, localAudioPath);
                var uploadPath = Path.Combine("dialogueaudios", $"{dialogueId}.wav");
                if (File.Exists(localAudioPath))
                {
                    await _sftpClient.UploadAsync(localAudioPath, "dialogueaudios", $"{dialogueId}.wav");
                    File.Delete(localAudioPath);
                    File.Delete(localVideoPath);
                    var @event = new AudioAnalyzeRun
                    {
                        Path = uploadPath
                    };
                    var toneAnalyzeEvent = new ToneAnalyzeRun
                    {
                        Path = uploadPath
                    };
                    _publisher.Publish(@event);
                    _publisher.Publish(toneAnalyzeEvent);
                    _log.Info("message sent to rabbit. Wait for tone analyze and audio analyze");
                }
            }
            catch (Exception e)
            {
                _log.Fatal($"exception occured {e}");
                throw;
            }
        }
    }
}