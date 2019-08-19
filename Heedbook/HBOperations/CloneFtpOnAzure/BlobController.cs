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
        public async Task UploadFileToBlob(string path, string name,string containerName)
        {
            var storageCredentials =  new StorageCredentials("backupp",  "pb/WS9Pc3x9cpz1l09T7/If4E9bDXnyEKgvReGbFtbvhBEoS8IKelvU1dcU1G3XKx+4sP2sfo1bMY//Dz3Ng1Q==");
            var cloudStorageAccount =  new CloudStorageAccount(storageCredentials,  true);
            var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            var container = cloudBlobClient.GetContainerReference($"{containerName}");
            await container.CreateIfNotExistsAsync();
            var newBlob =  container.GetBlockBlobReference($"{name}");
            await newBlob.UploadFromFileAsync($"{path}");
        }
        
    }
}