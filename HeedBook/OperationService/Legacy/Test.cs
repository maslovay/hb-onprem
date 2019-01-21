using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace OperationService.Legacy
{
    public static class Test
    {
        public static DateTime UnixEpoch { get; private set; }

        [FunctionName("test")]
        public static async Task<HttpResponseMessage> Run(
            HttpRequestMessage req,
            ILogger log)
        {
            {
                var blobName = "178bd1e8-e98a-4ed9-ab2c-ac74734d1903_20180806084037_2.mkv";
                var blobContainerName = "videos";
                log.LogInformation($"{blobName}");

                var sas = HeedbookMessengerStatic.BlobStorageMessenger.GetBlobSASUrl(blobContainerName, blobName);
                log.LogInformation($"1");
                log.LogInformation($"{sas}");

                log.LogInformation($"1");

                log.LogInformation("2");


                var res2 = HeedbookIdentity.IdentificationMessenger.CreateLargePersonGroup("1", "1", "Heedbook");
                var jsonToReturn = "";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(jsonToReturn, Encoding.UTF8, "application/json")
                };
            }
        }
    }
}