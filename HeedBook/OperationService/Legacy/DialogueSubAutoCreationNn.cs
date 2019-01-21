using System;
using System.Collections.Generic;
using System.Linq;
using HBLib.Models;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace OperationService.Legacy
{
    public static class DialogueSubAutoCreationNn
    {
        [FunctionName("Dialogue_Sub_AutoCreationNN")]
        public static void Run([TimerTrigger("0 */20 * * * *")]TimerInfo myTimer, ILogger log, ExecutionContext dir)
        {
            var messenger = new HeedbookMessenger();
            var collectionFramesInfo = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionFrameInformation"));
            var begTime = new DateTime(2018, 12, 3, 0, 0, 0);
            var endTime = DateTime.UtcNow.AddMinutes(-20);

            log.LogInformation($"Beg time ------ {begTime}, End time ------- {endTime}");
            var filter = new BsonDocument {
                { "StatusNN", "InProgress" },
                { "Time", new BsonDocument{ { "$gte", begTime} } },
                { "FaceId", new BsonDocument { { "$ne", "" } } }
            };

            var sort = Builders<BsonDocument>.Sort.Ascending("Time");
            var docsFramesInfo = collectionFramesInfo.Find(filter).Sort(sort).ToList();
            var docs = BsonSerializer.Deserialize<List<FramesInfoStructure>>(docsFramesInfo.ToJson());
            log.LogInformation($"All docs count ------- {docs.Count()}");

            // needful face id
            var faceIds = docs.GroupBy(p => p.FaceId)
                .Select(p => new { FaceId = p.Key, FaceIdCount = p.Count(), MaxTime = p.Max(q => q.Time) })
                .ToList()
                .Where(p => p.FaceIdCount >= 3 && p.MaxTime <= endTime).Select(p => p.FaceId);

            // all face id
            var allFaceIds = docs.Select(p => p.FaceId).ToList();

            // face to update
            var faceUpdate = allFaceIds.Except(
                docs.GroupBy(p => p.FaceId)
                    .Select(p => new { FaceId = p.Key, FaceIdCount = p.Count(), MaxTime = p.Max(q => q.Time) })
                    .ToList()
                    .Where(p => p.MaxTime > endTime).Select(p => p.FaceId));

            // select fields
            log.LogInformation($"All docs count ------------ {docs.Count()}");
            docs = docs.Where(p => faceIds.Contains(p.FaceId)).ToList();
            var resDocs = docs.GroupBy(p => p.FaceId)
                .Select(p => new {
                    FaceId = p.Key,
                    BegTime = p.Min(q => q.Time),
                    EndTime = p.Max(q => q.Time),
                    FramesCount = p.Count(),
                    ApplicationUserId = p.FirstOrDefault().ApplicationUserId }).ToList();

            foreach (var doc in resDocs)
            {
                log.LogInformation($"Work with face id ----------- {doc.FaceId}");
                var languageId = HeedbookMessengerStatic.Context().ApplicationUsers
                    .Include(p => p.Company)
                    .Include(p => p.Company.Language)
                    .FirstOrDefault(p => p.Id.ToString() == doc.ApplicationUserId).Company.Language.LanguageId;

                if (doc.FramesCount >= doc.EndTime.Subtract(doc.BegTime).TotalMinutes)
                {
                    var seconds = (doc.EndTime.Subtract(doc.BegTime).TotalSeconds > 60) ? 15 : 6;
                    // create dialogue in sql and send message to service bus
                    log.LogInformation($"{doc.ApplicationUserId}, {doc.BegTime.AddSeconds(-seconds)}, {doc.EndTime.AddSeconds(seconds)}, {doc.FaceId}");
                    SendMessage(doc.ApplicationUserId, languageId, doc.BegTime.AddSeconds(-seconds), doc.EndTime.AddSeconds(seconds), messenger);
                }
            }

            // mask 1 for update
            var mask1 = new BsonDocument {
                { "StatusNN", "InProgress" },
                { "Time", new BsonDocument{ { "$gte", begTime} } },
                { "FaceId", ""}
            };

            // mask 2 for update
            BsonArray bArray = new BsonArray();
            foreach (var term in faceUpdate)
            {
                bArray.Add(term);
            }

            var mask2 = new BsonDocument {
                { "StatusNN", "InProgress" },
                { "Time", new BsonDocument{ { "$gte", begTime} } },
                { "FaceId", new BsonDocument { { "$in", bArray} } }
            };

            var resultUpdate1 = Retry.Do(() => { return UpdateCollection(collectionFramesInfo, mask1, begTime, log); }, TimeSpan.FromSeconds(1), 5); 
            log.LogInformation($"Result of updating mask1 ----- {resultUpdate1}");
            var resultUpdate2 = Retry.Do(() => { return UpdateCollection(collectionFramesInfo, mask2, begTime, log); }, TimeSpan.FromSeconds(1), 5);
            log.LogInformation($"Result of updating mask2 ----- {resultUpdate2}");
            log.LogInformation("Function finished");

            log.LogInformation($"Function finished {dir.FunctionName}");
        }
        public static bool UpdateCollectionSteps(IMongoCollection<BsonDocument> collection, 
            BsonDocument mask, DateTime begTime, ILogger log, int step = 100)
        {
            try
            {
                var lenght = 1;
                var iteration = 0;
                while (lenght > 0)
                {
                    log.LogInformation($"Iteration ----- {iteration}, step -------- {step}");
                    iteration += 1;

                    var removeDocs = collection.Find(mask).Limit(step).ToList();
                    lenght = removeDocs.Count();
                    if (lenght > 0)
                    {
                        var removeFileNames = new BsonArray();
                        foreach (var removeDoc in removeDocs)
                        {
                            removeFileNames.Add(removeDoc["FileName"]);
                        }
                        var maskUpdate = new BsonDocument {
                        { "StatusNN", "InProgress" },
                        { "Time", new BsonDocument{ { "$gte", begTime} } },
                        { "FileName", new BsonDocument { { "$in", removeFileNames } } }
                    };

                        collection.UpdateMany(maskUpdate, Builders<BsonDocument>.Update.Set("StatusNN", "Finished"));
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool UpdateCollection(IMongoCollection<BsonDocument> collection,
            BsonDocument mask, DateTime begTime, ILogger log)
        {
            try
            {
                collection.UpdateMany(mask, Builders<BsonDocument>.Update.Set("StatusNN", "Finished"));
                return true;
            }
            catch
            {
                try
                {
                    int step = 128;
                    bool update = false;

                    while ( !update & step > 2)
                    {
                        update = UpdateCollectionSteps(collection, mask, begTime, log, step);
                        if (!update)
                        {
                            step = step / 2;
                        }
                    }
                    return update;
                }
                catch (Exception e)
                {
                    log.LogInformation($"Exception occured {e}");
                    throw;
                }
            }
        }


        // function for dialogue creation and sending information to service bus topic
        public static void SendMessage(string applicationUserId, int languageId, DateTime begTime, DateTime endTime, HeedbookMessenger messenger)
        {
            var guid = Guid.NewGuid();
            var sb = new MessageStructure();
            sb.DialogueId = guid;
            sb.ApplicationUserId = applicationUserId;
            sb.LanguageId = languageId;
            sb.BegTime = begTime.ToString("yyyyMMddHHmmss");
            sb.EndTime = endTime.ToString("yyyyMMddHHmmss");

            var emp = new Dialogue();
            emp.DialogueId = guid;
            emp.ApplicationUserId = applicationUserId;
            emp.BegTime = begTime;
            emp.EndTime = endTime;
            emp.LanguageId = languageId;
            emp.CreationTime = DateTime.Now;
            emp.InStatistic = true;
            emp.StatusId = 11;
            HeedbookMessengerStatic.Context().Dialogues.Add(emp);
            HeedbookMessengerStatic.Context().SaveChanges();

            var clientServiceBus = messenger.serviceBus;
            clientServiceBus.Publish(EnvVar.Get("TopicDialogueCreation"), JsonConvert.SerializeObject(sb));
        }

        // frames info structure
        public class FramesInfoStructure
        {
            public ObjectId _id;
            public string ApplicationUserId;
            public string FaceId;
            public DateTime Time;
            public bool FileExist;
            public string Status;
            public string StatusNN;
            public string FileName;
        }

        // message structure
        public class MessageStructure
        {
            public Guid DialogueId { get; set; }
            public string ApplicationUserId { get; set; }
            public string BegTime { get; set; }
            public string EndTime { get; set; }
            public int LanguageId { get; set; }
        }


    }
}
