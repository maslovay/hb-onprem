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

        public class GoogleSTT
        {
            public ILogger log;

            public string GetApiKey()
            {
                var collection = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>("google.accounts");

                // make request to get the documents
                var mask = new BsonDocument { { "Status", "Active" } };
                var docs = collection.Find(mask).ToList();

                if (docs.Count == 0)
                {
                    throw new Exception("No working google api keys found");
                }

                // choose random one
                var rnd = new Random();
                int r = rnd.Next(docs.Count);
                return docs[r]["Key"].ToString();
            }

            public string Recognize(string fn, int languageId, ILogger log2, bool enableWordTimeOffsets = true, bool enableSpeakerDiarization = true)
            {
                while (true)
                {
                    var apiKey = GetApiKey();

                    var jsStr = Retry.Do(() => { return RecognizeLongRunning(fn, apiKey, languageId, log2); }, TimeSpan.FromSeconds(1), 5);

                    dynamic js = JsonConvert.DeserializeObject(jsStr);
                    var error = js["error"];

                    log2.LogInformation($"{JsonConvert.SerializeObject(error)}");

                    if (error != null)
                    {
                        var collection = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>("google.accounts");
                        var mask = new BsonDocument { { "Key", apiKey } };

                        log2.LogInformation($"{error["status"] }");

                        if (((string)error["Status"] == "PERMISSION_DENIED"))
                        {
                            log2.LogInformation("Update 1");
                            /*{'error': {'code': 403,
                                        'details': [{'@type': 'type.googleapis.com/google.rpc.Help',
                                        'links': [{'description': 'Google developers console billing',
                                            'url': 'https://console.developers.google.com/billing/enable?project=sapient-zodiac-183008'}]}],
                                        'message': 'This API method requires billing to be enabled. Please enable billing on project sapient-zodiac-183008 by visiting https://console.developers.google.com/billing/enable?project=sapient-zodiac-183008 then retry. If you enabled billing for this project recently, wait a few minutes for the action to propagate to our systems and retry.',
                                        'status': 'PERMISSION_DENIED'}}*/
                            //log.Error("Failed to process google stt response. Making api key expired: {0}", apiKey);
                            collection.UpdateMany(mask, new BsonDocument { { "$set", new BsonDocument { { "Status", "Expired" } } } });
                        }
                        else if (((string)error["status"] == "PERMISSION_DENIED"))
                        {
                            log2.LogInformation("Update 2");
                            collection.UpdateMany(mask, new BsonDocument { { "$set", new BsonDocument { { "Status", "Expired" } } } });
                        }
                        else
                        {
                            log.LogError($"Failed to process google stt response. Making api key status error: {apiKey}");
                            //collection.UpdateMany(mask, new BsonDocument { { "$set", new BsonDocument { { "Status", "Error" } } } });
                        }
                        continue;
                    }
                    else
                    {
                        Dictionary<string, string> res = new Dictionary<string, string>();
                        var googleRecognitionId = js["name"].ToString();
                        res.Add("GoogleTransactionId", js["name"].ToString());
                        res.Add("GoogleApiKey", apiKey);
                        return JsonConvert.SerializeObject(res);
                    }

                }
            }

            public string RecognizeLongRunning(string fn, string apiKey, int languageId, ILogger log2, bool enableWordTimeOffsets = true)
            {

                httpClient.DefaultRequestHeaders.Accept.Clear();

                /*test example:
                    body = {
                    "config": {
                        "encoding":"FLAC",
                        "sampleRateHertz": 16000,
                        "languageCode": "en-US",
                        "enableWordTimeOffsets": true,
                        "enableSpeakerDiarization" : true
                    },
                    "audio": {
                        "uri":"gs://cloud-samples-tests/speech/brooklyn.flac"
                    }
                }*/
                var request = new
                {
                    config = new
                    {
                        encoding = "LINEAR16",
                        sampleRateHertz = 16000,
                        languageCode = GetLanguageName(languageId),
                        enableWordTimeOffsets = enableWordTimeOffsets,
                        //enableSpeakerDiarization,
                        //enableWordConfidence = true,
                        //enableAutomaticPunctuation = true
                        //model = 'video'
                    },
                    audio = new
                    {
                        uri = "gs://hbfiles/" + fn
                    }
                };

                var response = httpClient.PostAsync("https://speech.googleapis.com/v1/speech:longrunningrecognize?key=" + apiKey, new StringContent(JsonConvert.SerializeObject(request).ToString(), Encoding.UTF8, "application/json")).Result;
                //response.EnsureSuccessStatusCode();

                // dynamic dyn = response.Content.ReadAsAsync<dynamic>();
                return response.Content.ReadAsStringAsync().Result;
            }

            public static string GetLanguageName(int languageId)
            {
                switch (languageId)
                {
                    case 1:
                        return "en-US";
                    case 2:
                        return "ru-RU";
                    case 3:
                        return "es-ES";
                    case 4:
                        return "fr-FR";
                    case 5:
                        return "it-IT";
                    case 6:
                        return "pt-PT";
                    case 7:
                        return "tr-TR";
                    case 8:
                        return "de-DE";
                    case 9:
                        return "da-DK";
                    case 10:
                        return "hu-HU";
                    case 11:
                        return "nl-NL";
                    case 12:
                        return "nb-NO";
                    case 13:
                        return "pl-PL";
                    case 14:
                        return "vi-VN";
                    case 15:
                        return "ar-SA";
                    case 16:
                        return "hi-IN";
                    case 17:
                        return "th-TH";
                    case 18:
                        return "ko-KR";
                    case 19:
                        return "cmn-Hant-TW";
                    case 20:
                        return "yue-Hant-HK";
                    case 21:
                        return "ja-JP";
                    case 22:
                        return "cmn-Hans-HK";
                    case 23:
                        return "cmn-Hans-CN";
                    case 24:
                        return "pt-BR";
                    default:
                        return "en-US";
                }

            }
        }
    }
}