using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HBLib.AzureFunctions;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceBus.Messaging;

namespace OperationService.Legacy
{
    public static class MobileBlobCropToKeyframes
    {
        [FunctionName("Mobile_Blob_CropToKeyframes")]
        public static void Run(
            string msg,
            ExecutionContext dir,
            ILogger log)
        {
            var sessionId = Misc.GenSessionId();

            var msgSplit = Regex.Split(msg, "/");
            var blobContainerName = msgSplit[0];
            var blobName = msgSplit[1];
            var name = blobName;
            var blob = HeedbookMessengerStatic.BlobStorageMessenger.GetBlob(blobContainerName, blobName);
            //parse file name
            var basename = Path.GetFileNameWithoutExtension(name);
            var nameSplit = basename.Split('_');
            var applicationUserId = nameSplit[0];
            var t = nameSplit[1];
            var dt = DT.Parse(t);

            try
            {
                var extension = Path.GetExtension(name);

                var ffmpegapp = Path.Combine(Misc.BinPath(dir), "ffmpeg.exe");
                var ffmpeg = new FFMpegWrapper(ffmpegapp);

                var localDir = Misc.GenLocalDir(sessionId);

                try
                {
                    //log.Info($"Processing video: {name}");
                    var localFn = Path.Combine(localDir, name);

                    //load the file
                    using (var output = new System.IO.FileStream(localFn, FileMode.Create))
                    {
                        blob.DownloadToStreamAsync(output);
                    }

                    ffmpeg.SplitToKeyFrames(localFn, localDir);
                    // rename fns
                    var fns = new List<string>(OS.GetFiles(localDir, "*"));
                    var prefix = "keyframe";
                    fns = fns.Where(fn => Path.GetFileName(fn).StartsWith(prefix))
                             .OrderBy(fn =>
                                  Convert.ToInt32(Path.GetFileNameWithoutExtension(fn).Substring(prefix.Count())))
                             .ToList();

                    // merge keyframes
                    var durations = fns.Select(fn => ffmpeg.GetDuration(fn)).ToList();
                    Directory.CreateDirectory(Path.Combine(localDir, "blobs"));
                    double curDuration = 0;
                    var curBatch = new List<string>();
                    int i = 0;

                    while (i < fns.Count())
                    {
                        while ((curDuration <= 12.0) & (i < fns.Count()))
                        {
                            curBatch.Add(fns[i]);
                            curDuration += durations[i];
                            i++;
                        }

                        //todo: merge curBatch
                        if (curDuration == 0)
                        {
                            break;
                        }

                        var newFn = nameSplit[0] + '_' + DT.Format(dt) + "_" + nameSplit[2] + extension;
                        newFn = Path.Combine(localDir, "blobs", newFn);
                        ffmpeg.ConcatSameCodecs(curBatch, newFn, localDir);
                        dt = dt.AddSeconds(curDuration);

                        curDuration = 0;
                        curBatch = new List<string>();
                    }

                    fns = OS.GetFiles(Path.Combine(localDir, "blobs"), "*").ToList();
                    // rename fns
                    for (int j = 0; j < fns.Count(); j++)
                    {
                        var fn = fns[j];
                        //log.Info($"Processing file: {fn}");

                        var metadata = new Dictionary<string, string> {{"duration", durations[j].ToString()}};

                        try
                        {
                            using (var fileStream = System.IO.File.OpenRead(fn))
                            {
                                var videosContainer = EnvVar.Get("BlobContainerVideos");
                                HeedbookMessengerStatic.BlobStorageMessenger.SendBlob(videosContainer,
                                    Path.GetFileName(fn), fileStream,
                                    topicName: $"blob-{videosContainer}",
                                    metadata: metadata);
                                //log.Info($"Blob is uploaded {fn}");
                            }
                        }
                        catch (IOException e)
                        {
                            log.LogError($"Can't upload file {fn}, exception occured {e}");
                        }
                    }

                    OS.SafeDelete(localDir);
                    log.LogInformation($"Function finished: {dir.FunctionName}");
                }
                catch (Exception e)
                {
                    log.LogError($"Exception 1 occured {e}");
                    OS.SafeDelete(localDir);
                    throw;
                }
            }
            catch (Exception e)
            {
                log.LogError($"Exception occured {e}");
                throw;
            }
        }
    }
}