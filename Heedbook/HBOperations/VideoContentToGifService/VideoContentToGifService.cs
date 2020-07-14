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

namespace VideoContentToGifService
{
    public class VideoContentToGif
    {
        private readonly ElasticClient _log;
        private readonly INotificationPublisher _publisher;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        private readonly FFMpegWrapper _wrapper;
        private readonly ElasticClientFactory _elasticClientFactory;


        public VideoContentToGif(SftpClient sftpClient,
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
            System.Console.WriteLine($"started");
            var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{Path}");
            _log.SetArgs(path);
            try
            {
                _log.Info($"Function started with path: {path}");
                if(await _sftpClient.IsFileExistsAsync(path))
                {
                    System.Console.WriteLine($"path: {path}");

                    await _sftpClient.DownloadFromFtpToLocalDiskAsync(path, Directory.GetCurrentDirectory());
                    var localVideoName = Path.GetFileName(path);
                    var localFileName = Path.GetFileNameWithoutExtension(path);
                    System.Console.WriteLine(localVideoName);
                    var localGifName = localFileName + ".gif";

                    System.Console.WriteLine(localGifName);
                    await _wrapper.VideoToGifAsync(localVideoName, localGifName);
                    

                    await _sftpClient.CreateIfDirNoExistsAsync("gif");
                    await _sftpClient.UploadAsync(localGifName, "gif", $"{localGifName}");
                    System.Console.WriteLine($"ended");

                    File.Delete(localVideoName);
                    File.Delete(localGifName);
                }
                
            }
            catch (SftpPathNotFoundException e)
            {
                _log.Fatal($"{e}");
            }
            catch (Exception e)
            {
                _log.Fatal($"exception occured {e}");
                System.Console.WriteLine(e);
                throw;
            }
        }
    }
}