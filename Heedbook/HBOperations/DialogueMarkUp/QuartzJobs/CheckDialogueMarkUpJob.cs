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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;


namespace DialogueMarkUp.QuartzJobs
{
    public class CheckDialogueMarkUpJob : IJob
    {
        private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        private readonly INotificationPublisher _publisher;

        public CheckDialogueMarkUpJob(IServiceScopeFactory factory, INotificationPublisher publisher, ElasticClient log)
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _publisher = publisher;
            _log = log;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _log.Info("Function DialogueMarkUp started");
            var periodTime = 5 * 60; 
            var periodFrame = 10;

            try
            {
                var endTime = DateTime.UtcNow.AddMinutes(-30);
                var frames = _context.FrameAttributes
                    .Include(p => p.FileFrame)
                    .Where(p => p.FileFrame.StatusNNId == 6 && p.FileFrame.Time < endTime)
                    .OrderBy(p => p.FileFrame.Time)
                    .GroupBy(p => p.FileFrame.FileName)
                    .Select(p => p.First())
                    .ToList();
                _log.Info($"Processing {frames.Count()}");
                foreach (var applicationUserId in frames.Select(p => p.FileFrame.ApplicationUserId).ToList().Distinct().ToList())
                {
                    var framesUser = frames
                        .Where(p => p.FileFrame.ApplicationUserId == applicationUserId)
                        .OrderBy(p => p.FileFrame.Time)
                        .ToList();

                    // System.Console.WriteLine(JsonConvert.SerializeObject(framesUser.Select(p => p.FileFrame.FileName)));
                    System.Console.WriteLine($"Count -------------------- {framesUser.Count()}");

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
                        })
                        .Where(p => p.EndTime.Subtract(p.BegTime).TotalSeconds > 10)
                        .OrderBy(p => p.EndTime)
                        .ToList();
                    _log.Info($"Creating markup {JsonConvert.SerializeObject(markUps)}");
                    Console.WriteLine($"Creating markup {JsonConvert.SerializeObject(markUps)}");  
                    if (markUps.Any()) CreateMarkUp(markUps, framesUser, applicationUserId);
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
                        CreationTime = DateTime.UtcNow,
                        LanguageId = 1,
                        StatusId = 6
                    };
                    dialogues.Add(dialogue);

                    var markUpNew = new HBData.Models.DialogueMarkup{
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

                    var dialogueVideoMerge = new DialogueVideoAssembleRun
                    {
                        ApplicationUserId = applicationUserId,
                        DialogueId = dialogueId,
                        BeginTime = markup.BegTime,
                        EndTime = markup.EndTime
                    };
                    _log.Info($" Creating dialogue {JsonConvert.SerializeObject(dialogueVideoMerge)}");
                    _publisher.Publish(dialogueVideoMerge);

                    var dialogueCreation = new DialogueCreationRun {
                        ApplicationUserId = applicationUserId,
                        DialogueId = dialogueId,
                        BeginTime = markup.BegTime,
                        EndTime = markup.EndTime
                    };
                    _log.Info($" Filling frames {JsonConvert.SerializeObject(dialogueVideoMerge)}");
                    _publisher.Publish(dialogueCreation);

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
                for (int i = 0; i < markUps.Count() - 1; i++)
                {
                    var dialogueId = Guid.NewGuid();
                    var dialogue = new Dialogue
                    {
                        // DialogueId = (Guid) markUps[i].FaceId,
                        DialogueId = dialogueId,
                        ApplicationUserId = applicationUserId,
                        BegTime = markUps[i].BegTime,
                        EndTime = markUps[i].EndTime,
                        CreationTime = DateTime.UtcNow,
                        LanguageId = 1,
                        StatusId = 6
                    };
                    dialogues.Add(dialogue);

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

                    var dialogueVideoMerge = new DialogueVideoAssembleRun
                    {
                        ApplicationUserId = applicationUserId,
                        // DialogueId = (Guid) markUps[i].FaceId,
                        DialogueId = dialogueId,
                        BeginTime = markUps[i].BegTime,
                        EndTime = markUps[i].EndTime
                    };
                    _log.Info($"Creating dialogue {JsonConvert.SerializeObject(dialogueVideoMerge)}");
                    _publisher.Publish(dialogueVideoMerge);

                    var dialogueCreation = new DialogueCreationRun {
                       ApplicationUserId = applicationUserId,
                        // DialogueId = (Guid) markUps[i].FaceId,
                        DialogueId = dialogueId,
                        BeginTime = markUps[i].BegTime,
                        EndTime = markUps[i].EndTime
                    };
                    _log.Info($" Filling frames {JsonConvert.SerializeObject(dialogueVideoMerge)}");
                    _publisher.Publish(dialogueCreation);

                }
                _context.Dialogues.AddRange(dialogues);
                _context.SaveChanges();
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
            _log.Info($"Face ids - {JsonConvert.SerializeObject(frameAttribute.Select(p => new {FaceId = p.FileFrame.FaceId, FileName = p.FileFrame.FaceId}).ToList())}");
            return frameAttribute;
        }
        private Guid? FindFaceId(List<FrameAttribute> frameAttribute, int periodTime, double treshold = 0.4)
        {
            var frameCompare = frameAttribute.Last();
            if (frameCompare.FileFrame.FaceId != null) 
            {
                System.Console.WriteLine("Face id exist");
                return frameCompare.FileFrame.FaceId;
            }
            else
            {
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
                    if (cos > treshold) //return frameAttribute[i].FileFrame.FaceId;
                    {
                        faceIds.Add(frameAttribute[i].FileFrame.FaceId);
                    }
                    i --;
                }
                return (faceIds.Any()) ?  faceIds.GroupBy(x => x).OrderByDescending(x => x.Count()).First().Key : Guid.NewGuid();
            }
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
    }
}