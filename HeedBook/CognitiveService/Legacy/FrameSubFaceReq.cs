using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CognitiveService.Model;
using HBData.Models;
using HBLib.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.ProjectOxford.Face;
using MongoDB.Bson;
using MongoDB.Driver;
using RabbitMqEventBus.Models;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace CognitiveService.Legacy
{
    public class FrameSubFaceReq
    {
        private readonly ILogger _logger;
        public FrameSubFaceReq(ILogger logger)
        {
            _logger = logger;
        }
        
        private readonly List<String> _faceClientStrings = EnvVar
                                                                .GetAll().Where(kvp =>
                                                                     kvp.Key.StartsWith("FaceServiceClient"))
                                                                .Select(kvp => kvp.Value).ToList();
        //private static List<FaceServiceClient> faceClients = faceClientStrings.Select(faceClientString => new FaceServiceClient(faceClientString, EnvVar.Get("FaceServiceApiRoot"))).ToList();
        public async Task Run(
            FaceRecognitionMessage msg,
            ExecutionContext dir)
        {
            /*{
              "_id": "5a19bc8f8f041747b8b39ba3",
              "ApplicationUserId": "47e1d456-7e4b-42ff-beb0-089e146c000c",
              "T": "2017-11-20T13:30:33.000Z",
              "FacesLength": 0,
              "IsFacePresent": false,
              "BlobName": "C:/Users/arsen/Desktop/hb/hb-operations/dev/HB_Operations_Cons_CSharp/HB_Operations_Cons_CSharp/bin/Debug/net461/Video_Blob_FramesFromVideo/frames/47e1d456-7e4b-42ff-beb0-089e146c000c_20171120163033.jpg",
              "BlobContainer": "frames",
              "Status": "InProgress",
              "CreationTime": "2017-11-25T18:55:11.081Z"
            }*/

            String blobContainer = msg.BlobContainer;
            blobContainer = blobContainer ?? EnvVar.Get("BlobContainerFrames");

            String blobName = msg.BlobName;
            if (blobName == null) throw new Exception("BlobName is empty");

            var sas = HeedbookMessengerStatic.BlobStorageMessenger.GetBlobSASUrl(blobContainer, blobName);

            var nameSplit = Path.GetFileNameWithoutExtension(blobName).Split('_');
            var applicationUserId = nameSplit[0];
            var t = nameSplit[1];
            var dt = DT.Parse(t);

            try
            {
                var faceClientIndex = DateTime.Now.Millisecond % _faceClientStrings.Count;
                var subscriptionKey = _faceClientStrings[faceClientIndex];
                var cli = new FaceServiceClient(subscriptionKey, EnvVar.Get("FaceServiceApiRoot"));

                var res = await cli.DetectAsync(sas, true, false,
                    new[]
                    {
                        FaceAttributeType.Age, FaceAttributeType.Gender,
                        FaceAttributeType.HeadPose
                    });
                
                //&& applicationUserId == "178bd1e8-e98a-4ed9-ab2c-ac74734d1903"
                if (Convert.ToBoolean(EnvVar.Get("IsPeopleDetection")))
                    if (res.Any())
                    {
                        _logger.LogInformation($"Processing blob {blobName}");
                        Boolean result;
                        try
                        {
                            result = await Retry.Do(
                                () => DetermineFaceAsync(dt, applicationUserId, blobName),
                                TimeSpan.FromSeconds(1), 5);
                        }
                        catch
                        {
                            result = false;
                        }

                        ;
                        if (result) _logger.LogInformation("Frame successfully determined");
                        else _logger.LogInformation("Determined failed");
                    }

                var collection =
                    HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(
                        Environment.GetEnvironmentVariable("CollectionFrameFaceMicrosoft"));
                var doc = new FaceRecognitionModel
                {
                    ApplicationUserId = Guid.Parse(applicationUserId),
                    Time = dt,
                    CreationTime = DateTime.Now,
                    BlobName = blobName,
                    BlobContainer = blobContainer,
                    Status = VideoStatus.Active
                };

                HeedbookMessengerStatic.MongoDBMessenger.SafeReplaceOne(collection,
                    new BsonDocument
                    {
                        {"ApplicationUserId", applicationUserId},
                        {"Time", dt}
                    },
                    doc);

                //log.Info("Added FaceReq document to mongodb database: {doc}", doc.ToJson());
                _logger.LogInformation($"Function finished: {dir.FunctionName}");
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception occured {e}");
                throw;
            }
        }

        public async Task<Boolean> DetermineFaceAsync(DateTime dt, String applicationUserId, String blobName)
        {
            var faceId = String.Empty;
            var lastFramesCount = 8;
            var minSeconds = 300;

            var begTime = dt.AddSeconds(-minSeconds);

            try
            {
                //var framesDocs = HeedbookMessengerStatic.mongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionFrameFaceMicrosoft"));
                var framesDocs =
                    HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(
                        Environment.GetEnvironmentVariable("CollectionFrameInformation"));

                var mask = new BsonDocument
                {
                    {"ApplicationUserId", applicationUserId},
                    {"FaceId", new BsonDocument {{"$ne", ""}}},
                    //{ "FaceId", new BsonDocument { { "$ne", new BsonString(string.Empty) } } },
                    // { "$nor", new BsonArray{ new BsonDocument{{ "Value", new BsonDocument { { "$size", 0} } } } } },
                    {"Time", new BsonDocument {{"$gte", begTime}, {"$lte", dt}}}
                };
                var sort = new BsonDocument {{"Time", 1}};
                var docs = framesDocs.Find(mask).SortByDescending(p => p["_id"]).Limit(lastFramesCount).ToList();

                var faceIds = new List<String>();
                foreach (var frameDoc in docs)
                {
                    var faceClientIndexDoc = DateTime.Now.Millisecond % _faceClientStrings.Count;
                    var subscriptionKeyDoc = _faceClientStrings[faceClientIndexDoc];

                    var isSame = await HeedbookIdentity.CompareMessenger.FaceCompare(subscriptionKeyDoc, blobName,
                        frameDoc["FileName"].ToString());
                    if (isSame)
                    {
                        faceId = frameDoc["FaceId"].ToString();
                        faceIds.Add(faceId);

                        var r = new Random();
                        var sleepTime = r.Next(100, 200);
                        Thread.Sleep(sleepTime);
                        break;
                    }

                    //faceId = faceIds.GroupBy(p => p).OrderByDescending(p => p.Count()).Select(p => p.Key).First();
                }

                // adding to frames information
                faceId = faceId != String.Empty ? faceId : Guid.NewGuid().ToString();
                var applicatiom = new ApplicationUser();
                var filter = new BsonDocument {{"FileName", blobName}};
                var update = new BsonDocument {{"$set", new BsonDocument {{"FaceId", faceId}}}};

                framesDocs.UpdateOne(filter, update);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public class FaceGroup
        {
            public ObjectId _id;
            public String CompanyId;
            public String CompanyName;
            public String FaceGroupId;
            public List<String> ForbiddenFaces;
            public String Status;
            public String SubscriptionKey;
        }
    }
}