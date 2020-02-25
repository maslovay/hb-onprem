using System;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Web;
using CloneFtpOnAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace LinkToBlobController
{
    public class CreateLinkToBlob
    {
        private StorageAccInfo _storageAccInfo;

        public CreateLinkToBlob(StorageAccInfo storageAccInfo)
        {
            _storageAccInfo = storageAccInfo;
        }
        public string CreateSasUri(string resourceUri, string containerName, string fileName)
        {
            var storageCredentials = new StorageCredentials(_storageAccInfo.AccName, _storageAccInfo.AccKey);
            var cloudStorageAccount = new CloudStorageAccount(storageCredentials, true);
            var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();

            var container = cloudBlobClient.GetContainerReference(containerName);
            var newBlob = container.GetBlockBlobReference(fileName);
            var policy = new SharedAccessBlobPolicy()
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(6),
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddHours(-1)
            };
            var sas = newBlob.GetSharedAccessSignature(policy);
            var url = new Uri(resourceUri + $"{sas}").ToString();
            return url;
        }
        
    }
}