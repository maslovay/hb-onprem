using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using HBLib.AzureFunctions;
using HBLib.Extensions;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OperationService.Legacy {
    public static class AudioBlobSttGoogle
    {

        private static HttpClient httpClient = new HttpClient();

        [FunctionName("Audio_Blob_STTGoogle")]
        public static void Run(
            string msg,
            ExecutionContext dir,
            ILogger log
        )
        {
            var sessionId = Misc.GenSessionId();

            // load blob metadata
            var msgSplit = Regex.Split(msg, "/");
            var blobContainerName = msgSplit[0];
            var blobName = msgSplit[1];
            var name = blobName;

            var blob = HeedbookMessengerStatic.BlobStorageMessenger.GetBlob(blobContainerName, blobName);
            blob.FetchAttributesAsync();
            var blobMetadata = blob.Metadata;

            var applicationUserId = blobMetadata["ApplicationUserId"];
            var t = blobMetadata["BegTime"];
            var dt = DT.Parse(t);
            var langId = Convert.ToInt32(blobMetadata["LanguageId"]);
            var dialogueId = Path.GetFileNameWithoutExtension(name);

            var googleSTT = new GoogleSTT { log = log };

            try
            {
                //log.Info($"Processing file {name}");

                var localDir = Misc.GenLocalDir(sessionId);
                try
                {
                    var ffmpeg = new FFMpegWrapper(Path.Combine(Misc.BinPath(dir), "ffmpeg.exe"));


                    var fullAudioFn = Path.Combine(localDir, $"fullAudio.wav");

                    using (var output = new System.IO.FileStream(fullAudioFn, FileMode.Create))
                    {
                        blob.DownloadToStreamAsync(output);
                    }

                    var metadata = ffmpeg.SplitBySeconds(fullAudioFn, 15.0, localDir);

                    /*{
                      "words": [
                        {
                          "begTime": "0.200s",
                          "endTime": "1.200s",
                          "word": "some"
                        },
                        {
                          "begTime": "1.200s",
                          "endTime": "1.400s",
                          "word": "text"
                        }
                      ]
                    }*/
                    var words = new BsonArray();
                    var ticktack = 0;
                    foreach (var curMetadata in metadata)
                    {
                        ticktack += 1;
                        //log.Info("Proceeded {ticktack}, {metadata.Count}", ticktack.ToString(), metadata.Count.ToString());
                        var curFn = curMetadata["fn"];
                        //string newFn = applicationUserId + '_' + DT.Format(dt) + "_" + langId.ToString() + ".wav";

                        /* {
                          "results": [
                            {
                              "alternatives": [
                                {
                                  "transcript": "some text",
                                  "confidence": 0.9122468,
                                  "words": [
                                    {
                                      "startTime": "0.200s",
                                      "endTime": "1.200s",
                                      "word": "some"
                                    },
                                    {
                                      "startTime": "1.200s",
                                      "endTime": "1.400s",
                                      "word": "text"
                                    }
                                  ]
                                }
                              ]
                            }
                          ]
                        }
                        */
                        var jsStr = googleSTT.Recognize(curFn, langId);
                        // bad workaround to get json and bson
                        dynamic js = JsonConvert.DeserializeObject(jsStr);

                        if (((JObject)js).Count == 0)
                        {
                            //log.Info($"Empty!");
                        }
                        else
                        {
                            foreach (var wordJs in js.results[0].alternatives[0].words)
                            {
                                string begTimeStr = (wordJs.startTime).ToString();
                                string endTimeStr = (wordJs.endTime).ToString();
                                string word = wordJs.word;
                                var begTime = dt.AddSeconds(Convert.ToDouble(begTimeStr.Remove(begTimeStr.Length - 1)));
                                var endTime = dt.AddSeconds(Convert.ToDouble(endTimeStr.Remove(endTimeStr.Length - 1)));
                                words.Add(new BsonDocument { { "BegTime", begTime } ,
                                                     { "EndTime", endTime},
                                                     { "Word", word} });
                            }
                        }

                        dt = dt.AddSeconds(15);
                        //log.Info("Success {ticktack}, {metadata.Count}", ticktack.ToString(), metadata.Count.ToString());
                    }
                    //log.Info("Connect to MongoDB");
                    var wordsDoc = new BsonDocument { { "Words", words } };
                    var doc = new BsonDocument { { "Value", wordsDoc } };

                    var collection = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionAudioSTTGoogle"));

                    doc["ApplicationUserId"] = applicationUserId;
                    doc["DialogueId"] = dialogueId;
                    doc["Time"] = dt;
                    doc["CreationTime"] = DateTime.Now;
                    doc["BlobName"] = name;
                    doc["Status"] = "Active";

                    //log.Info("Adding data to MongoDB");
                    try
                    {
                        HeedbookMessengerStatic.MongoDBMessenger.SafeReplaceOne(collection,
                                     new BsonDocument { { "ApplicationUserId", applicationUserId},
                                                         { "DialogueId", dialogueId} },
                                     doc);
                    }
                    catch
                    {
                        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(30));
                        try
                        {
                            HeedbookMessengerStatic.MongoDBMessenger.SafeReplaceOne(collection,
                                        new BsonDocument { { "ApplicationUserId", applicationUserId},
                                                         { "DialogueId", dialogueId} },
                                        doc);
                        }
                        catch (Exception e)
                        {
                            //log.Info("Mongo connection error occured {e}", e);
                        }
                    }

                    //log.Info("Added document to mongodb database: {doc}", doc.ToJson());

                    var publishJs = new Dictionary<string, string> { { "DialogueId", dialogueId } };
                    HeedbookMessengerStatic.ServiceBusMessenger.Publish(EnvVar.Get("TopicAudioSTT"), publishJs.JsonPrint());

                    // delete all files
                    var fns = OS.GetFiles(localDir, "*");
                    foreach (var fn in fns)
                    {
                        OS.SafeDelete(fn);
                    }
                    OS.SafeDelete(localDir);
                }
                catch (Exception e)
                {
                    OS.SafeDelete(localDir);
                }
                //log.Info($"Function finished: {dir.FunctionName}");
            }
            catch (Exception e)
            {
                //log.Critical("Exception occured {e}", e);
                throw;
            }
        }

        public class GoogleSTT
        {
            public ILogger log;

            public string GetApiKey()
            {
                var collection = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>("google.accounts");

                // make request to get the documents
                var mask = new BsonDocument { { "status", "active" } };
                var docs = collection.Find(mask).ToList();

                if (docs.Count == 0)
                {
                    throw new Exception("No working google api keys found");
                }

                // choose random one
                var rnd = new Random();
                int r = rnd.Next(docs.Count);
                return docs[r]["key"].ToString();
            }

            public string Recognize(string fn, int languageId, bool enableWordTimeOffsets = true)
            {
                while (true)
                {
                    var apiKey = GetApiKey();

                    var jsStr = Retry.Do(() => { return Recognize(fn, apiKey, languageId); }, TimeSpan.FromSeconds(1), 5);

                    // bad workaround to get json and bson
                    dynamic js = JsonConvert.DeserializeObject(jsStr);

                    var error = js["error"];

                    if (error != null)
                    {
                        var collection = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>("google.accounts");
                        var mask = new BsonDocument { { "key", apiKey } };

                        if ((error["status"] != null) && ((string)error["status"] == "PERMISSION_DENIED"))
                        {
                            /*{'error': {'code': 403,
                                      'details': [{'@type': 'type.googleapis.com/google.rpc.Help',
                                        'links': [{'description': 'Google developers console billing',
                                          'url': 'https://console.developers.google.com/billing/enable?project=sapient-zodiac-183008'}]}],
                                      'message': 'This API method requires billing to be enabled. Please enable billing on project sapient-zodiac-183008 by visiting https://console.developers.google.com/billing/enable?project=sapient-zodiac-183008 then retry. If you enabled billing for this project recently, wait a few minutes for the action to propagate to our systems and retry.',
                                      'status': 'PERMISSION_DENIED'}}*/
                            log.LogError("Failed to process google stt response. Making api key expired: {apiKey}", apiKey);
                            collection.UpdateMany(mask, new BsonDocument { { "$set", new BsonDocument { { "status", "expired" } } } });
                        }
                        else
                        {
                            log.LogError("Failed to process google stt response. Making api key status error: {apiKey}", apiKey);
                            collection.UpdateMany(mask, new BsonDocument { { "$set", new BsonDocument { { "status", "error" } } } });
                        }
                        continue;
                    }
                    else
                    {
                        return jsStr;
                    }
                }
            }

            public string Recognize(string fn, string apiKey, int languageId, bool enableWordTimeOffsets = true)
            {

                var buffer = File.ReadAllBytes(fn);
                var res = Convert.ToBase64String(buffer);
                
                httpClient.DefaultRequestHeaders.Accept.Clear();

                /*test example:
                    body = {
                    "config": {
                        "encoding":"FLAC",
                        "sampleRateHertz": 16000,
                        "languageCode": "en-US",
                        "enableWordTimeOffsets": False
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
                        enableWordTimeOffsets = enableWordTimeOffsets
                    },
                    audio = new
                    {
                        content = res
                    }
                };

                var response = httpClient.PostAsync("https://speech.googleapis.com/v1/speech:recognize?key=" + apiKey, new StringContent(JsonConvert.SerializeObject(request).ToString(), Encoding.UTF8, "application/json")).Result;
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
                    default:
                        return "en-US";
                }

            }
        }
    }
}