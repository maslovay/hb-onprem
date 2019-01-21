using HBLib.AzureFunctions;
using HBLib.Extensions;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage.Blob;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HBFrames
{
    public static class Video_Blob_FramesFromVideoMemory
    {
        [FunctionName("Video_Blob_FramesFromVideoMemory")]
        public static async Task RunAsync(string mySbMsg, ILogger log, ExecutionContext dir)
        {
            if (Convert.ToBoolean(EnvVar.Get("IsFramesInMemory")))
            {
                var msgSplit = Regex.Split(mySbMsg, "/");
                var blobContainerName = msgSplit[0];
                var blobName = msgSplit[1];
                var name = blobName;

                var blob = HeedbookMessengerStatic.BlobStorageMessenger.GetBlob(blobContainerName, blobName);

                //parse file name
                var nameSplit = Path.GetFileNameWithoutExtension(name).Split('_');
                var applicationUserId = nameSplit[0];
                var t = nameSplit[1];
                var dt = DT.Parse(t);
                var languageId = Convert.ToInt32(nameSplit[2]);

                try
                {
                    var collectionBlobVideos = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionBlobVideos"));
                    var framesList = ExtractFrames(applicationUserId, dt, languageId, blobName);

                    var duration = VideoDuration(blobName, log);
                    var doc = new BsonDocument {
                        { "ApplicationUserId", applicationUserId },
                        { "Time", dt},
                        { "BlobName", name},
                        { "BlobContainer", EnvVar.Get("BlobContainerVideos")},
                        { "CreationTime", DateTime.Now},
                        { "Duration", duration},
                        { "BegTime", dt},
                        { "EndTime", dt.AddSeconds(duration)},
                        { "FileExist", true},
                        { "Status", "Active" }};

                    HeedbookMessengerStatic.MongoDBMessenger.SafeReplaceOne(
                        collectionBlobVideos,
                        new BsonDocument { { "ApplicationUserId", applicationUserId},
                            { "BegTime", dt},
                            //{ "EndTime", dt.AddSeconds(duration) }
                        },
                        doc);

                    var frameDocs = new List<BsonDocument>();
                    foreach (var frameName in framesList)
                    {
                        var delimiter = '_';
                        var split = frameName.Split(delimiter);
                        if (HeedbookMessengerStatic.BlobStorageMessenger.Exist(EnvVar.Get("BlobContainerFrames"), frameName))
                        {
                            var frameDoc = new BsonDocument {
                        { "FileName", frameName},
                        { "ApplicationUserId", applicationUserId},
                        { "Time", DateTime.SpecifyKind(DateTime.ParseExact(split[1], "yyyyMMddHHmmss", CultureInfo.InvariantCulture), DateTimeKind.Utc)},
                        { "Status", "InProgress" },
                        { "StatusNN", "InProgress"},
                        { "FileExist", true},
                        { "FaceId", ""} };
                            frameDocs.Add(frameDoc);

                            var sbMessage = $"{EnvVar.Get("BlobContainerFrames")}/{frameName}";
                            HeedbookMessengerStatic.ServiceBusMessenger.Publish($"blob-{EnvVar.Get("BlobContainerFrames")}", sbMessage);
                        }
                        else
                        {
                            // to do: change to elastic logger
                            log.LogError($"No frame in Blob Storage with name {frameName}");
                        }
                    }

                    var collectionFrameInfo = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionFrameInformation"));
                    try
                    {
                        HeedbookMessengerStatic.MongoDBMessenger.SafeInsert(collectionFrameInfo, frameDocs);
                    }
                    catch (Exception e)
                    {
                        log.LogError($"Failed to uploaded information about frames to MongoDB {e}");
                    }

                    var publishJs = new Dictionary<string, string> { { "ApplicationUserId", applicationUserId } };
                    HeedbookMessengerStatic.ServiceBusMessenger.Publish(EnvVar.Get("TopicDialogueAutoCreation"), publishJs.JsonPrint());
                    log.LogInformation($"Function finished {dir.FunctionName}");
                }
                catch (Exception e)
                {
                    log.LogError($"Exception occured {e}");
                    throw e;
                }
            }
        }

        public static double VideoDuration(string filename, ILogger log)
        {
            var blob = HeedbookMessengerStatic.BlobStorageMessenger.GetBlob(EnvVar.Get("BlobContainerVideos"), filename);

            using (MemoryStream memStream = new MemoryStream())
            {
                blob.DownloadToStreamAsync(memStream);

                // Configuration of ffmpeg process that will be created
                var psi = new ProcessStartInfo(@"D:\home\site\wwwroot\bin\ffmpeg.exe")
                {
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    Arguments = "-i pipe:0 -f null pipe:1"
                };

                // Use configuration of process and run it
                var process = new Process { StartInfo = psi };
                process.Start();

                // Write to StdIn(Async)
                var inputTask = Task.Run(() =>
                {
                    StreamWriter rawToProc = process.StandardInput;
                    rawToProc.BaseStream.Write(memStream.ToArray(), 0, memStream.ToArray().Length);
                    process.StandardInput.Close();
                });

                // START BLOCK Write Stdout of ffmpeg to byte array(ffmpegOut)
                FileStream baseStream = process.StandardError.BaseStream as FileStream;
                byte[] ffmpegOut;
                int lastRead = 0;
                using (MemoryStream ms = new MemoryStream())
                {
                    // to do: 4096 ? Answer: Count of bytes in iteration. We can use any number
                    byte[] buffer = new byte[5];
                    do
                    {
                        lastRead = baseStream.Read(buffer, 0, buffer.Length);
                        ms.Write(buffer, 0, lastRead);
                    }
                    while (lastRead > 0);

                    ffmpegOut = ms.ToArray();
                }
                // END BLOCK
                Stream StreamForUpload = new MemoryStream();
                long ffmpegOutLen = ffmpegOut.Length;

                StreamForUpload.Write(ffmpegOut, 0, ffmpegOut.Length);
                StreamForUpload.Seek(0, SeekOrigin.Begin);

                //create variable with information about video(standarderror of ffmpeg)
                string result = Encoding.UTF8.GetString((StreamForUpload as MemoryStream).ToArray());

                var pattern = @"Lsize=N/A time=(.+)\s?bitrate";
                var matches = Regex.Matches(result, pattern);
                var match = matches[matches.Count - 1];
                var captured = match.Groups[1].ToString();

                log.LogInformation($"Captured --- {captured}, Result ---- {result}, Matches Count --- {matches.Count}");

                Task.WaitAll(inputTask);
                process.WaitForExit();

                
                var ts = TimeSpan.Parse(captured);
                return ts.TotalSeconds;
            }
        }

        public static  List<string> ExtractFrames(string applicationUserId, DateTime dt, int languageId, string filename)
        {
            var framesList = new List<string>();
            var blob = HeedbookMessengerStatic.BlobStorageMessenger.GetBlob(EnvVar.Get("BlobContainerVideos"), filename);
            var framestep = 3;

            using (MemoryStream memStream = new MemoryStream())
            {
                blob.DownloadToStreamAsync(memStream);
                var processStartInfo = new ProcessStartInfo(@"D:\home\site\wwwroot\bin\ffmpeg.exe")
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    Arguments = $"-i pipe:0 -f image2 -vf fps=1/{framestep} -update 1 pipe:1"
                };

                var process = new Process { StartInfo = processStartInfo };
                process.Start();

                // Write to StdIn of ffmpeg (Async)
                var inputTask = Task.Run(() =>
                {
                    StreamWriter rawToProc = process.StandardInput;
                    rawToProc.BaseStream.Write(memStream.ToArray(), 0, memStream.ToArray().Length);
                    process.StandardInput.Close();
                });

                // START BLOCK Write Stdout of ffmpeg to byte array(ffmpegOut)
                FileStream baseStream = process.StandardOutput.BaseStream as FileStream;

                byte[] ffmpegOut;
                int lastRead = 0;
                using (MemoryStream ms = new MemoryStream())
                {
                    //Size of buffer is size of step in loop that we use for get all bytes from stdout stream
                    byte[] buffer = new byte[4096];
                    do
                    {
                        lastRead = baseStream.Read(buffer, 0, buffer.Length);
                        ms.Write(buffer, 0, lastRead);
                    }
                    while (lastRead > 0);
                    ffmpegOut = ms.ToArray();
                }
                // END BLOCK Write Stdout of ffmpeg to byte array(ffmpegOut) 
                Stream StreamForUpload = new MemoryStream();
                bool isUpload = false;
                int blobNamePrefix = 0;

                CloudBlockBlob blobFrame = null;

                for (int i = 0; i < ffmpegOut.Length - 3; i++)
                {
                    try
                    {
                        // Detect jpeg bytes start file signature
                        if (ffmpegOut[i] == 255 && ffmpegOut[i + 1] == 216)
                        {
                            isUpload = true;
                            var blobName = $"{applicationUserId}_{DT.Format(dt.AddSeconds(framestep * blobNamePrefix))}_{languageId}.jpg";
                            framesList.Add(blobName);
                            blobFrame = HeedbookMessengerStatic.BlobStorageMessenger.GetBlob(EnvVar.Get("BlobContainerFrames"), blobName);
                            blobNamePrefix++;
                        }
                        // Detect jpeg bytes end file signature
                        if (ffmpegOut[i + 2] == 255 && ffmpegOut[i + 3] == 217)
                        {
                            isUpload = false;
                            StreamForUpload.Seek(0, SeekOrigin.Begin);
                            blobFrame.UploadFromStreamAsync(StreamForUpload);
                            StreamForUpload.SetLength(0);
                        }
                        // Write to stream jpeg content between start and end file signature
                        if (isUpload == true)
                        {
                            StreamForUpload.WriteByte((byte)ffmpegOut[i]);
                        }
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }

                Task.WaitAll(inputTask);
                process.WaitForExit();
                return framesList;
            }
        }
    }
}
