using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CloneFtpOnAzure
{
    public  class BlobController
    {
        private CloudBlobContainer containerReference;

        public async Task ConnectToBlob()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("<backupp>_DefaultEndpointsProtocol=https;AccountName=backupp;AccountKey=pb/WS9Pc3x9cpz1l09T7/If4E9bDXnyEKgvReGbFtbvhBEoS8IKelvU1dcU1G3XKx+4sP2sfo1bMY//Dz3Ng1Q==;EndpointSuffix=core.windows.net"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer containerReference = blobClient.GetContainerReference("dialoguevideos");
        }
        
        public async Task UploadBlob(string path, string name)
        {
            CloudBlobContainer container = containerReference;
            CloudBlockBlob blob = container.GetBlockBlobReference("dialoguevideos");
            using (var fileStream = System.IO.File.OpenRead(@"Path"))
            {
              await blob.UploadFromStreamAsync(fileStream);
            }
            
        }
    }
}