using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HBLib.AzureFunctions;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace OperationService.Legacy {
    public static class DialogueSubDialogueVideoMerge
    {
        [FunctionName("Dialogue_Sub_DialogueVideoMerge")]
        public static async Task Run(string msg,
            ExecutionContext dir,
            ILogger log)
        {
            var sessionId = Misc.GenSessionId();

            //DIALOGUE VIDEO FILE MERGE 
            /*{
              "_id": "5a19bc8f8f041747b8b39ba3",
              "DialogueId": "47e1d456-7e4b-42ff-beb0-089e146c000c",
              "BegTime": "20171127120005",
              "EndTime": "20171127120025"
            }*/

            dynamic msgJs = JsonConvert.DeserializeObject(msg);
            DateTime begTime = DT.Parse(msgJs["BegTime"].ToString());
            DateTime endTime = DT.Parse(msgJs["EndTime"].ToString());
            string dialogueId = msgJs["DialogueId"];
            string applicationUserId = msgJs["ApplicationUserId"];
            int langId = Convert.ToInt32(msgJs["LanguageId"].ToString());

            try
            {
                var ffmpeg = new FFMpegWrapper(Path.Combine(Misc.BinPath(dir), "ffmpeg.exe"));

                // get information about dialogue ids
                var collectionBlobVideos = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionBlobVideos"));

                begTime = DateTime.SpecifyKind(begTime, DateTimeKind.Utc);
                endTime = DateTime.SpecifyKind(endTime, DateTimeKind.Utc);

                // intersection: a, b, c, d = begTime, endTime, "BegTime", "EndTime". Intersection condition: d >= a, b >= c <=> "EndTime" >= begTime, "BegTime" <= endTime
                var query1 = new BsonDocument { { "$and", new BsonArray {
                                                    new BsonDocument { {"EndTime", new BsonDocument { {"$gte", begTime} } } },
                                                    new BsonDocument { {"BegTime", new BsonDocument { {"$lte", endTime} } } }
                                              } } };

                var query2 = new BsonDocument { { "FileExist", true }, { "ApplicationUserId", applicationUserId } };
                var query = new BsonDocument { { "$and", new BsonArray { query1, query2 } } };

                // {'_id': ObjectId('5a1c383cab98f54f6c98ca67'), 'BegTime': datetime.datetime(2017, 11, 27, 12, 0), 'EndTime': datetime.datetime(2017, 11, 27, 12, 0, 15), 'BlobContainer': 'videos', 'BlobName': 'a.webm', 'FileExist': True}
                var inputBlobDocs = new List<BsonDocument>();
                foreach (var doc in collectionBlobVideos.Find(query).ToEnumerable())
                {
                    inputBlobDocs.Add(doc);
                }

                inputBlobDocs = inputBlobDocs.OrderBy(p => p["CreationTime"]).ToList();

                // collect videos that properly cover the dialogue
                try
                {

                    inputBlobDocs = CollectBlobs(inputBlobDocs, dialogueId, begTime, endTime, log);
                }
                catch (Exception e)
                {
                    log.LogError("Failed to collect the dialog {e}", e);
                    throw;
                }

                inputBlobDocs = inputBlobDocs.OrderBy(p => p["BegTime"]).ToList();
                var localDir = Misc.GenLocalDir(sessionId);

                try
                {

                    // download blobs
                    var blobFns = new List<string>();
                    foreach (var doc in inputBlobDocs)
                    {
                        var sas = HeedbookMessengerStatic.BlobStorageMessenger.GetBlobSASUrl(doc["BlobContainer"].ToString(), doc["BlobName"].ToString());
                        var uri = new Uri(sas);

                        var blobFn = Path.Combine(localDir, uri.Segments[2]);
                        using (var wclient = new WebClient())
                        {
                            wclient.DownloadFile(sas, blobFn);
                        }
                        blobFns.Add(blobFn);
                    }

                    var blobSTime = inputBlobDocs[0]["BegTime"].ToUniversalTime();

                    var extensions = inputBlobDocs.Select(doc => Path.GetExtension(doc["BlobName"].ToString())).ToList();
                    var ext = extensions[0];
                    var basename = $"{dialogueId}{ext}";
                    var tmpBasename = $"_tmp_{dialogueId}{ext}";
                    var outputFn = Path.Combine(localDir, basename);
                    var outputTmpFn = Path.Combine(localDir, tmpBasename);

                    // concatenation
                    string output;
                    if (extensions.Any(p => p != ext))
                    {
                        output = ffmpeg.ConcatDifferentCodecs(blobFns, outputTmpFn);
                    }
                    else
                    {
                        output = ffmpeg.ConcatSameCodecs(blobFns, outputTmpFn, localDir);
                    }

                    // cutting
                    string output2;
                    output2 = ffmpeg.CutBlob(outputTmpFn, outputFn, (begTime - blobSTime).ToString(@"hh\:mm\:ss\.ff"), (endTime - begTime).ToString(@"hh\:mm\:ss\.ff"));

                    // upload blob
                    var metadata = new Dictionary<string, string> { { "ApplicationUserId", applicationUserId},
                                                            { "LanguageId", langId.ToString() },
                                                            { "BegTime", DT.Format(begTime)},
                                                            { "EndTime", DT.Format(endTime)} };
                    var dialogueVideosContainer = EnvVar.Get("BlobContainerDialogueVideos");
                    HeedbookMessengerStatic.BlobStorageMessenger.SendBlob(dialogueVideosContainer, Path.GetFileName(outputFn), outputFn, metadata, topicName: $"blob-{dialogueVideosContainer}");

                    // remove all local files
                    OS.SafeDelete(localDir);
                    log.LogInformation($"Function finished: {dir.FunctionName}");
                }
                catch (Exception e)
                {
                    log.LogError($"Exception occured while executing Dialogue_Sub_DialogueVideoMerge {e}");
                    OS.SafeDelete(localDir);
                    throw e;
                }
            }
            catch (Exception e)
            {
                log.LogError("Exception occured {e}", e);
                throw e;
            }
        }

        public static List<BsonDocument> CollectBlobs(List<BsonDocument> blobDocs, string dialogueId, DateTime begTime, DateTime endTime
            , ILogger log
            )
        {
            var begTimeDouble = Misc.ConvertToUnixTimestamp(begTime);
            var endTimeDouble = Misc.ConvertToUnixTimestamp(endTime);

            var resIC = new IntervalCollection();
            var dialogue = new Interval(begTimeDouble, endTimeDouble);
            var dialogueIC = new IntervalCollection(dialogue);

            if (dialogueIC.Length() == 0)
            {
                //log.Fatal("DialogueIC has zero length {dialogueIC}", dialogueIC.JsonPrint());
                throw new Exception("DialogueIC has zero length");
            }

            // make document paired with corresponding interval
            var blobsAndIntervals = blobDocs.Select(doc => new Tuple<BsonDocument, Interval>(doc, new Interval(Misc.ConvertToUnixTimestamp((DateTime)doc["BegTime"]),
                                                                                                               Misc.ConvertToUnixTimestamp((DateTime)doc["EndTime"])))).ToList();

            // reverse
            blobsAndIntervals = blobsAndIntervals.OrderBy(p => Convert.ToDateTime(p.Item1["CreationTime"])).Reverse().ToList();

            var res = new List<BsonDocument>();
            foreach (var blobAndInterval in blobsAndIntervals)
            {
                // unpack
                var doc = blobAndInterval.Item1;
                var blob = blobAndInterval.Item2;

                var blobIC = new IntervalCollection(blob);

                //log.Info("Processing blob {blobIC} {doc}", blobIC.JsonPrint(), doc.ToJson());

                if ((blobIC & resIC).Length() > 0)
                {
                    //log.Info("Interception too big {blobIC} {resIC}", blobIC.JsonPrint(), resIC.JsonPrint());
                    continue;
                    //throw new Exception("Interception too big");
                }

                if ((blobIC & dialogueIC).Length() == 0)
                {
                    //log.Info("Interception is zero {blobIC} {resIC}", blobIC.JsonPrint(), resIC.JsonPrint());
                    continue;
                }

                resIC = resIC | blobIC;
                res.Add(doc);

                //log.Info("resIC after doc {resIC}", resIC.JsonPrint());


                if ((dialogueIC - resIC).Length() == 0)
                {
                    //dialogue is filled!
                    //log.Info("Filled! {dialogueIC}, {resIC}", dialogueIC.JsonPrint(), resIC.JsonPrint());
                    break;
                }
            }

            if (resIC.isEmpty())
            {
                var analyzedDialogue = HeedbookMessengerStatic.Context().Dialogues.First(p => p.DialogueId.ToString() == dialogueId);
                analyzedDialogue.Comment = "Dialogue is empty";
                HeedbookMessengerStatic.Context().SaveChanges();

                log.LogError("Dialogue is empty and hence cannot be created! {dialogueIC} {resIC}", dialogueIC.JsonPrint(), resIC.JsonPrint());
                throw new Exception("Dialogue is empty and hence cannot be created!");
            }

            if (!resIC.isMonolith())
            {
                var analyzedDialogue = HeedbookMessengerStatic.Context().Dialogues.First(p => p.DialogueId.ToString() == dialogueId);
                analyzedDialogue.Comment = "Dialogue is not a monolith";
                HeedbookMessengerStatic.Context().SaveChanges();

                log.LogError("Dialogue is not a monolith and hence cannot be created! {dialogueIC} {resIC}", dialogueIC.JsonPrint(), resIC.JsonPrint());
                throw new Exception("Dialogue is not a monolith and hence cannot be created!");
            }

            double error1 = (dialogueIC - resIC).Length() / dialogueIC.Length();
            if (error1 >= 0.2)
            {
                var analyzedDialogue = HeedbookMessengerStatic.Context().Dialogues.First(p => p.DialogueId.ToString() == dialogueId);
                analyzedDialogue.Comment = "Too much of the dialogue is unfilled";
                HeedbookMessengerStatic.Context().SaveChanges();

                //log.Error("Too much of the dialogue is unfilled {dialogueIC} {resIC}", dialogueIC.JsonPrint(), resIC.JsonPrint());
                //HeedbookMessengerStatic.SlackMessenger.Post($"Too much of the dialogue {dialogueId}, {error1} is unfilled", "#1555e0");
                throw new Exception("Too much of the dialogue is unfilled");
            }

            //todo: make properly
            //double error2 = (resIC - dialogueIC).Length();
            //if (error2 >= 0.3) {
            //    throw new Exception("Too much of the non-dialogue video is filled");
            //}

            //todo: save info about the merge in MongoDB/sql

            return res;
        }
    }
}
