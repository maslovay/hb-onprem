using AsrHttpClient;
using HBData.Models;
using HBData.Repository;
using HBLib.Model;
using HBLib.Utils;
using LemmaSharp;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Quartz;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBLib;
using Microsoft.EntityFrameworkCore;
using DialogueMarkUp.Utils;


namespace DialogueMarkUp.QuartzJobs
{
    public class CheckDialogueMarkUpJob : IJob
    {
        private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        private readonly INotificationPublisher _publisher;
        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly VectorCalculation _vectorCalc;
        private readonly ClassCreator _classCreator;

        public CheckDialogueMarkUpJob(IServiceScopeFactory factory,
            INotificationPublisher publisher,
            ElasticClientFactory elasticClientFactory)
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _publisher = publisher;
            _elasticClientFactory = elasticClientFactory;
            _vectorCalc = new VectorCalculation();
            _classCreator = new ClassCreator();
        }

         public async Task Execute(IJobExecutionContext context)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            var periodTime = 5 * 60; 
            var periodFrame = 10;

            try
            {
                var endTime = DateTime.UtcNow.AddMinutes(-30);
                var frameAttributes = _context.FrameAttributes
                    .Include(p => p.FileFrame)
                    .Where(p => p.FileFrame.StatusNNId == 6 && p.FileFrame.Time < endTime)
                    .OrderBy(p => p.FileFrame.Time)
                    .GroupBy(p => p.FileFrame.FileName)
                    .Select(p => p.FirstOrDefault())
                    .ToList();
                System.Console.WriteLine(frameAttributes.Count());
                var appUsers = frameAttributes.Select(p => p.FileFrame.ApplicationUserId).Distinct().ToList();
                
                var minTime = frameAttributes.Min(p => p.FileFrame.Time);
                var videos = _context.FileVideos.Where(p => appUsers.Contains(p.ApplicationUserId) && p.EndTime >= minTime).ToList();
                
                foreach (var applicationUserId in appUsers)
                {
                    var framesUser = frameAttributes
                        .Where(p => p.FileFrame.ApplicationUserId == applicationUserId)
                        .OrderBy(p => p.FileFrame.Time)
                        .ToList();
                    var videosUser = videos.Where(p => p.ApplicationUserId == applicationUserId).ToList();

                    framesUser = FindAllFaceId(framesUser, periodFrame, periodTime);
                    
                    var videoFacesUser = CreateVideoFaces(framesUser, videosUser);
                    _context.AddRange(videoFacesUser.Select(p => new VideoFace{
                        VideoFaceId = Guid.NewGuid(),
                        FileVideoId = p.Video.FileVideoId,
                        FaceId = JsonConvert.SerializeObject(p.FaceIds)
                    }));

                    var markUps = framesUser.GroupBy(p => p.FileFrame.FaceId)
                        .Where(p => p.Count() >= 2)
                        .Select(x => new MarkUp {
                            ApplicationUserId = applicationUserId,
                            FaceId = x.Key,
                            BegTime = x.Min(q => q.FileFrame.Time),
                            EndTime = x.Max(q => q.FileFrame.Time),
                            FileNames = x.OrderBy(p => p.FileFrame.Time).Select(q => q.FileFrame).ToList(),
                            Descriptor = x.First().Descriptor,
                            Gender = x.First().Gender,
                            Videos = videoFacesUser.Where(q => q.FaceIds.Contains(x.Key))
                                .Select(q => q.Video)
                                .OrderBy(q => q.BegTime)
                                .ToList()
                        })
                        .Where(p => p.EndTime.Subtract(p.BegTime).TotalSeconds > 10)
                        .OrderBy(p => p.EndTime)
                        .ToList();
                    if (markUps.Any()) 
                    {
                        
                        CreateMarkUp(markUps, framesUser, applicationUserId, _log);
                        
                    }
                }
                _context.SaveChanges();
                _log.Info("Function DialogueMarkUp finished");                
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception while executing DialogueMarkUp occured {e}");
                throw;
            }
        }

        public List<FrameAttribute> GetFrameVideo(FileVideo video, List<FrameAttribute> frames)
        {
            return frames.Where(p => 
                p.FileFrame.ApplicationUserId == video.ApplicationUserId && 
                p.FileFrame.Time <= video.EndTime &&
                p.FileFrame.Time >= video.BegTime).ToList();
        }

        public List<VideoFaceLocal> CreateVideoFaces(List<FrameAttribute> frames, List<FileVideo> videos)
        {
            var videoFaces = new List<VideoFaceLocal>();
            foreach (var video in videos)
            {
                videoFaces.Add(new VideoFaceLocal{
                    Video = video,
                    FaceIds = GetFrameVideo(video, frames).Select(p => p.FileFrame.FaceId).ToList()
                });
            }
            videoFaces = videoFaces.Where(p => p.FaceIds.Any()).ToList();
            return videoFaces;
        }

        public class VideoFaceLocal
        {
            public FileVideo Video;
            public List<Guid?> FaceIds;
        } 

        private void CreateMarkUp(List<MarkUp> markUps, List<FrameAttribute> framesUser, Guid applicationUserId, ElasticClient log)
        {
            var dialogueCreationList = new List<DialogueCreationRun>();
            var dialogueVideoAssembleList = new List<DialogueVideoAssembleRun>();
            int markUpCount;
            if (markUps != null)
            {
                var lastTime = markUps.Max(p =>p.EndTime);
                if (lastTime.Date < DateTime.Now.Date)
                {
                    framesUser
                        .Where(p => p.FileFrame.Time <= markUps.Last().EndTime)
                        .ToList()
                        .ForEach(p => p.FileFrame.StatusNNId = 7);
                    markUpCount = markUps.Count();
                }
                else
                {
                    if (markUps.Count() >= 2 )
                    {
                        framesUser
                            .Where(p => p.FileFrame.Time <= markUps[markUps.Count() - 2].EndTime)
                            .ToList()
                            .ForEach(p => p.FileFrame.StatusNNId = 7);
                    }
                    markUpCount = markUps.Count() - 1;
                }
                var dialogues = new List<Dialogue>();
                for (int i = 0; i < markUpCount; i++)
                {
                    log.Info($"Processing markUp {markUps[i].BegTime}, {markUps[i].EndTime}");
                    if (markUps[i] != null)
                    {
                        log.Info($"Processing markup {i}, {markUps[i].BegTime}, {markUps[i].EndTime}");
                        var updatedMarkUps = UpdateMarkUp(markUps[i], log);
                        log.Info($"Result of update - {JsonConvert.SerializeObject(updatedMarkUps)}");
                        foreach (var updatedMarkUp in updatedMarkUps)
                        {   
                            var dialogueId = Guid.NewGuid();
                            var dialogue = _classCreator.CreateDialogueClass(dialogueId, applicationUserId, updatedMarkUp.BegTime, 
                                updatedMarkUp.EndTime, updatedMarkUp.Descriptor);
                            log.Info($"Create dialogue --- {dialogue.BegTime}, {dialogue.EndTime}, {dialogue.DialogueId}");
                            dialogues.Add(dialogue);
                            _context.Dialogues.Add(dialogue);

                            var markUpNew = _classCreator.CreateMarkUpClass(applicationUserId, updatedMarkUp.BegTime,  updatedMarkUp.EndTime);
                            _context.DialogueMarkups.Add(markUpNew);

                            dialogueVideoAssembleList.Add( new DialogueVideoAssembleRun
                            {
                                ApplicationUserId = applicationUserId,
                                DialogueId = dialogueId,
                                BeginTime = updatedMarkUp.BegTime,
                                EndTime = updatedMarkUp.EndTime
                            });
                            dialogueCreationList.Add(new DialogueCreationRun {
                                ApplicationUserId = applicationUserId,
                                DialogueId = dialogueId,
                                BeginTime = updatedMarkUp.BegTime,
                                EndTime = updatedMarkUp.EndTime,
                                AvatarFileName = updatedMarkUp.FileNames.Select(p => p.FileName).First(),
                                Gender = updatedMarkUp.Gender
                            });
                        }
                    }
                }
                _context.SaveChanges();

                foreach (var dialogueCreation in dialogueCreationList)
                {
                    _publisher.Publish(dialogueCreation);
                }

                foreach (var dialogueAssemble in dialogueVideoAssembleList)
                {
                    _publisher.Publish(dialogueAssemble);
                }

                var personDetection = new PersonDetectionRun{
                    ApplicationUserIds = dialogues.Select(p => p.ApplicationUserId).Distinct().ToList()
                };
                _publisher.Publish(personDetection);
            }
        }

        private List<MarkUp> UpdateMarkUp(MarkUp markUp, ElasticClient log, double persent = 0.7)
        {
            var updatedMarkUp = new List<MarkUp>();
            var dialogueDuration = markUp.EndTime.Subtract(markUp.BegTime).TotalSeconds;
            var videoDuration = markUp.Videos.Sum(p => p.EndTime.Subtract(p.BegTime).TotalSeconds);
            var videos = markUp.Videos;

            if (videoDuration / dialogueDuration > persent)
            {
                updatedMarkUp.Add(markUp);
            }
            else
            {
                int i = 0;
                while (i < videos.Count())
                {
                    log.Info($"Current index is {i}");
                    var takeVideos = 1;
                    var begTime = videos[i].BegTime;
                    var endTime = videos[i].EndTime;
                    var currentVideoDuration = endTime.Subtract(begTime).TotalSeconds;
                    for (int j = i + 1; j < videos.Count(); j++)
                    {
                        currentVideoDuration += videos[j].EndTime.Subtract(videos[j].BegTime).TotalSeconds;
                        var currentDialogueDuration = videos[j].EndTime.Subtract(videos[i].BegTime).TotalSeconds;
                        if (currentVideoDuration /currentDialogueDuration > persent)
                        {
                            takeVideos = j - i + 1;
                        }
                    }
                    var markUpTmp = new MarkUp();
                    markUpTmp.ApplicationUserId = markUp.ApplicationUserId;
                    markUpTmp.FaceId = markUp.FaceId;
                    markUpTmp.BegTime = videos[i].BegTime;
                    markUpTmp.EndTime = videos[i + takeVideos - 1].EndTime;
                    markUpTmp.FileNames = markUp.FileNames.Where(p => p.Time >= videos[i].BegTime && p.Time <= videos[i + takeVideos -1].EndTime).ToList();
                    markUpTmp.Descriptor = markUp.Descriptor;
                    markUpTmp.Gender = markUp.Gender;
                    markUpTmp.Videos = markUp.Videos.Skip(i).Take(takeVideos).ToList();
                    updatedMarkUp.Add(markUpTmp);
                    i += takeVideos;
                    log.Info($"Current dialogue duration -- {currentVideoDuration}, current video duration {videos[i + takeVideos -1].EndTime.Subtract(videos[i].BegTime)}, Index value - {i},  ");
                }
            }
            return updatedMarkUp;
        }

        private List<FrameAttribute> FindAllFaceId(List<FrameAttribute> frameAttribute, int periodFrame, int periodTime)
        {
            for (int i = 0; i< frameAttribute.Count(); i ++)
            {  
                var skipFrames = Math.Max(0, i + 1 - periodFrame);
                var takeFrame = Math.Min(i + 1, periodFrame); 
                var framesCompare = frameAttribute.Skip(skipFrames).Take(takeFrame).ToList();
                var faceId = FindFaceId(framesCompare, periodTime);
                frameAttribute[i].FileFrame.FaceId = faceId;
            }
            return frameAttribute;
        }
        
        private Guid? FindFaceId(List<FrameAttribute> frameAttribute, int periodTime, double threshold = 0.4)
        {
            var frameCompare = frameAttribute.Last();
            if (frameCompare.FileFrame.FaceId != null) 
            {
                return frameCompare.FileFrame.FaceId;
            }

            frameAttribute = frameAttribute.Where(p => p.FileFrame.Time >= frameCompare.FileFrame.Time.AddMinutes(-periodTime)).ToList();
            var index = frameAttribute.Count() - 1;
            var lastFrame = frameAttribute[index];
                
            var faceIds = new List<Guid?>();

            var i = index - 1;
            while (i >= 0)
            {
                var cos = _vectorCalc.Cos(JsonConvert.DeserializeObject<List<double>>(lastFrame.Descriptor),
                    JsonConvert.DeserializeObject<List<double>>(frameAttribute[i].Descriptor));
                if (cos > threshold) 
                    faceIds.Add(frameAttribute[i].FileFrame.FaceId);
                --i;
            }
            return (faceIds.Any()) ?  faceIds.GroupBy(x => x).OrderByDescending(x => x.Count()).First().Key : Guid.NewGuid();
        }
    }
}