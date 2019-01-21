using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HBLib.AzureFunctions;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace OperationService.Legacy
{
    public static class AudioScheduleSonorousControl
    {
        [FunctionName("Audio_Schedule_SonorousControl")]
        // each hour
        public static async System.Threading.Tasks.Task RunAsync([TimerTrigger("0 0 * * * *")] TimerInfo myTimer,
            ILogger log, ExecutionContext dir)
        {
            var silentUsers = new List<UserInfo>();

            var usersOnline = HeedbookMessengerStatic.Context().Sessions
                                                     .Include(p => p.ApplicationUser)
                                                     .Include(p => p.ApplicationUser.Company)
                                                     .Where(p => p.StatusId == 6)
                                                     .Select(p => new UserInfo
                                                      {
                                                          ApplicationUserId = p.ApplicationUserId.ToString(),
                                                          CompanyName = p.ApplicationUser.Company.CompanyName,
                                                          FullName = p.ApplicationUser.FullName
                                                      });

            foreach (var user in usersOnline)
            {
                var videosCollection =
                    HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(
                        Environment.GetEnvironmentVariable("CollectionBlobVideos"));
                var mask = new BsonDocument
                {
                    {"ApplicationUserId", user.ApplicationUserId}
                };
                var sort = new BsonDocument {{"BegTime", 1}};
                var docs = videosCollection.Find(mask).SortByDescending(p => p["_id"]).Limit(1).ToList();
                if (docs.Count() != 0)
                {
                    var blobName = docs[0]["BlobName"].ToString();
                    var blobContainerName = "videos";
                    var name = blobName;

                    var blob = HeedbookMessengerStatic.BlobStorageMessenger.GetBlob(blobContainerName, blobName);

                    // load blob metadata
                    await blob.FetchAttributesAsync();
                    var blobMetadata = blob.Metadata;

                    var sessionId = Misc.GenSessionId();

                    var basename = Path.GetFileNameWithoutExtension(name);
                    var nameSplit = Path.GetFileNameWithoutExtension(name).Split('_');
                    var applicationUserId = nameSplit[0];

                    try
                    {
                        var ffmpeg = new FFMpegWrapper(Path.Combine(Misc.BinPath(dir), "ffmpeg.exe"));
                        var localDir = Misc.GenLocalDir(sessionId);
                        ;
                        var localVideoFn = Path.Combine(localDir, name);
                        var localAudioFn = Path.Combine(localDir, basename + ".wav");
                        try
                        {
                            using (var output = new System.IO.FileStream(localVideoFn, FileMode.Create))
                            {
                                await blob.DownloadToStreamAsync(output);
                            }

                            var res = ffmpeg.VideoToWav(localVideoFn, localAudioFn);

                            if (!File.Exists(localAudioFn))
                            {
                                silentUsers.Add(user);
                                OS.SafeDelete(localVideoFn);
                                OS.SafeDelete(localDir);
                                log.LogInformation($"No sound for user {user.ApplicationUserId}, {user.FullName}");
                            }
                            else
                            {
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
                    }
                    catch (Exception e)
                    {
                        log.LogError("Exception occured {e}", e);
                        throw;
                    }
                }
            }

            if (silentUsers.Count() != 0)
            {
                var text = $"No audio for users: {Environment.NewLine}";
                foreach (var user in silentUsers)
                {
                    text +=
                        $"Full name ------ {user.FullName}, CompanyName ------ {user.CompanyName} {Environment.NewLine}";
                }

//                var apiKey = EnvVar.Get("SendGridApiKey");
//                var client = new SendGridClient(apiKey);
//
//                //msg.AddTo(new EmailAddress("otradnova7777@gmail.com", "Teacher"));
//                var response = await client.SendEmailAsync(msg);
                log.LogInformation($"Function finished: {dir.FunctionName}");
            }
            else
            {
                log.LogInformation($"Function finished: {dir.FunctionName}");
            }
        }

        public class UserInfo
        {
            public string ApplicationUserId;
            public string FullName;
            public string CompanyName;
        }
    }
}