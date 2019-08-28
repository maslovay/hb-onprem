using System;
using System.IO;
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

        public async Task UploadFileStreamToBlob(MemoryStream stream, string name, string containerName)
        {
            try
            {
                var storageCredentials = new StorageCredentials(_storageAccInfo.AccName, _storageAccInfo.AccKey);
                var cloudStorageAccount = new CloudStorageAccount(storageCredentials, true);
                var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                var container = cloudBlobClient.GetContainerReference(containerName);
                await container.CreateIfNotExistsAsync();
                var newBlob = container.GetBlockBlobReference(name);
                await newBlob.UploadFromStreamAsync(stream);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}