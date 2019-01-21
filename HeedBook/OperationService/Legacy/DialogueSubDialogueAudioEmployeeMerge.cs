using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HBLib.AzureFunctions;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace OperationService.Legacy
{
    public static class DialogueSubDialogueAudioEmployeeMerge
    {
        [FunctionName("Dialogue_Sub_DialogueAudioEmployeeMerge")]
        public static async Task Run(
            String msg, ExecutionContext dir, ILogger log)
        {
            var sessionId = Misc.GenSessionId();

            //DIALOGUE VIDEO FILE MERGE 
            /*{
              "_id": "5a19bc8f8f041747b8b39ba3",
              "DialogueId": "47e1d456-7e4b-42ff-beb0-089e146c000c",
              "BegTime": "20171127120005",
              "EndTime": "20171127120025"
            }*/
            if (msg == "Don't sleep!") return;

            dynamic msgJs = JsonConvert.DeserializeObject(msg);
            DateTime begTime = DT.Parse(msgJs["BegTime"].ToString());
            DateTime endTime = DT.Parse(msgJs["EndTime"].ToString());
            String dialogueId = msgJs["DialogueId"];
            String applicationUserId = msgJs["ApplicationUserId"];
            Int32 langId = Convert.ToInt32(msgJs["LanguageId"].ToString());

            try
            {
                var ffmpeg = new FFMpegWrapper(Path.Combine(Misc.BinPath(dir), "ffmpeg.exe"));

                // get information about dialogue ids
                var collectionAudiosEmp =
                    HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionBlobAudiosEmp"));

                begTime = DateTime.SpecifyKind(begTime, DateTimeKind.Utc);
                endTime = DateTime.SpecifyKind(endTime, DateTimeKind.Utc);


                // intersection: a, b, c, d = begTime, endTime, "BegTime", "EndTime". Intersection condition: d >= a, b >= c <=> "EndTime" >= begTime, "BegTime" <= endTime
                var query1 = new BsonDocument
                {
                    {
                        "$and", new BsonArray
                        {
                            new BsonDocument {{"EndTime", new BsonDocument {{"$gte", begTime}}}},
                            new BsonDocument {{"BegTime", new BsonDocument {{"$lte", endTime}}}}
                        }
                    }
                };

                var query2 = new BsonDocument {{"FileExist", true}, {"ApplicationUserId", applicationUserId}};

                var query = new BsonDocument {{"$and", new BsonArray {query1, query2}}};

                // {'_id': ObjectId('5a1c383cab98f54f6c98ca67'), 'BegTime': datetime.datetime(2017, 11, 27, 12, 0), 'EndTime': datetime.datetime(2017, 11, 27, 12, 0, 15), 'BlobContainer': 'videos', 'BlobName': 'a.webm', 'FileExist': True}
                var inputBlobDocs = collectionAudiosEmp.Find(query).ToList();

                inputBlobDocs = inputBlobDocs.OrderBy(p => p["CreationTime"]).ToList();

                if (inputBlobDocs.Count != 0)
                {
                    foreach (var blob in inputBlobDocs)
                    {
                        //log.Info("{BlobName}", blob["BlobContainer"]);
                    }

                    // collect audios that properly cover the dialogue
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

                    // download blobs
                    var blobFns = new List<String>();
                    foreach (var doc in inputBlobDocs)
                    {
                        var sas = HeedbookMessengerStatic.BlobStorageMessenger.GetBlobSASUrl(
                            doc["BlobContainer"].ToString(),
                            doc["BlobName"].ToString());
                        var uri = new Uri(sas);

                        var blobFn = Path.Combine(localDir, uri.Segments[2]);
                        using (var wclient = new WebClient())
                        {
                            wclient.DownloadFile(sas, blobFn);
                        }

                        blobFns.Add(blobFn);
                    }

                    var blobSTime = inputBlobDocs[0]["BegTime"].ToUniversalTime();

                    var extensions = inputBlobDocs.Select(doc => Path.GetExtension(doc["BlobName"].ToString()))
                                                  .ToList();
                    var ext = extensions[0];
                    var basename = $"{dialogueId}{ext}";
                    var tmpBasename = $"_tmp_{dialogueId}{ext}";
                    var outputFn = Path.Combine(localDir, basename);
                    var outputTmpFn = Path.Combine(localDir, tmpBasename);

                    //log.Info("Creating file {outputFn}", outputFn);

                    // concatenation
                    String output;
                    if (extensions.Any(p => p != ext))
                        output = ffmpeg.ConcatDifferentCodecs(blobFns, outputTmpFn);
                    else
                        output = ffmpeg.ConcatSameCodecs(blobFns, outputTmpFn, localDir);

                    // cutting
                    String output2;
                    output2 = ffmpeg.CutBlob(outputTmpFn, outputFn, (begTime - blobSTime).ToString(@"hh\:mm\:ss\.ff"),
                        (endTime - begTime).ToString(@"hh\:mm\:ss\.ff"));

                    // upload blob

                    var metadata = new Dictionary<String, String>
                    {
                        {"ApplicationUserId", applicationUserId},
                        {"LanguageId", langId.ToString()},
                        {"BegTime", DT.Format(begTime)},
                        {"EndTime", DT.Format(endTime)}
                    };
                    var employeeAudiosContainer = EnvVar.Get("BlobContainerDialogueAudiosEmp");
                    HeedbookMessengerStatic.BlobStorageMessenger.SendBlob(employeeAudiosContainer,
                        Path.GetFileName(outputFn), outputFn);
                    //HeedbookMessengerStatic.BlobStorageMessenger.SendBlob(employeeAudiosContainer, Path.GetFileName(outputFn), outputFn, metadata);

                    var name = dialogueId + ".mkv";
                    var blob1 = HeedbookMessengerStatic.BlobStorageMessenger.GetBlob(employeeAudiosContainer, name);

                    basename = name.Split('.')[0];
                    var localVideoFn = Path.Combine(localDir, name);
                    var localAudioFn = Path.Combine(localDir, basename + ".wav");

                    try
                    {
                        using (var output1 = new FileStream(localVideoFn, FileMode.Create))
                        {
                            await blob1.DownloadToStreamAsync(output1);
                        }

                        ffmpeg.VideoToWav(localVideoFn, localAudioFn);

                        using (var stream = File.Open(localAudioFn, FileMode.Open))
                        {
                            var blobContainerDialogueAudios = EnvVar.Get("BlobContainerDialogueAudios");
                            HeedbookMessengerStatic.BlobStorageMessenger.SendBlob(employeeAudiosContainer,
                                basename + ".wav", stream, metadata,
                                $"blob-{blobContainerDialogueAudios}");
                            //log.Info($"Audio file uploaded: {basename}.wav");
                        }


                        HeedbookMessengerStatic.BlobStorageMessenger.DeleteBlob(employeeAudiosContainer, name);
                        OS.SafeDelete(localVideoFn);
                        OS.SafeDelete(localAudioFn);
                        OS.SafeDelete(localDir);

                        // remove all local files
                        OS.SafeDelete(localDir);
                    }
                    catch (Exception e)
                    {
                        OS.SafeDelete(localVideoFn);
                        OS.SafeDelete(localAudioFn);
                        OS.SafeDelete(localDir);
                        OS.SafeDelete(localDir);
                        throw;
                    }

                    log.LogInformation($"Function finished: {dir.FunctionName}");
                }

                //else
                //{
                //log.Info("Employee audios is empty");
                //log.Info($"Function finished: {dir.FunctionName}");
                //}
            }
            catch (Exception e)
            {
                log.LogError("Exception occured {e}", e);
                throw;
            }
        }

        public static List<BsonDocument> CollectBlobs(List<BsonDocument> blobDocs, String dialogueId, DateTime begTime,
            DateTime endTime
            , ILogger log
        )
        {
            var begTimeDouble = Misc.ConvertToUnixTimestamp(begTime);
            var endTimeDouble = Misc.ConvertToUnixTimestamp(endTime);

            var resIC = new IntervalCollection();
            var dialogue = new Interval(begTimeDouble, endTimeDouble);
            var dialogueIC = new IntervalCollection(dialogue);

            if (dialogueIC.Length() == 0) throw new Exception("DialogueIC has zero length");

            // make document paired with corresponding interval
            var blobsAndIntervals = blobDocs.Select(doc => new Tuple<BsonDocument, Interval>(doc,
                new Interval(Misc.ConvertToUnixTimestamp((DateTime) doc["BegTime"]),
                    Misc.ConvertToUnixTimestamp((DateTime) doc["EndTime"])))).ToList();

            // reverse
            blobsAndIntervals = blobsAndIntervals.OrderBy(p => Convert.ToDateTime(p.Item1["CreationTime"])).Reverse()
                                                 .ToList();

            //log.Info("{blobDocs}", blobDocs.Select(p => p.ToJson()).ToList().JsonPrint());

            var res = new List<BsonDocument>();
            foreach (var blobAndInterval in blobsAndIntervals)
            {
                // unpack
                var doc = blobAndInterval.Item1;
                var blob = blobAndInterval.Item2;

                var blobIC = new IntervalCollection(blob);

                //log.Info("Processing blob {blobIC} {doc}", blobIC.JsonPrint(), doc.ToJson());

                if ((blobIC & resIC).Length() > 0) continue;

                if ((blobIC & dialogueIC).Length() == 0) continue;

                resIC = resIC | blobIC;
                res.Add(doc);

                //log.Info("resIC after doc {resIC}", resIC.JsonPrint());


                if ((dialogueIC - resIC).Length() == 0)
                {
                    // dialogue is filled!
                    log.LogInformation("Filled! {dialogueIC}, {resIC}", dialogueIC.JsonPrint(), resIC.JsonPrint());
                    break;
                }
            }

            if (resIC.isEmpty())
            {
                //var analyzedDialogue = HeedbookMessengerStatic.context.Dialogues.First(p => p.DialogueId.ToString() == dialogueId);
                //analyzedDialogue.Comment = "Employee audio is empty";
                //HeedbookMessengerStatic.context.SaveChanges();

                //log.Error("Employee audio is empty and hence cannot be created! {dialogueIC} {resIC}", dialogueIC.JsonPrint(), resIC.JsonPrint());
                log.LogError("Employee audio is empty and hence cannot be created!");
                throw new Exception("Employee audio is empty and hence cannot be created!");
            }

            if (!resIC.isMonolith())
            {
                //var analyzedDialogue = HeedbookMessengerStatic.context.Dialogues.First(p => p.DialogueId.ToString() == dialogueId);
                //analyzedDialogue.Comment = "Employee audio is not a monolith";
                //HeedbookMessengerStatic.context.SaveChanges();

                //log.Error("Employee audio is not a monolith and hence cannot be created! {dialogueIC} {resIC}", dialogueIC.JsonPrint(), resIC.JsonPrint());
                log.LogError("Employee audio is not a monolith and hence cannot be created!");
                throw new Exception("Employee audio is not a monolith and hence cannot be created!");
            }

            var error1 = (dialogueIC - resIC).Length() / dialogueIC.Length();
            if (error1 >= 0.2)
            {
                var analyzedDialogue = HeedbookMessengerStatic
                                      .Context().Dialogues.First(p => p.DialogueId.ToString() == dialogueId);
                analyzedDialogue.Comment = "Too much of the dialogue is unfilled";
                HeedbookMessengerStatic.Context().SaveChanges();

                //log.Error("Too much of the dialogue is unfilled {dialogueIC} {resIC}", dialogueIC.JsonPrint(), resIC.JsonPrint());
                log.LogError("Too much of the dialogue is unfilled");
                throw new Exception("Too much of the dialogue is unfilled");
            }

            //todo: make properly
            //double error2 = (resIC - dialogueIC).Length();
            //if (error2 >= 0.3) {
            //    throw new Exception("Too much of the non-dialogue audio is filled");
            //}

            //todo: save info about the merge in MongoDB/sql

            return res;
        }
    }
}