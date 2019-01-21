using System;
using System.Collections.Generic;
using System.Linq;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.ProjectOxford.Face;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace OperationService.Legacy
{
    public static class DialogueSubAutoCreation
    {
        [FunctionName("Dialogue_Sub_AutoCreation")]
        public static async System.Threading.Tasks.Task RunAsync(
            string msg,
            ExecutionContext dir,
            ILogger log)
        {
            try
            {
                string applicationUserId;
                // read message from service bus
                dynamic msgJs = JsonConvert.DeserializeObject(msg);
                try
                {
                    applicationUserId = msgJs["ApplicationUserId"];
                }
                catch (Exception e)
                {
                    //log.Error("Failed to read message {e}", e);
                    throw;
                }

                // get collection from MongoDB
                //var collectionFrame = new IMongoCollection<BsonDocument>();
                //var collectionAlgo = new IMongoCollection<BsonDocument>();
                IMongoCollection<BsonDocument> collectionFrame, collectionAlgo;
                try
                {
                    collectionFrame = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("f"));
                    collectionAlgo = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionMarkupAlgorithm"));

                }
                catch (Exception e)
                {
                    //log.Info("Exception occured with connection {}", e);
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(10));
                    collectionFrame = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionFrameFacedetection"));
                    collectionAlgo = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionMarkupAlgorithm"));

                }
                // create mask
                var maskAlgo = new BsonDocument { { "ApplicationUserId", applicationUserId } };
                var maskAlgoInProgress = new BsonDocument { { "ApplicationUserId", applicationUserId }, { "Status", "InProgress" } };
                var sortAlgo = Builders<BsonDocument>.Sort.Ascending("BegTime");
                var docsAlgo = collectionAlgo.Find(maskAlgo).Sort(sortAlgo).ToList();
                var docsAlgoInProgress = collectionAlgo.Find(maskAlgoInProgress).Sort(sortAlgo).ToList();
                // check errors in collections
                if (docsAlgoInProgress.Count > 1)
                {
                    //log.Error($"Function finished, algorithm doesn't work correct: {dir.FunctionName}");
                    return;
                }
                // generate mask for frames
                var maskFrame = new BsonDocument();
                var curTime = DateTime.UtcNow.AddHours(-1);

                if (docsAlgo.Count == 0)
                {
                    maskFrame = new BsonDocument { { "ApplicationUserId", applicationUserId},
                    {"IsFacePresent", true },
                    {"Time", new BsonDocument{ {"$lte", curTime} } } };
                }
                else
                {
                    var endTime = Convert.ToDateTime(docsAlgo[docsAlgo.Count - 1]["End"]);
                    maskFrame = new BsonDocument { {"ApplicationUserId", applicationUserId },
                    {"IsFacePresent", true },
                    {"Time", new BsonDocument{ {"$lte", curTime}, {"$gt", endTime} } } };
                }
                // get frames not proceeded, but with time at least 1 hour to NowTime
                var docs = collectionFrame.Find(maskFrame).ToList();

                if (docs.Count == 0)
                {
                    //log.Info($"Function finished, no new frames detected: {dir.FunctionName}");
                    return;
                }

                //log.Info("Start to glue dialogues");

                var autoDialoguesList = new List<AutoDialogue>();
                var dialogue = new AutoDialogue();

                //log.Info($"Frames count {docs.Count}");

                for (int i = 0; i < docs.Count; i++)
                {
                    // case when i = 0 - special, because of streem
                    if (i == 0)
                    {
                        // case when all algoDialogues Finished
                        if (docsAlgoInProgress.Count == 0)
                        {
                            dialogue.beg = Convert.ToDateTime(docs[i]["Time"]);
                            dialogue.begFrame = Convert.ToString(docs[i]["BlobName"]);
                        }
                        // case when not all algoDialogues finished
                        else
                        {
                            // if distance between dialogues is too big
                            if ((Convert.ToDateTime(docs[i]["Time"]) - Convert.ToDateTime(docsAlgoInProgress[0]["End"])).TotalSeconds > Convert.ToDouble(EnvVar.Get("PauseAlgoMinDuration")))
                            {
                                // create old dialogue
                                dialogue.beg = Convert.ToDateTime(docsAlgoInProgress[0]["Beg"]);
                                dialogue.end = Convert.ToDateTime(docsAlgoInProgress[0]["End"]);
                                dialogue.begFrame = Convert.ToString(docsAlgoInProgress[0]["BegFrame"]);
                                dialogue.endFrame = Convert.ToString(docsAlgoInProgress[0]["EndFrame"]);
                                autoDialoguesList.Add(dialogue);
                                // create new dialogue
                                dialogue = new AutoDialogue();
                                dialogue.beg = Convert.ToDateTime(docs[i]["Time"]);
                                dialogue.begFrame = Convert.ToString(docs[i]["BlobName"]);
                            }
                            // if distance between dialogues is small - concat two dialogues
                            else
                            {
                                dialogue.beg = Convert.ToDateTime(docsAlgoInProgress[0]["Beg"]);
                                dialogue.begFrame = Convert.ToString(docsAlgoInProgress[0]["BegFrame"]);
                                if (i == docs.Count - 1)
                                {
                                    dialogue.end = Convert.ToDateTime(docs[i]["Time"]);
                                    dialogue.endFrame = Convert.ToString(docs[i]["BlobName"]);
                                    autoDialoguesList.Add(dialogue);
                                }
                            }
                        }
                    }
                    // case when i > 0
                    else
                    {
                        // if distance between dialgues is too big - create old and new dialogues
                        if ((Convert.ToDateTime(docs[i]["Time"]) - Convert.ToDateTime(docs[i-1]["Time"])).TotalSeconds > Convert.ToDouble(EnvVar.Get("PauseAlgoMinDuration")))
                        {
                            dialogue.end = Convert.ToDateTime(docs[i - 1]["Time"]);
                            dialogue.endFrame = Convert.ToString(docs[i - 1]["BlobName"]);
                            if ((dialogue.end - dialogue.beg).TotalSeconds > Convert.ToDouble(EnvVar.Get("DialogueMinDuration")))
                            {
                                autoDialoguesList.Add(dialogue);
                            }
                            dialogue = new AutoDialogue();
                            dialogue.beg = Convert.ToDateTime(docs[i]["Time"]);
                            dialogue.begFrame = Convert.ToString(docs[i]["BlobName"]);
                        }
                        // if distance between dialogues is small  - continue, if i != docs.Count - 1, else - create dialogue
                        else
                        {
                            if (i == docs.Count - 1)
                            {
                                dialogue.end = Convert.ToDateTime(docs[i]["Time"]);
                                dialogue.endFrame = Convert.ToString(docs[i]["BlobName"]);
                                autoDialoguesList.Add(dialogue);
                            }
                        }
                    }
                }
                //log.Info("Auto Dialogues by time created {autoDialoguesList.Count}", autoDialoguesList.Count);

                if (autoDialoguesList.Count == 0)
                {
                    //log.Info($"Function finished: {dir.FunctionName}");
                    return;
                }

                if (autoDialoguesList.Count == 1)
                {
                    collectionAlgo.DeleteMany(maskAlgoInProgress);
                    Publish(collectionAlgo, applicationUserId, autoDialoguesList[0].beg, autoDialoguesList[0].end, autoDialoguesList[0].begFrame, autoDialoguesList[0].endFrame, "InProgress");
                    //log.Info($"Function finished: {dir.FunctionName}");
                    return;
                }

                //log.Info("Starting to compare algo time dialogues");
                collectionAlgo.DeleteMany(maskAlgoInProgress);
                var resultList = new List<AutoDialogue>();
                dialogue = new AutoDialogue();
                dialogue.beg = autoDialoguesList[0].beg;
                dialogue.begFrame = autoDialoguesList[0].begFrame;

                for (int i = 1; i < autoDialoguesList.Count; i++)
                {
                    // check by face compare
                    if (await FaceCompareAsync(autoDialoguesList[i].begFrame, autoDialoguesList[i - 1].endFrame))
                    {
                        if (i == autoDialoguesList.Count - 1)
                        {
                            dialogue.end = autoDialoguesList[i].end;
                            dialogue.endFrame = autoDialoguesList[i].endFrame;
                            Publish(collectionAlgo, applicationUserId, dialogue.beg, dialogue.end, dialogue.begFrame, dialogue.endFrame, "InProgress");
                        }
                    }
                    else
                    {
                        dialogue.end = autoDialoguesList[i - 1].end;
                        dialogue.endFrame = autoDialoguesList[i - 1].endFrame;
                        Publish(collectionAlgo, applicationUserId, dialogue.beg, dialogue.end, dialogue.begFrame, dialogue.endFrame, "Finished");
                        dialogue = new AutoDialogue();
                        dialogue.beg = autoDialoguesList[i].beg;
                        dialogue.begFrame = autoDialoguesList[i].begFrame;
                        if (i == autoDialoguesList.Count - 1)
                        {
                            dialogue.end = autoDialoguesList[i].end;
                            dialogue.endFrame = autoDialoguesList[i].endFrame;
                            Publish(collectionAlgo, applicationUserId, dialogue.beg, dialogue.end, dialogue.begFrame, dialogue.endFrame, "InProgress");
                        }
                    }
                }
                //log.Info($"Function finished: {dir.FunctionName}");
            }
            catch (Exception e)
            {
                //log.Critical("Exception occured {e}", e);
            }
        }

        public static async System.Threading.Tasks.Task<bool> FaceCompareAsync(string file1, string file2)
        {
            var url1 = HeedbookMessengerStatic.BlobStorageMessenger.GetBlobSASUrl(EnvVar.Get("BlobContainerFrames"), file1);
            var url2 = HeedbookMessengerStatic.BlobStorageMessenger.GetBlobSASUrl(EnvVar.Get("BlobContainerFrames"), file2);

            //connect to storage
            var faceClientStrings = EnvVar.GetAll().Where(kvp => kvp.Key.StartsWith("FaceServiceClient")).Select(kvp => kvp.Value).ToList();
            var faceClientIndex = DateTime.Now.Millisecond % faceClientStrings.Count;
            var cli = new FaceServiceClient(faceClientStrings[faceClientIndex], EnvVar.Get("FaceServiceApiRoot"));

            var Identify = await cli.DetectAsync(url1);
            if (Identify.Length == 0)
            {
                return false;
            }

            var mainFace = (from f in Identify
                            orderby f.FaceRectangle.Width
                            select f).FirstOrDefault();
            var mainFaceId = mainFace.FaceId;
            Identify = await cli.DetectAsync(url2);

            if (Identify.Length == 0)
            {
                return false;
            }

            mainFace = (from f in Identify
                        orderby f.FaceRectangle.Width
                        select f).FirstOrDefault();
            var ComparableFaceId = mainFace.FaceId;
            var VerifyRes = cli.VerifyAsync(mainFaceId, ComparableFaceId);
            return VerifyRes.Result.IsIdentical;
        }

        public class AutoDialogue
        {
            public DateTime beg { get; set; }
            public DateTime end { get; set; }
            public string begFrame { get; set; }
            public string endFrame { get; set; }
        }

        public static void Publish(IMongoCollection<BsonDocument> collectionAlgo, string applicationUserId, DateTime beg, DateTime end, string begFrame, string endFrame, string status)
        {
            var result = new BsonDocument { { "ApplicationUserId", applicationUserId },
                                    { "DialogueId", Guid.NewGuid() },
                                    { "Status", status },
                                    { "Beg", beg},
                                    { "End", end},
                                    { "BegFrame", begFrame},
                                    { "EndFrame", endFrame}};
            collectionAlgo.InsertOne(result);
        }

    }
}
