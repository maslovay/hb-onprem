//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net;
//using System.Net.Http;
//using System.Text;
//using HBData;
//using HBLib.Utils;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Azure.WebJobs.Host;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using MongoDB.Bson;
//using MongoDB.Bson.Serialization;
//using MongoDB.Driver;
//
//namespace OperationService.Legacy
//{
//    public static class TestServiceBus
//    {
//        public static DateTime UnixEpoch { get; private set; }
//
//        [FunctionName("Test_ServiceBus")]
//        public static HttpResponseMessage Run(
//            HttpRequestMessage req,
//            ILogger log)
//        {
//            {
//                var time = DateTime.Now.AddDays(-60);
//                var context = HeedbookMessengerStatic.Context();
//                var dialogueIds = context.Dialogues
//                                         .Include(p => p.DialogueSpeech)
//                                         .Where(p => p.StatusId == 3 && p.BegTime > time &&
//                                                     p.DialogueSpeech.FirstOrDefault().PositiveShare == null)
//                                         .Select(p => p.DialogueId.ToString()).Distinct().ToList();
//                log.LogInformation($"{dialogueIds.Count()}");
//                //var dialoguesSpeechs = context.DialogueSpeechs.Where(p => dialogueIds.Contains(p.DialogueId.ToString())).ToList();
//                //log2.Info($"{dialoguesSpeechs.Count()}");
//                foreach (var dialogueId in dialogueIds)
//                {
//                    log.LogInformation($"{dialogueId}");
//                    var collection =
//                        HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(
//                            EnvVar.Get("CollectionAudioSTTGoogle"));
//                    var mask = new BsonDocument {{"DialogueId", dialogueId}, {"BlobContainerName", "dialogueaudios"}};
//                    var docs = collection.Find(mask).ToList();
//
//                    if (docs.Count == 0) continue;
//                    //log.Critical("No records found for audio.stt {dialogueId}", dialogueId);
//                    var doc = docs[0];
//                    var fullText = "";
//                    var length = BsonSerializer.Deserialize<BsonArray>(doc["Value"]["Words"].ToJson()).Count();
//
//                    for (int j = 0; j < length; j++)
//                    {
//                        var textData = doc["Value"]["Words"][j];
//                        fullText += textData["Word"].ToString() + " ";
//                    }
//
//                    var sentimentScore = PositiveShare(fullText);
//                    log.LogInformation($"Dialogueid {dialogueId} positive share {sentimentScore}");
//                    try
//                    {
//                        var dialogueSpeech =
//                            context.DialogueSpeeches.First(p => p.DialogueId.ToString() == dialogueId);
//                        dialogueSpeech.PositiveShare = sentimentScore;
//                        context.SaveChanges();
//                    }
//                    catch
//                    {
//                    }
//                }
//
//                HeedbookMessengerStatic.Context().SaveChanges();
//                return new HttpResponseMessage(HttpStatusCode.OK)
//                {
//                    Content = new StringContent("OK", Encoding.UTF8, "application/json")
//                };
//            }
//        }
//
//        public static double? PositiveShare(string text)
//        {
//            // Create a client.
//
//            ITextAnalyticsAPI client = new TextAnalyticsAPI();
//            client.AzureRegion = AzureRegions.Westeurope;
//            client.SubscriptionKey = EnvVar.Get("TextAnalytics1");
//            string textLanguage;
//            try
//            {
//                LanguageBatchResult language = client.DetectLanguage(
//                    new BatchInput(
//                        new List<Input>()
//                        {
//                            new Input("1", text)
//                        }));
//                textLanguage = language.Documents[0].DetectedLanguages[0].Iso6391Name;
//                var result = client.Sentiment(
//                    new MultiLanguageBatchInput(
//                        new List<MultiLanguageInput>()
//                        {
//                            new MultiLanguageInput(textLanguage, "0", text),
//                        }));
//
//                return result.Documents[0].Score;
//            }
//            catch
//            {
//                return null;
//            }
//        }
//    }
//}