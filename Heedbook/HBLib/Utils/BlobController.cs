using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace HBLib.Utils
{
    public class BlobController
    {
        private StorageAccInfo _storageAccInfo;
        public BlobController(StorageAccInfo storageAccInfo)
        {
            _storageAccInfo = storageAccInfo;
        }

        private CloudBlobClient ConnectToAzureBlob()
        {
            var storageCredentials = new StorageCredentials(_storageAccInfo.AccName, _storageAccInfo.AccKey);
            var cloudStorageAccount = new CloudStorageAccount(storageCredentials, true);
            return cloudStorageAccount.CreateCloudBlobClient();
        }

        public async Task<bool> IsFileExists(string filename, string container)
        {
            var cloudBlobClient = ConnectToAzureBlob();
            var containerReference = cloudBlobClient.GetContainerReference(container);
            var blob = containerReference.GetBlockBlobReference(filename);
            return await blob.ExistsAsync();
        }
        public async Task UploadFileStreamToBlob(String filePath, Stream stream)
        {
            var cloudBlobClient = ConnectToAzureBlob();
            var splited = filePath.Split('/');
            var containerName = splited.First();
            var fileName = splited.Last();
            var container = cloudBlobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            var newBlob = container.GetBlockBlobReference(fileName);
            await newBlob.UploadFromStreamAsync(stream);
        }
    }
}