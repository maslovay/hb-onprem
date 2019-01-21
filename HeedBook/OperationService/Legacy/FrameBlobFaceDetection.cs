//using System;
//using System.Drawing;
//using System.IO;
//using System.Text.RegularExpressions;
//using HBLib.AzureFunctions;
//using HBLib.Utils;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Azure.WebJobs.Host;
//using Microsoft.Extensions.Logging;
//using Microsoft.ServiceBus.Messaging;
//using MongoDB.Bson;
//using MongoDB.Bson.IO;
//
//namespace OperationService.Legacy
//{
//    public static class FrameBlobFaceDetection
//    {
//        [FunctionName("Frame_Blob_FaceDetection")]
//        public static void Run(
//            string msg,
//            ExecutionContext dir,
//            ILogger log)
//        {
//            var sessionId = Misc.GenSessionId();
//            var localDir = Misc.GenLocalDir(sessionId);
//
//            var msgSplit = Regex.Split(msg, "/");
//            var containerName = msgSplit[0];
//            var blobName = msgSplit[1];
//            var name = blobName;
//
//            var blob = HeedbookMessengerStatic.BlobStorageMessenger.GetBlob(containerName, blobName);
//
//            var blobStream = new MemoryStream();
//            blob.DownloadToStreamAsync(blobStream);
//
//            var nameSplit = Path.GetFileNameWithoutExtension(name).Split('_');
//            var applicationUserId = nameSplit[0];
//            var t = nameSplit[1];
//            var dt = DT.Parse(t);
//            var langId = Convert.ToInt32(nameSplit[2]);
//
//
//            try
//            {
//                //DETECT FACE IN IMAGE
//                var haarcascadeFn = Path.Combine(Misc.BinPath(dir), "haarcascade_frontalface_alt2.xml");
//                var localHaarcascadeFn = Path.Combine(localDir, Path.GetFileName(haarcascadeFn));
//                try
//                {
//                    File.Copy(haarcascadeFn, localHaarcascadeFn);
//
//                    var face = Retry.Do(() => { return new CascadeClassifier(localHaarcascadeFn); },
//                        TimeSpan.FromSeconds(1), 5);
//
//                    var img = new Image<Bgr, byte>(new Bitmap(blobStream));
//
//                    var faces = face.DetectMultiScale(img);
//
//                    var collection =
//                        HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(
//                            Environment.GetEnvironmentVariable("CollectionFrameFacedetection"));
//
//                    var now = DateTime.Now;
//                    var doc = new BsonDocument
//                    {
//                        {"ApplicationUserId", applicationUserId},
//                        {"Time", dt},
//                        {"FacesLength", faces.Length},
//                        {"IsFacePresent", faces.Length != 0},
//                        {"BlobName", name},
//                        {"BlobContainer", "frames"},
//                        {"Status", "InProgress"},
//                        {"CreationTime", now}
//                    };
//                    // insert safely
//                    HeedbookMessengerStatic.MongoDBMessenger.SafeReplaceOne(collection,
//                        new BsonDocument
//                        {
//                            {"ApplicationUserId", applicationUserId},
//                            {"Time", dt}
//                        },
//                        doc);
//
//                    var jsonWriterSettings = new JsonWriterSettings {OutputMode = JsonOutputMode.Strict};
//                    doc["Time"] = DT.Format(dt);
//                    doc["CreationTime"] = DT.Format(now);
//                    doc["Status"] = "Active";
//                    if (faces.Length != 0)
//                    {
//                        HeedbookMessengerStatic.ServiceBusMessenger.Publish(EnvVar.Get("TopicFrameFacedetection"),
//                            doc.ToJson(jsonWriterSettings));
//                    }
//
//                    //log.Info("Added document to mongodb database: {doc}", doc.ToJson().JsonPrint());
//
//                    OS.SafeDelete(localHaarcascadeFn);
//                    OS.SafeDelete(localDir);
//
//                    log.LogError($"Function finished: {dir.FunctionName}");
//                }
//                catch (Exception e)
//                {
//                    log.LogError($"Exception 1 occured {e}");
//                    OS.SafeDelete(localHaarcascadeFn);
//                    OS.SafeDelete(localDir);
//                    throw;
//                }
//            }
//            catch (Exception e)
//            {
//                log.LogError($"Exception occured {e}");
//                throw;
//            }
//        }
//    }
//}