using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HBLib.AzureFunctions;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OperationService.Legacy
{
    public static class AudioBlobSttGoogleLongrunning
    {

        private static HttpClient httpClient = new HttpClient();

        [FunctionName("Audio_Blob_STTGoogleLongrunning")]
        public static async Task Run(string msg,
            //public static async Task Run([ServiceBusTrigger("test", "test", AccessRights.Manage, Connection = "heedbook_SERVICEBUS")]string msg,
            ILogger log,
            ExecutionContext dir)
        {

            // load blob metadata
            var msgSplit = Regex.Split(msg, "/");
            var blobContainerName = msgSplit[0];
            var blobName = msgSplit[1];
            var name = blobName;


            var blob = HeedbookMessengerStatic.BlobStorageMessenger.GetBlob(blobContainerName, blobName);
            await blob.FetchAttributesAsync();
            var blobMetadata = blob.Metadata;

            var applicationUserId = blobMetadata["ApplicationUserId"];
            var t = blobMetadata["BegTime"];
            var fileBegTime = DT.Parse(t);
            var languageId = Convert.ToInt32(blobMetadata["LanguageId"]);
            var dialogueId = Path.GetFileNameWithoutExtension(name);

            var blobGoogleDriveName = blobContainerName != EnvVar.Get("BlobContainerDialogueAudiosEmp")
                        ? dialogueId + "_client" + Path.GetExtension(name)
                        : dialogueId + "_emp" + Path.GetExtension(name);

            var googleSTT = new GoogleSTT { log = log };

            try
            {
                //log2.Info("Processing file blob name - {0}, googlefilename - {1}", name, blobGoogleDriveName);
                var binPath = Misc.BinPath(dir);

                //recognize the speech
                var jsStr = googleSTT.Recognize(blobGoogleDriveName, languageId, log, true, blobContainerName != EnvVar.Get("BlobContainerDialogueAudiosEmp"));
                //log2.Info($"Google STT recogntion results {jsStr.ToString()}");

                dynamic js = JsonConvert.DeserializeObject(jsStr);

                if (((JObject)js).Count == 0)
                {
                    log.LogInformation("Empty result recognition");
                }
                else
                {
                    var doc = new BsonDocument { };
                    var collection = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionAudioSTTGoogle"));
                    doc["ApplicationUserId"] = applicationUserId;
                    doc["DialogueId"] = dialogueId;
                    doc["Time"] = fileBegTime;
                    doc["CreationTime"] = DateTime.UtcNow;
                    doc["BlobName"] = name;
                    doc["BlobContainerName"] = blobContainerName;
                    doc["Status"] = "InProgress";
                    doc["GoogleTransactionId"] = js.GoogleTransactionId.ToString();
                    doc["GoogleApiKey"] = js.GoogleApiKey.ToString();
                    doc["BlobGoogleDriveName"] = blobGoogleDriveName;
                    doc["IsClient"] = blobContainerName != EnvVar.Get("BlobContainerDialogueAudiosEmp")
                        ? true
                        : false;

                    try
                    {
                        HeedbookMessengerStatic.MongoDBMessenger.SafeReplaceOne(collection,
                                     new BsonDocument {
                                         { "ApplicationUserId", applicationUserId},
                                         { "DialogueId", dialogueId} ,
                                         { "BlobGoogleDriveName", doc["BlobGoogleDriveName"] }
                                     }, doc);
                    }
                    catch (Exception e)
                    {
                        log.LogError("Mongo connection error occured {0}", e);
                    }

                    log.LogInformation($"Function finished: {dir.FunctionName}");
                }
            }
            catch (Exception e)
            {
                log.LogError($"Exception occured {e}");
                throw e;
            }
        }
    }
}