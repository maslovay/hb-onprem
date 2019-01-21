using System;
using System.IO;
using System.Text.RegularExpressions;
using HBLib.AzureFunctions;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace OperationService.Legacy
{
    public static class AudioBlobVideoToSound
    {
        [FunctionName("Audio_Blob_VideoToSound")]
        public static void Run(string msg,
            ExecutionContext dir,
            ILogger log)
        {
            //GRAB AUDIO FROM VIDEO
            var msgSplit = Regex.Split(msg, "/");
            var blobContainerName = msgSplit[0];
            var blobName = msgSplit[1];
            var name = blobName;
            var blob = HeedbookMessengerStatic.BlobStorageMessenger.GetBlob(blobContainerName, blobName);

            log.LogInformation($"{blobName}");

            // load blob metadata
            blob.FetchAttributesAsync();
            var blobMetadata = blob.Metadata;

            var sessionId = Misc.GenSessionId();

            var basename = Path.GetFileNameWithoutExtension(name);
            var nameSplit = Path.GetFileNameWithoutExtension(name).Split('_');
            var applicationUserId = nameSplit[0];

            try
            {
                //log.Info($"Processing video {name}");

                var ffmpeg = new FFMpegWrapper(Path.Combine(Misc.BinPath(dir), "ffmpeg.exe"));
                var localDir = Misc.GenLocalDir(sessionId);
                ;
                var localVideoFn = Path.Combine(localDir, name);
                var localAudioFn = Path.Combine(localDir, basename + ".wav");
                try
                {
                    using (var output = new System.IO.FileStream(localVideoFn, FileMode.Create))
                    {
                        blob.DownloadToStreamAsync(output);
                    }

                    var res = ffmpeg.VideoToWav(localVideoFn, localAudioFn);

                    if (!File.Exists(localAudioFn))
                    {
                        //var fullName = HeedbookMessengerStatic.context.ApplicationUsers.FirstOrDefault(p => p.Id.ToString() == applicationUserId).FullName; 
                        //HeedbookMessengerStatic.SlackMessenger.Post($"Exception occured while executing Audio_Blob_VideoToSound, No sounds on video - {applicationUserId}", "#1555e0");
                        OS.SafeDelete(localVideoFn);
                        OS.SafeDelete(localDir);
                        log.LogError($"No sound in video file {name}");
                        return;
                    }
                    else
                    {
                        using (var stream = File.Open(localAudioFn, FileMode.Open))
                        {
                            var dialogueAudiosContainer = EnvVar.Get("BlobContainerDialogueAudios");
                            HeedbookMessengerStatic.BlobStorageMessenger.SendBlob(dialogueAudiosContainer,
                                basename + ".wav", stream, blobMetadata,
                                topicName: $"blob-{dialogueAudiosContainer}");
                            //log.Info($"Audio file uploaded: {basename}.wav");
                        }

                        //log.Info("Deleting fns");
                        OS.SafeDelete(localVideoFn);
                        OS.SafeDelete(localAudioFn);
                        OS.SafeDelete(localDir);
                    }
                }
                catch (Exception e)
                {
                    log.LogError($"Exception occured while executing Audio_Blob_VideoToSound {e}");
                    try
                    {
                        OS.SafeDelete(localVideoFn);
                    }
                    catch
                    {
                    }

                    try
                    {
                        OS.SafeDelete(localAudioFn);
                    }
                    catch
                    {
                    }

                    try
                    {
                        OS.SafeDelete(localDir);
                    }
                    catch
                    {
                    }

                    throw;
                }

                log.LogError($"Function finished: {dir.FunctionName}");
            }
            catch (Exception e)
            {
                log.LogError("Exception occured {e}", e);
                throw;
            }
        }
    }
}