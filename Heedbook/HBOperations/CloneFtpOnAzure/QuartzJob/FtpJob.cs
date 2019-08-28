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

namespace CloneFtpOnAzure
{
    public class FtpJob : IJob
    {
        private SftpClient _sftpClient;
        private BlobController _blobController;
        private readonly ElasticClientFactory _elasticClientFactory;
        private StorageAccInfo _storageAccInfo;
        private RecordsContext _context;
        public FtpJob(SftpClient sftpClient,
            BlobController blobController,
        ElasticClientFactory elasticClientFactory,
            StorageAccInfo storageAccInfo,
            RecordsContext context)
        {
            _context = context;
            _storageAccInfo = storageAccInfo;
            _elasticClientFactory = elasticClientFactory;
            _blobController = blobController;
            _sftpClient = sftpClient;
        }
        
        public async Task Execute(IJobExecutionContext context)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            try
            {
                var dialogue = _context.Dialogues
                    .Where(d => d.Status.StatusId == 3 &&
                                d.CreationTime >= DateTime.UtcNow.AddHours(-24))
                    .Select(s=>s.DialogueId);
                foreach (var qq in dialogue)
                {
                    var avatar = qq + ".jpg";
                    var video = qq + ".mkv";
                    var audio = qq + ".wav";
                    var avstream = await _sftpClient.DownloadFromFtpAsMemoryStreamAsync("clientavatars/" + avatar);
                 await _blobController.UploadFileStreamToBlob(avstream,
                          avatar, "clientavatars");
                    var vistream = await _sftpClient.DownloadFromFtpAsMemoryStreamAsync("dialoguevideos/" + video);
                  await _blobController.UploadFileStreamToBlob(vistream,
                      avatar, "dialoguevideos");
                    var austream = await _sftpClient.DownloadFromFtpAsMemoryStreamAsync("dialogueaudios/" + audio);  
                   await _blobController.UploadFileStreamToBlob(austream,
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