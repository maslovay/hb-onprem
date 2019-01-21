using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HBLib.AzureFunctions;
using HBLib.Extensions;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using MongoDB.Bson;

namespace FrameFromVideoService.Legacy
{
    public static class VideoBlobFramesFromVideo
    {
        [FunctionName("Video_Blob_FramesFromVideo")]
        public static void Run(
            string msg,
            ExecutionContext dir
        )
        {
            if (Convert.ToBoolean(EnvVar.Get("IsFramesInMemory"))) return;
            var sessionId = Misc.GenSessionId();

            var msgSplit = Regex.Split(msg, "/");
            var blobContainerName = msgSplit[0];
            var blobName = msgSplit[1];
            var name = blobName;

            var blob = HeedbookMessengerStatic.BlobStorageMessenger.GetBlob(blobContainerName, blobName);

            //parse file name
            var nameSplit = Path.GetFileNameWithoutExtension(name).Split('_');
            var applicationUserId = nameSplit[0];
            var t = nameSplit[1];
            var dt = DT.Parse(t);
            var langId = Convert.ToInt32(nameSplit[2]);

            //var log = LoggerFactory.CreateAdapter(ilog, dir, "{ApplicationUserId} {name}", applicationUserId, name);

            try
            {
                //log.Info($"Pr/ocessing video to frames: {name}");

                var localDir = Misc.GenLocalDir(sessionId);

                var framestep = 3;

                var ffmpeg = new FFMpegWrapper(Path.Combine(Misc.BinPath(dir), "ffmpeg.exe"));
                var cmd = new CMDWithOutput();

                var localVideoFn = Path.Combine(localDir, name);
                try
                {
                    //load the file
                    using (var output = new System.IO.FileStream(localVideoFn, FileMode.Create))
                    {
                        blob.DownloadToStreamAsync(output);
                    }

                    double duration;
                    try
                    {
                        duration = ffmpeg.GetDuration(localVideoFn);
                    }
                    catch
                    {
                        duration = 0;
                    }

                    //log.Info("Duration: {duration} Size: {size}", duration, new System.IO.FileInfo(localVideoFn).Length);

                    if (duration == 0)
                    {
                        throw new Exception($"Duration for video file is zero {localVideoFn}");
                    }

                    var collectionBlobVideos =
                        HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(
                            EnvVar.Get("CollectionBlobVideos"));

                    var doc = new BsonDocument
                    {
                        {"ApplicationUserId", applicationUserId},
                        {"Time", dt},
                        {"BlobName", name},
                        {"BlobContainer", EnvVar.Get("BlobContainerVideos")},
                        {"CreationTime", DateTime.Now},
                        {"Duration", duration},
                        {"BegTime", dt},
                        {"EndTime", dt.AddSeconds(duration)},
                        {"FileExist", true},
                        {"Status", "Active"}
                    };

                    HeedbookMessengerStatic.MongoDBMessenger.SafeReplaceOne(collectionBlobVideos,
                        new BsonDocument
                        {
                            {"ApplicationUserId", applicationUserId},
                            {"BegTime", dt},
                            {"EndTime", dt.AddSeconds(duration)}
                        },
                        doc);


                    //log.Info("Added document to mongodb database: {doc}", doc.ToJson());

                    // convert to frames and send to blob storage
                    var fns = new List<string>();

                    var ffmpegCmd = $"-i {localVideoFn}";

                    // 00:14:56 -> 5 frames, 00:15:00 -> 5 frames, 00:15:05 -> 6 frames 
                    for (int i = 0; i < (int) Math.Ceiling(duration / framestep); i++)
                    {
                        var fn = Path.Combine(localDir,
                            $"{applicationUserId}_{DT.Format(dt.AddSeconds(framestep * i))}_{langId}.jpg");
                        var ts = TimeSpan.FromSeconds(framestep * i);
                        ffmpegCmd = $"{ffmpegCmd} -ss {ts.ToString(@"hh\:mm\:ss")} -frames:v 1 {fn} ";
                        fns.Add(fn);
                    }

                    //log.Info("Number of frames: {nFrames} {fns}", (int)Math.Ceiling(duration / framestep), fns.JsonPrint());

                    // todo: access denied often happens here. Should change for something more proper
                    Retry.Do(() => cmd.runCMD(ffmpeg.ffPath, ffmpegCmd), TimeSpan.FromSeconds(5),
                        maxAttemptCount: 3);

                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));

                    var existingFrames = fns.Select(fn => File.Exists(fn)).ToList();

                    if (existingFrames.Count == 0)
                    {
                        throw new Exception($"No frames located for file {localVideoFn}");
                    }

                    var frameDocs = new List<BsonDocument>();
                    // load information about frames to mongoDB


                    //load frames to frames storage
                    foreach (var fn in fns)
                    {
                        if (!File.Exists(fn))
                        {
                            //log.Info("Something went wrong in making frames from the input blob {fn}", fn);
                            continue;
                        }

                        //log.Info($"Processing file: {fn}");

                        try
                        {
                            using (var fileStream = System.IO.File.OpenRead(fn))
                            {
                                var framesContainer = EnvVar.Get("BlobContainerFrames");
                                HeedbookMessengerStatic.BlobStorageMessenger.SendBlob(framesContainer,
                                    Path.GetFileName(fn), fileStream,
                                    topicName: $"blob-{framesContainer}");
                                //log.Info($"Blob is uploaded {fn}");
                            }
                        }
                        catch (IOException e)
                        {
                            //log.Error($"Can't load file {fn}" + " {e}", e);
                        }

                        var delimiter = '_';
                        var split = Path.GetFileName(fn).Split(delimiter);

                        if (HeedbookMessengerStatic.BlobStorageMessenger.Exist("frames", Path.GetFileName(fn)))
                        {
                            //log.Info($"Creating information about existing frame { Path.GetFileName(fn)}");
                            var frameDoc = new BsonDocument
                            {
                                {"FileName", Path.GetFileName(fn)},
                                {"ApplicationUserId", split[0]},
                                {
                                    "Time",
                                    DateTime.SpecifyKind(
                                        DateTime.ParseExact(split[1], "yyyyMMddHHmmss",
                                            CultureInfo.InvariantCulture), DateTimeKind.Utc)
                                },
                                {"Status", "InProgress"},
                                {"StatusNN", "InProgress"},
                                {"FileExist", true},
                                {"FaceId", ""}
                            };
                            frameDocs.Add(frameDoc);
                        }
                        else
                        {
                            //log.Info($"Creating information about not existing frame {Path.GetFileName(fn)}");
                            var frameDoc = new BsonDocument
                            {
                                {"FileName", Path.GetFileName(fn)},
                                {"ApplicationUserId", split[0]},
                                {
                                    "Time",
                                    DateTime.SpecifyKind(
                                        DateTime.ParseExact(split[1], "yyyyMMddHHmmss",
                                            CultureInfo.InvariantCulture), DateTimeKind.Utc)
                                },
                                {"Status", "InProgress"},
                                {"StatusNN", "InProgress"},
                                {"FileExist", false},
                                {"FaceId", ""}
                            };
                            frameDocs.Add(frameDoc);
                        }

                        OS.SafeDelete(fn);
                    }

                    var collectionFrameInfo =
                        HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(
                            EnvVar.Get("CollectionFrameInformation"));
                    try
                    {
                        HeedbookMessengerStatic.MongoDBMessenger.SafeInsert(collectionFrameInfo, frameDocs);
                        //log.Info("Information about frames succesfully uploaded to MongoDB");
                    }
                    catch (Exception e)
                    {
                        //log.Error("Failed to uploaded information about frames to MongoDB {e}", e);
                    }


                    //log.Info("Send message to serviceBus for Dialogue auto creation");
                    var publishJs = new Dictionary<string, string> {{"ApplicationUserId", applicationUserId}};
                    HeedbookMessengerStatic.ServiceBusMessenger.Publish(EnvVar.Get("TopicDialogueAutoCreation"),
                        publishJs.JsonPrint());

                    //delete video files
                    //video files delete files
                    OS.SafeDelete(localVideoFn);
                    OS.SafeDelete(localDir);
                }
                catch (Exception e)
                {
                    OS.SafeDelete(localVideoFn);
                    OS.SafeDelete(localDir);
                    throw;
                }

                //log.Info($"Function finished: {dir.FunctionName}");
            }
            catch (Exception e)
            {
                //log.Critical("Exception occured {e}", e);
                throw;
            }
        }
    }
}