using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBLib;
using HBLib.Utils;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Quartz;
using Microsoft.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace CloneFtpOnAzure
{
    public class FtpJob : IJob
    {
        private SftpClient _sftpClient;
        private BlobController _blobController;
        private readonly ElasticClientFactory _elasticClientFactory;
        private StorageAccInfo _storageAccInfo;
        private RecordsContext _context;
        private readonly IServiceScopeFactory _scopeFactory;
        public FtpJob(IServiceScopeFactory scopeFactory,
            SftpClient sftpClient,
            BlobController blobController,
        ElasticClientFactory elasticClientFactory,
            StorageAccInfo storageAccInfo)
        {
            _scopeFactory = scopeFactory;
            _storageAccInfo = storageAccInfo;
            _elasticClientFactory = elasticClientFactory;
            _blobController = blobController;
            _sftpClient = sftpClient;
        }
        
        public async Task Execute(IJobExecutionContext context)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _log = _elasticClientFactory.GetElasticClient();
                try
                {
                    _context = scope.ServiceProvider.GetRequiredService<RecordsContext>();
                    
                    var dialogue = _context.Dialogues
                        .Where(d => d.Status.StatusId == 3 &&
                                    d.CreationTime >= DateTime.UtcNow.AddHours(-24))
                        .Select(s => s.DialogueId)
                        .ToList();
                    foreach (var qq in dialogue)
                    {
                        var avatar = qq + ".jpg";
                        var video = qq + ".mkv";
                        var audio = qq + ".wav";
                        
                        var avatarStream = await _sftpClient.DownloadFromFtpAsMemoryStreamAsync("clientavatars/" + avatar);
                        await _blobController.UploadFileStreamToBlob(avatarStream,
                            avatar, "clientavatars");
                        var videosStream = await _sftpClient.DownloadFromFtpAsMemoryStreamAsync("dialoguevideos/" + video);
                        await _blobController.UploadFileStreamToBlob(videosStream,
                            avatar, "dialoguevideos");
                        var audioStream = await _sftpClient.DownloadFromFtpAsMemoryStreamAsync("dialogueaudios/" + audio);
                        await _blobController.UploadFileStreamToBlob(audioStream,
                            avatar, "dialogueaudios");
                    }

                    _log.Info("Download and Upload finished");

                }
                catch (Exception e)
                {
                    _log.Fatal($"{e}");
                    throw;
                }
            }
        }
    }
}