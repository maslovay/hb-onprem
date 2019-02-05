using HBLib.AzureFunctions;
using HBLib.Extensions;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceBus.Messaging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HB_Operations_Cons_CSharp.AppFunctions
{
    public static class Audio_Blob_STTGoogleLongrunningCheckStatus
    {

        private static HttpClient httpClient = new HttpClient();

        [FunctionName("Audio_Blob_STTGoogleLongrunningCheckStatus")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer,
            //TraceWriter ilog,
            ExecutionContext dir,
            ILogger log)
        {

            try
            {

                // make request to get the documents
                var collection = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionAudioSTTGoogle"));
                //var mask = new BsonDocument { { "Status", "InProgress" } };
                var docs = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionAudioSTTGoogle")).Find(p => p["Status"] == "InProgress").ToList();

                var binPath = Misc.BinPath(dir);

                //log.Info($"{docs.Count} file(s) recognition in process");

                //read the recognition results
                foreach (var doc in docs)
                {
                    try
                    {
                        //log.Info("Working with file recognition " + doc["BlobGoogleDriveName"]);

                        var words = new BsonArray();
                        var reqResults = await GoogleConnectorStatic.GetGoogleSTTResults(doc["GoogleTransactionId"].ToString(), doc["GoogleApiKey"].ToString());

                        dynamic reqResultsDynamic = JsonConvert.DeserializeObject(reqResults);
                        //check if file is recognized
                        //log.Info("{reqResultsDynamic}", reqResultsDynamic);

                        if (reqResultsDynamic["error"] != null)
                        {
                            //eroor with recognition results
                            //log.Error("Error recornition {}", reqResults);
                            doc["Status"] = "Error";
                            try
                            {
                                HeedbookMessengerStatic.MongoDBMessenger.SafeReplaceOne(collection,
                                             new BsonDocument {
                                                 { "ApplicationUserId", doc["ApplicationUserId"]},
                                                 { "DialogueId", doc["DialogueId"]} ,
                                                 { "BlobGoogleDriveName", doc["BlobGoogleDriveName"] },
                                             },
                                             doc);

                                //log.Info("Added document to mongodb database: {doc}", doc.ToJson());

                                try
                                {
                                    //delete the file inside the google storage
                                    await GoogleConnectorStatic.DeleteFileGoogleCloud(doc["BlobGoogleDriveName"].ToString(), token);
                                }
                                catch (Exception e)
                                {
                                    log.Error($"Exeption deleting the file {e}");
                                }

                            }
                            catch (Exception e)
                            {
                                log.Error($"Mongo connection error occured {e}");
                            }

                        }

                        if (((JObject)reqResultsDynamic).Count == 0 || reqResultsDynamic["done"] == null)
                        {
                            //log.Info($"Empty result recognition or recognition is in process");
                        }
                        else
                        {
                            try
                            {
                                foreach (var result in reqResultsDynamic.response.results)
                                {
                                    //log.Info($"{reqResultsDynamic.response.results} result STT blocks");
                                    foreach (var wordJs in result.alternatives[0].words)
                                    {

                                        string begTimeStr = (wordJs.startTime).ToString();
                                        string endTimeStr = (wordJs.endTime).ToString();
                                        string word = wordJs.word;
                                        var begTime = DateTime.Parse(doc["Time"].ToString()).AddSeconds(Convert.ToDouble(begTimeStr.Remove(begTimeStr.Length - 1)));
                                        var endTime = DateTime.Parse(doc["Time"].ToString()).AddSeconds(Convert.ToDouble(endTimeStr.Remove(endTimeStr.Length - 1)));
                                        words.Add(new BsonDocument { { "BegTime", begTime } ,
                                                     { "EndTime", endTime},
                                                     { "Word", word} });
                                    }
                                }

                                //set words and status to STT object
                                var wordPack = new BsonDocument { { "Words", words } };
                                doc.Add("Value", wordPack);

                                doc["Status"] = "Finished";
                            }
                            catch
                            {
                                //log.Info("Empty recognition results");
                                //set words and status to STT object
                                var wordPack = new BsonDocument { { "Words", words } };
                                doc.Add("Value", wordPack);
                                doc["Status"] = "Finished";
                            }

                            try
                            {
                                HeedbookMessengerStatic.MongoDBMessenger.SafeReplaceOne(collection,
                                             new BsonDocument {
                                                 { "ApplicationUserId", doc["ApplicationUserId"]},
                                                 { "DialogueId", doc["DialogueId"]} ,
                                                 { "BlobGoogleDriveName", doc["BlobGoogleDriveName"] }
                                             },
                                             doc);

                                //log.Info("Added document to mongodb database: {doc}", doc.ToJson());

                                //send message for dialogue filling
                                var publishJs = new Dictionary<string, string>();
                                publishJs["DialogueId"] = doc["DialogueId"].ToString();
                                publishJs["BlobContainerName"] = doc["BlobContainerName"].ToString();
                                //var publishJs = new Dictionary<string, string> { { "DialogueId", doc["DialogueId"].ToString(), "BlobContainerName",  }, };
                                HeedbookMessengerStatic.ServiceBusMessenger.Publish(EnvVar.Get("TopicAudioSTT"), publishJs.JsonPrint());

                                try
                                {
                                    //delete the file inside the google storage
                                    await GoogleConnectorStatic.DeleteFileGoogleCloud(doc["BlobGoogleDriveName"].ToString(), token);
                                }
                                catch (Exception e)
                                {
                                    //log.Error("Exeption deleting the file {e}", e);
                                }


                            }
                            catch (Exception e)
                            {
                                log.Info($"Mongo connection error occured {e}");
                            }
                        }
                        // session.Dispose();
                    }
                    catch (Exception e)
                    {
                        log.Fatal($"Exception 1 occured {e}");
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                log.Fatal($"Exception occured {e}");
                throw;
            }

        }
    }
}