using System;
using System.IO;
using System.Threading.Tasks;
using HBLib;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace HBLib.Utils
{
    public class BlobClient
    {
        private BlobSettings _blobSettings;
        private CloudBlobClient _cloudBlobClient;
        public BlobClient(BlobSettings blobSettings)
        {
            _blobSettings = blobSettings;   
            var storageCredentials = new StorageCredentials(_blobSettings.AccName, _blobSettings.AccKey);
            var cloudStorageAccount = new CloudStorageAccount(storageCredentials, true);
            _cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();         
        }        
        public string CreateSasUri(string containerName, string fileName)
        {
            var container = _cloudBlobClient.GetContainerReference(containerName);
            var newBlob = container.GetBlockBlobReference(fileName);
            var policy = new SharedAccessBlobPolicy()
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(6),
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddHours(-1)
            };
            var sas = newBlob.GetSharedAccessSignature(policy);
            var url = new Uri(_blobSettings.resourceUri + $"{sas}").ToString();
            return url;
        }
        public async Task UploadFileStreamToBlob(String containerName, String fileName, Stream stream)
        {
            var container = _cloudBlobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            var newBlob = container.GetBlockBlobReference(fileName);
            stream.Position = 0;
            await newBlob.UploadFromStreamAsync(stream);
        }        
    }
}