using System;
using System.Linq;
using HBLib.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace OperationService.Legacy
{
    public static class ServiceScheduleSessionFixer
    {

        [FunctionName("Service_Schedule_SessionFixer")]
        public static void Run([TimerTrigger("0 */50 * * * *")]TimerInfo myTimer,
            ILogger log,
            ExecutionContext dir)
        {

            try
            {
                //log.Info($"Automatic session closing at {DateTime.Now}");
                int maxBlobDelaySeconds = Int32.Parse(EnvVar.Get("MaxBlobDelaySeconds"));

                //get all unclosed & not young sessions 
                var allUnclosedSessions = HeedbookMessengerStatic.Context().Sessions.Where(p => (p.StatusId == 6 || p.StatusId == null) && p.BegTime < DateTime.UtcNow.AddSeconds(-maxBlobDelaySeconds)).ToList();
                log.LogInformation($"Sessions: {allUnclosedSessions.Count()} are opened");

                foreach (var session in allUnclosedSessions)
                {
                    try
                    {
                        IMongoCollection<BsonDocument> blobCollection = null;

                        //check type of session and get the blob set
                        if (session.IsDesktop)
                        {
                            blobCollection = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionBlobVideos"));
                        }
                        else
                        {
                            blobCollection = HeedbookMessengerStatic.MongoDB.GetCollection<BsonDocument>(EnvVar.Get("CollectionBlobVideosMobileApp"));
                        }

                        //get last video blob from desktop
                        var videoBlobs = blobCollection.Find(new BsonDocument {{ "ApplicationUserId", session.ApplicationUserId },
                                                                                { "BegTime", new BsonDocument  {{ "$gte", session.BegTime}} } }).Sort(Builders<BsonDocument>.Sort.Descending("BegTime"));



                        if (videoBlobs.Any())
                        {
                            log.LogInformation("Last video from user = "+ videoBlobs.First().GetValue("EndTime").ToString());
                            DateTime lastBlobEndTime = DateTime.Parse(videoBlobs.First().GetValue("EndTime").ToString());
                            
                            //check sessions without new stream
                            if (lastBlobEndTime < DateTime.UtcNow.AddSeconds(-maxBlobDelaySeconds))
                            {
                                session.EndTime = lastBlobEndTime;
                                session.StatusId = 7;

                                //send notification
                                var user = HeedbookMessengerStatic.Context().ApplicationUsers.First(p => p.ApplicationUserId == session.ApplicationUserId);
                                HeedbookMessengerStatic.PushNotificationMessenger.SendNotificationToCompanyManagers(user.CompanyId.Value, "Session closed " + user.FullName, "Start time:" + session.BegTime + ", duration:" + Math.Round(session.EndTime.Subtract(session.BegTime).TotalHours, 2) + " hours", "/home/index");

                                HeedbookMessengerStatic.Context().SaveChanges();

                                log.LogInformation($"Session user {session.ApplicationUserId} forsed closed, start at {session.BegTime} because of no stream. Last blob was at {lastBlobEndTime}");
                            }
                        }

                        //no stream from this session, remove session
                        if (!videoBlobs.Any())
                        {
                            HeedbookMessengerStatic.Context().Sessions.Remove(session);
                            HeedbookMessengerStatic.Context().SaveChanges();
                            log.LogInformation($"Session user {session.ApplicationUserId} forsed deleted, start at {session.BegTime} because of no stream.");
                        }
                    }
                    catch (Exception e)
                    {
                        log.LogError($"Exception occured {e}");
                        throw;
                    }
                }
                log.LogInformation($"Function finished: {dir.FunctionName}");
            }
            catch (Exception e)
            {
                log.LogError($"Exception occured {e}");
                throw;
            }
        }
    }
}