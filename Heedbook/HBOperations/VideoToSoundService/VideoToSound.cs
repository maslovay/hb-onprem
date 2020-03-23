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

namespace VideoToSoundService
{
    public class VideoToSound
    {
        private readonly ElasticClient _log;
        private readonly INotificationPublisher _publisher;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        private readonly FFMpegWrapper _wrapper;
        private readonly ElasticClientFactory _elasticClientFactory;


        public VideoToSound(SftpClient sftpClient,
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

        public async Task Run(String path)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{Path}");
            _log.SetArgs(path);
            try
            {
                _log.Info("Function started");
                var dialogueId = Path.GetFileNameWithoutExtension(path.Split('/').Last());
                var localVideoPath = await _sftpClient.DownloadFromFtpToLocalDiskAsync(path);
                var localAudioPath = Path.Combine(_sftpSettings.DownloadPath, dialogueId + ".wav");
                await _wrapper.VideoToWavAsync(localVideoPath, localAudioPath);
                var uploadPath = Path.Combine("dialogueaudios", $"{dialogueId}.wav");
                if (File.Exists(localAudioPath))
                {
                    if (await _wrapper.IsAudioStereo(localAudioPath))
                    {
                        _log.Info("Processing stereo audio");
                        var localAudioPathLeft = Path.Combine(_sftpSettings.DownloadPath, dialogueId + "_left.wav");
                        var localAudioPathRight = Path.Combine(_sftpSettings.DownloadPath, dialogueId + "_right.wav");
                        await _wrapper.SplitAudioToMono(localAudioPath,localAudioPathLeft, localAudioPathRight );
                        await _sftpClient.UploadAsync(localAudioPathLeft, "dialogueaudios", $"{dialogueId}.wav");
                        await _sftpClient.UploadAsync(localAudioPathRight, "dialogueaudiosemp", $"{dialogueId}.wav");
                        
                        var uploadPathEmp = Path.Combine("dialogueaudiosemp", $"{dialogueId}.wav");
                        File.Delete(localAudioPath);
                        File.Delete(localVideoPath);
                        File.Delete(localAudioPathLeft);
                        File.Delete(localAudioPathRight);

                        var audioAnalyzeEvent = new AudioAnalyzeRun
                        {
                            Path = uploadPath
                        };
                        var toneAnalyzeEvent = new ToneAnalyzeRun
                        {
                            Path = uploadPath
                        };

                        var audioAnalyzeEmpEvent = new AudioAnalyzeRun
                        {
                            Path = uploadPathEmp
                        };
                        var toneAnalyzeEmpEvent = new ToneAnalyzeRun
                        {
                            Path = uploadPathEmp
                        };
                        _publisher.Publish(audioAnalyzeEvent);
                        Thread.Sleep(100);
                        _publisher.Publish(toneAnalyzeEvent);
                        Thread.Sleep(100);
                        _publisher.Publish(audioAnalyzeEmpEvent);
                        Thread.Sleep(100);
                        _publisher.Publish(toneAnalyzeEmpEvent);
                        _log.Info("message sent to rabbit. Wait for tone analyze and audio analyze");
                    }
                    else
                    {
                        _log.Info("Processing mono audio");
                        await _sftpClient.UploadAsync(localAudioPath, "dialogueaudios", $"{dialogueId}.wav");
                        File.Delete(localAudioPath);
                        File.Delete(localVideoPath);

                        var audioAnalyzeEvent = new AudioAnalyzeRun
                        {
                            Path = uploadPath
                        };
                        var toneAnalyzeEvent = new ToneAnalyzeRun
                        {
                            Path = uploadPath
                        };
                        _publisher.Publish(audioAnalyzeEvent);
                        _publisher.Publish(toneAnalyzeEvent);
                        _log.Info("message sent to rabbit. Wait for tone analyze and audio analyze");
                    }
                }
                _log.Info("Function finished");

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