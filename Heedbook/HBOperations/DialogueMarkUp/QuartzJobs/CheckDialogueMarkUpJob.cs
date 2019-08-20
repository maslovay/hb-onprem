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


namespace DialogueMarkUp.QuartzJobs
{
    public class CheckDialogueMarkUpJob : IJob
    {
        private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        private readonly INotificationPublisher _publisher;
        private readonly ElasticClientFactory _elasticClientFactory;

        public CheckDialogueMarkUpJob(IServiceScopeFactory factory,
            INotificationPublisher publisher,
            ElasticClientFactory elasticClientFactory)
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _publisher = publisher;
            _elasticClientFactory = elasticClientFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            _log.Info("Function started");
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
                    .Select(p => p.First())
                    .ToList();
                _log.Info($"Processing {frameAttributes.Count()}");

                var appUsers = frameAttributes.Select(p => p.FileFrame.ApplicationUserId).Distinct().ToList();
                
                foreach (var applicationUserId in appUsers)
                {
                    var framesUser = frameAttributes
                        .Where(p => p.FileFrame.ApplicationUserId == applicationUserId)
                        .OrderBy(p => p.FileFrame.Time)
                        .ToList();

                    _log.Info($"Application user id is {applicationUserId}, Frames to proceed - {framesUser.Count()}");

                    framesUser = FindAllFaceId(framesUser, periodFrame, periodTime);
                    var markUps = framesUser.GroupBy(p => p.FileFrame.FaceId)
                        .Where(p => p.Count() > 2)
                        .Select(x => new MarkUp {
                            ApplicationUserId = applicationUserId,
                            FaceId = x.Key,
                            BegTime = x.Min(q => q.FileFrame.Time),
                            EndTime = x.Max(q => q.FileFrame.Time),
                            BegFileName = x.Min(q => q.FileFrame.FileName),
                            EndFileName = x.Max(q => q.FileFrame.FileName),
                            Descriptor = x.First().Descriptor,
                            Gender = x.First().Gender
                        })
                        .Where(p => p.EndTime.Subtract(p.BegTime).TotalSeconds > 10)
                        .OrderBy(p => p.EndTime)
                        .ToList();
                    
                    _log.Info($"Creating markup {JsonConvert.SerializeObject(markUps)}");  
                    if (markUps.Any()) 
                        CreateMarkUp(markUps, framesUser, applicationUserId);
                }
                _log.Info("Function DialogueMarkUp finished");                
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception while executing DialogueMarkUp occured {e}");
                throw;
            }
        }

        private void CreateMarkUp(List<MarkUp> markUps, List<FrameAttribute> framesUser, Guid applicationUserId)
        {
            var dialogueCreationList = new List<DialogueCreationRun>();
            var dialogueVideoAssembleList = new List<DialogueVideoAssembleRun>();
            if (markUps.Last().EndTime.Date < DateTime.Now.Date)
            {
                framesUser
                    .Where(p => p.FileFrame.Time <= markUps.Last().EndTime)
                    .ToList()
                    .ForEach(p => p.FileFrame.StatusNNId = 7);
                
                var dialogues = new List<Dialogue>();
                foreach (var markup in markUps)
                {
                    var dialogueId = Guid.NewGuid();
                    var dialogue = new Dialogue
                    {
                        // DialogueId = (Guid) markup.FaceId,
                        DialogueId = dialogueId,
                        ApplicationUserId = applicationUserId,
                        BegTime = markup.BegTime,
                        EndTime = markup.EndTime,
                        PersonFaceDescriptor = markup.Descriptor,
                        CreationTime = DateTime.UtcNow,
                        LanguageId = 1,
                        StatusId = 6,
                        InStatistic = true
                    };
                    dialogues.Add(dialogue);
                    CheckSessionForDialogue(dialogue).Wait();
                    var markUpNew = new DialogueMarkup{
                        DialogueMarkUpId = Guid.NewGuid(),
                        ApplicationUserId = applicationUserId,
                        BegTime = markup.BegTime,
                        BegTimeMarkup = markup.BegTime,
                        EndTime = markup.EndTime,
                        EndTimeMarkup = markup.EndTime,
                        IsDialogue = true,
                        CreationTime = DateTime.UtcNow,
                        StatusId = 7,
                        TeacherId = "NN"
                    };
                    _context.DialogueMarkups.Add(markUpNew);

                    dialogueVideoAssembleList.Add(new DialogueVideoAssembleRun
                    {
                        ApplicationUserId = applicationUserId,
                        DialogueId = dialogueId,
                        BeginTime = markup.BegTime,
                        EndTime = markup.EndTime
                    });

                    dialogueCreationList.Add(new DialogueCreationRun
                    {
                        ApplicationUserId = applicationUserId,
                        DialogueId = dialogueId,
                        BeginTime = markup.BegTime,
                        EndTime = markup.EndTime,
                        AvatarFileName = markup.BegFileName,
                        Gender = markup.Gender
                    });
                }
                _context.Dialogues.AddRange(dialogues);
                _context.SaveChanges();
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
                var dialogues = new List<Dialogue>();
                for (int i = 0; i < markUps.Count() - 1; ++i)
                {
                    var dialogueId = Guid.NewGuid();
                    var dialogue = new Dialogue
                    {
                        // DialogueId = (Guid) markUps[i].FaceId,
                        DialogueId = dialogueId,
                        ApplicationUserId = applicationUserId,
                        BegTime = markUps[i].BegTime,
                        EndTime = markUps[i].EndTime,
                        PersonFaceDescriptor = markUps[i].Descriptor,
                        CreationTime = DateTime.UtcNow,
                        LanguageId = 1,
                        StatusId = 6,
                        InStatistic = true

                    };
                    dialogues.Add(dialogue);
                    CheckSessionForDialogue(dialogue).Wait();
                    var markUpNew = new HBData.Models.DialogueMarkup{
                        DialogueMarkUpId = Guid.NewGuid(),
                        ApplicationUserId = applicationUserId,
                        BegTime = markUps[i].BegTime,
                        BegTimeMarkup = markUps[i].BegTime,
                        EndTime = markUps[i].EndTime,
                        EndTimeMarkup = markUps[i].EndTime,
                        IsDialogue = true,
                        CreationTime = DateTime.UtcNow,
                        StatusId = 7,
                        TeacherId = "NN"
                    };
                    _context.DialogueMarkups.Add(markUpNew);

                    dialogueVideoAssembleList.Add( new DialogueVideoAssembleRun
                    {
                        ApplicationUserId = applicationUserId,
                        // DialogueId = (Guid) markUps[i].FaceId,
                        DialogueId = dialogueId,
                        BeginTime = markUps[i].BegTime,
                        EndTime = markUps[i].EndTime
                    });
                    dialogueCreationList.Add(new DialogueCreationRun {
                       ApplicationUserId = applicationUserId,
                        // DialogueId = (Guid) markUps[i].FaceId,
                        DialogueId = dialogueId,
                        BeginTime = markUps[i].BegTime,
                        EndTime = markUps[i].EndTime,
                        AvatarFileName = markUps[i].BegFileName,
                        Gender = markUps[i].Gender
                    });
                }
                _context.Dialogues.AddRange(dialogues);
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

        private List<FrameAttribute> FindAllFaceId(List<FrameAttribute> frameAttribute, int periodFrame, int periodTime)
        {
            for (int i = 0; i< frameAttribute.Count(); i ++)
            {  
                var skipFrames = Math.Max(0, i + 1 - periodFrame);
                var takeFrame = Math.Min(i + 1, periodFrame); 
                var framesCompare = frameAttribute.Skip(skipFrames).Take(takeFrame).ToList();
                var faceId = FindFaceId(framesCompare, periodTime);
                // System.Console.WriteLine($"Index ---- {i}, Face id -- {faceId}, Time - {frameAttribute[i].FileFrame.Time}, FileName - {frameAttribute[i].FileFrame.FileName}");
                frameAttribute[i].FileFrame.FaceId = faceId;
            }
            return frameAttribute;
        }
        
        private Guid? FindFaceId(List<FrameAttribute> frameAttribute, int periodTime, double threshold = 0.4)
        {
            var frameCompare = frameAttribute.Last();
            if (frameCompare.FileFrame.FaceId != null) 
            {
                System.Console.WriteLine("Face id exist");
                return frameCompare.FileFrame.FaceId;
            }

            frameAttribute = frameAttribute.Where(p => p.FileFrame.Time >= frameCompare.FileFrame.Time.AddMinutes(-periodTime)).ToList();
            var index = frameAttribute.Count() - 1;
            var lastFrame = frameAttribute[index];
                
            var faceIds = new List<Guid?>();

            var i = index - 1;
            while (i >= 0)
            {
                var cos = Cos(JsonConvert.DeserializeObject<List<double>>(lastFrame.Descriptor),
                    JsonConvert.DeserializeObject<List<double>>(frameAttribute[i].Descriptor));
                //    System.Console.WriteLine($"{cos}, {i}");
                if (cos > threshold) //return frameAttribute[i].FileFrame.FaceId;
                    faceIds.Add(frameAttribute[i].FileFrame.FaceId);
                --i;
            }
            return (faceIds.Any()) ?  faceIds.GroupBy(x => x).OrderByDescending(x => x.Count()).First().Key : Guid.NewGuid();
        }

        private double VectorNorm(List<double> vector)
        {
            return Math.Sqrt(vector.Sum(p => Math.Pow(p, 2) ));
        }

        private double? VectorMult(List<double> vector1, List<double> vector2)
        {
            if (vector1.Count() != vector2.Count()) return null;
            var result = 0.0;
            for (int i =0; i < vector1.Count(); i++)
            {   
                result += vector1[i] * vector2[i];
            }
            return result;
        }

        private double? Cos(List<double> vector1, List<double> vector2)
        {
            return VectorMult(vector1, vector2) / VectorNorm(vector1) / VectorNorm(vector2);
        }

        private async Task CheckSessionForDialogue(Dialogue dialogue)
        {
            var applicationUserId = dialogue.ApplicationUserId;
            
            var applicationUser = _context.ApplicationUsers.FirstOrDefault(p => p.Id == applicationUserId);
            var intersectSession = _context.Sessions.Where(p => p.ApplicationUserId == applicationUserId
                    && (p.StatusId == 6 || p.StatusId == 7)
                    && ((p.BegTime <= dialogue.BegTime
                            && p.EndTime > dialogue.BegTime
                            && p.EndTime < dialogue.EndTime) 
                        || (p.BegTime < dialogue.EndTime
                            && p.BegTime > dialogue.BegTime
                            && p.EndTime >= dialogue.EndTime)
                        || (p.BegTime >= dialogue.BegTime
                            && p.EndTime <= dialogue.EndTime)
                        || (p.BegTime < dialogue.BegTime
                            && p.EndTime > dialogue.EndTime)))
                .ToList();
            
            if(dialogue is null)
            {
                _log.Info($"CheckSessionForDialogue: dialogue is null, applicationUserId: {applicationUserId}");
                return;
            }
            if (!intersectSession.Any())
            {
                var curTime = DateTime.UtcNow;
                var oldTime = DateTime.UtcNow.AddDays(-3);
                var lastSession = _context.Sessions
                        .Where(p => p.ApplicationUserId == applicationUserId 
                            && p.BegTime >= oldTime 
                            && p.EndTime <= dialogue.BegTime)
                        .OrderByDescending(p => p.BegTime)
                        .ToList()
                        .FirstOrDefault();
                if(lastSession != null && lastSession.StatusId == 6)
                {
                    lastSession.EndTime = dialogue.EndTime;
                }
                else
                {                    
                    _context.Sessions.Add( new Session
                    {
                        SessionId = Guid.NewGuid(),
                        ApplicationUserId = applicationUserId,
                        ApplicationUser = applicationUser,
                        BegTime = dialogue.BegTime,
                        EndTime = dialogue.EndTime,
                        StatusId = 7
                    });
                }
                await _context.SaveChangesAsync();
                return;
            } 

            var dialogueBeginSession = intersectSession.FirstOrDefault(p => p.BegTime <= dialogue.BegTime
                    && p.EndTime > dialogue.BegTime);
            var dialogueEndSession = intersectSession.FirstOrDefault(p => p.BegTime < dialogue.EndTime
                    && p.EndTime >= dialogue.EndTime);      

            if(dialogueBeginSession == null && dialogueEndSession == null)
            {
                var insideSessions = intersectSession.Where(p => p.BegTime > dialogue.BegTime
                    && p.EndTime < dialogue.EndTime).OrderBy(p => p.BegTime);
                if(insideSessions.Any())
                {
                    var lastInsideSession = insideSessions.LastOrDefault();
                    if(lastInsideSession.StatusId == 6)
                    {
                        lastInsideSession.BegTime = dialogue.BegTime;
                        lastInsideSession.EndTime = dialogue.EndTime;
                        foreach(var s in insideSessions.Where(p => p!=lastInsideSession))
                        {
                            s.StatusId = 8;
                        }
                    }
                    else
                    {
                        _context.Sessions.Add( new Session
                        {
                            SessionId = Guid.NewGuid(),
                            ApplicationUserId = applicationUserId,
                            ApplicationUser = applicationUser,
                            BegTime = dialogue.BegTime,
                            EndTime = dialogue.EndTime,
                            StatusId = 7
                        });  
                        foreach(var s in insideSessions)
                        {
                            s.StatusId = 8;
                        } 
                    }                 
                }
            }
            else if(dialogueBeginSession != null 
                && dialogueEndSession == null)
            {
                var insideSessions = intersectSession.Where(p => p.BegTime >= dialogueBeginSession.EndTime && p.EndTime < dialogue.EndTime)
                    .OrderBy(p => p.BegTime).ToList();
                
                if(insideSessions.Any())
                {
                    var lastInsideSession = insideSessions.LastOrDefault();
                    if(lastInsideSession.StatusId == 6)
                    {                        
                        dialogueBeginSession.EndTime = dialogue.BegTime;
                        lastInsideSession.BegTime = dialogue.BegTime;
                        lastInsideSession.EndTime = dialogue.EndTime;
                        foreach(var s in insideSessions.Where(p => p!=lastInsideSession))
                        {
                            s.StatusId = 8;
                        }
                    }
                    else
                    {
                        dialogueBeginSession.EndTime = dialogue.EndTime;                        
                        foreach(var s in insideSessions)
                        {
                            s.StatusId = 8;
                        }
                    }                    
                }
                else
                {
                    dialogueBeginSession.EndTime = dialogue.EndTime;
                }
            }  
            else if(dialogueBeginSession == null 
                && dialogueEndSession != null)
            {
                var insideSessions = intersectSession.Where(p => p.BegTime > dialogue.BegTime && p.EndTime <= dialogueEndSession.BegTime)
                    .OrderBy(p => p.BegTime).ToList();
                
                if(insideSessions.Any())
                {
                    dialogueEndSession.BegTime = dialogue.BegTime;       
                    foreach(var s in insideSessions)
                    {
                        s.StatusId = 8;
                    }             
                }
                else
                {
                    dialogueEndSession.BegTime = dialogue.BegTime;
                }
            }          
            else if(dialogueBeginSession != null 
                && dialogueEndSession != null 
                && dialogueBeginSession.SessionId != dialogueEndSession.SessionId)
            {
                var insideSession = intersectSession.Where(p => p.BegTime >= dialogueBeginSession.EndTime 
                        && p.EndTime <= dialogueEndSession.BegTime)
                    .ToList();
                if(insideSession.Any())
                {
                    foreach(var s in insideSession)
                    {
                        s.StatusId = 8;
                    }
                }                
                dialogueEndSession.BegTime = dialogueBeginSession.BegTime;
                dialogueBeginSession.StatusId = 8;                
            }            
            await _context.SaveChangesAsync();            
        }
    }
}