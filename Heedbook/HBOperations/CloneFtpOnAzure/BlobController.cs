using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CloneFtpOnAzure
{
    public class BlobController
    {
        private StorageAccInfo _storageAccInfo;

        public BlobController(StorageAccInfo storageAccInfo)
        {
            _storageAccInfo = storageAccInfo;
        }

        public async Task UploadFileStreamToBlob(String filePath, Stream stream)
        {
            
            var splited = filePath.Split('/');
            var containerName = splited.First();
            var fileName = splited.Last();
            
            var storageCredentials = new StorageCredentials(_storageAccInfo.AccName, _storageAccInfo.AccKey);
            var cloudStorageAccount = new CloudStorageAccount(storageCredentials, true);
            var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            
            var container = cloudBlobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            var newBlob = container.GetBlockBlobReference(fileName);
            stream.Position = 0;
            await newBlob.UploadFromStreamAsync(stream);
        }
    }
}