using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        public SftpClient _sftpClient;
        public BlobController _blobController;
        private readonly ElasticClientFactory _elasticClientFactory;
        private StorageAccInfo _storageAccInfo;
        public FtpJob(SftpClient sftpClient,
            BlobController blobController,
        ElasticClientFactory elasticClientFactory,
            StorageAccInfo storageAccInfo)
        {
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
                string[] path = _storageAccInfo.DirectoryName;
                var files = new Dictionary<string, ICollection<String>>();
                path.Select(async item => files[item] = await _sftpClient.ListDirectoryFiles(item)).ToList();
                _log.Info("Try to DownloadOnFtp and UploadBlobOnAzure");
                foreach (var file in files)
                {
                    foreach (var fileName in file.Value)
                    {
                        
                        var localPath = await _sftpClient.DownloadFromFtpToLocalDiskAsync(file.Key + "/" + fileName);
                        if (_sftpClient.GetLastWriteTime(localPath) <= DateTime.UtcNow.AddHours(-24))
                        {
                            await _blobController.UploadFileToBlob(localPath,
                                Path.Combine(file.Key + Path.GetFileName(localPath)), file.Key);
                        }

                        File.Delete(localPath);
                    }
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