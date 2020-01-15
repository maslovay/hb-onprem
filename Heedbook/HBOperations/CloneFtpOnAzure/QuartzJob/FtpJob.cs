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

                    var dialogues = _context.Dialogues
                        .Where(d => d.Status.StatusId == 3 &&
                                    d.CreationTime >= DateTime.UtcNow.AddHours(-24))
                        .Select(s => s.DialogueId)
                        .ToList();
                    var tasks = new List<Task>();
                    var dict = new Dictionary<String, String>()
                    {
                        {_storageAccInfo.AvatarName, ".jpg"},
                        {_storageAccInfo.VideoName, ".mkv"},
                        {_storageAccInfo.AudioName, ".wav"}
                    };
                    _log.Info("Try to download and upload");
                    foreach (var dialogue in dialogues)
                    {
                        foreach (var (key, value) in dict)
                        {
                            var filePath = key + "/" + dialogue + value;
                            var stream =  await _sftpClient.DownloadFromFtpAsMemoryStreamAsync(filePath);
                            tasks.Add(_blobController.UploadFileStreamToBlob(filePath, stream));
                        }                        
                    }
                    
                    await Task.WhenAll(tasks);
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