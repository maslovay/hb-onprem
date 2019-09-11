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
using Microsoft.Extensions.Configuration;
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
        private IConfiguration _configuration;

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
                    _configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    var oldSettings = new SftpSettings()
                    {
                        Host = "40.112.78.6",
                        Port = 22,
                        UserName = "nkrokhmal",
                        Password = "kloppolk_2018",
                        DestinationPath = "/home/nkrokhmal/storage/",
                        DownloadPath = "/opt/download/"
                        
                    };
                    var sftpCLientOld = new SftpClient(oldSettings, _configuration);
                    
                    var tasks = new List<Task>();
                    
                    var oldPath = await sftpCLientOld.ListDirectoryAsync("");
                    
                    foreach (var sftpFile in oldPath)
                    {
                        if (sftpFile.IsDirectory)
                        {
                            var files = await sftpCLientOld.ListDirectoryFiles(sftpFile.Name);
                            Parallel.ForEach(files, async (file) =>
                            {
                                using (var stream = await sftpCLientOld.DownloadFromFtpAsMemoryStreamAsync(sftpFile.Name + "/" + file))
                                {
                                   await _sftpClient.UploadAsMemoryStreamAsync(stream, sftpFile.Name, file);
                                   await sftpCLientOld.DeleteFileIfExistsAsync(sftpFile.Name + "/" + file);
                                }
                            });
                        }
                    }
                    _log.Info("Try to download and upload");
//                    foreach (var dialogue in dialogues)
//                    {
//                        foreach (var (key, value) in dict)
//                        { 
//                            var filePath = key + "/" + dialogues + value;
//                            var fileName = dialogues + value;
//                            var stream =  await _sftpClient.DownloadFromFtpAsMemoryStreamAsync(oldPath);
//                            tasks.Add(sftpCLientOld.UploadAsMemoryStreamAsync(stream,key,fileName));
//                        }
//
//                    }

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