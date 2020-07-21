using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HBLib;
using HBLib.Utils;
using Microsoft.Extensions.Configuration;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using Renci.SshNet.Common;

namespace VideoConvertToMp4
{
    public class VideoConvertToMp4
    {
        private readonly ElasticClient _log;
        private readonly INotificationPublisher _publisher;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        private readonly FFMpegWrapper _wrapper;
        private readonly ElasticClientFactory _elasticClientFactory;


        public VideoConvertToMp4(SftpClient sftpClient,
            INotificationPublisher publisher,
            SftpSettings sftpSettings,
            ElasticClientFactory elasticClientFactory,
            FFMpegWrapper wrapper)
        {
            _sftpClient = sftpClient;
            _publisher = publisher;
            _sftpSettings = sftpSettings;
            _wrapper = wrapper;
            _elasticClientFactory = elasticClientFactory;
        }

        public async Task Run(String dialogueId)
        {
            System.Console.WriteLine($"started: {dialogueId}");
            var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{Path}");
            _log.SetArgs(dialogueId);
            try
            {
                var fullPath = $"dialoguevideos/{dialogueId}.mkv";
                var localInputVideoPath = await _sftpClient.DownloadFromFtpToLocalDiskAsync(fullPath);
                var localOutputVideoPath = $"{dialogueId}.mp4";
                await _wrapper.ConvertMkvToMp4Async(localInputVideoPath, localOutputVideoPath);
                await _sftpClient.UploadAsync(localOutputVideoPath, "dialoguevideos", $"{localOutputVideoPath}");
                _log.Info($"{localOutputVideoPath} uploaded on ftp");

                File.Delete(localInputVideoPath);
                File.Delete(localOutputVideoPath);
            }
            catch (SftpPathNotFoundException e)
            {
                _log.Fatal($"{e}");
            }
            catch (Exception e)
            {
                _log.Fatal($"exception occured {e}");
                throw;
            }
        }
    }
}