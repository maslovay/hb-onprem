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



namespace DialogueMarkUp.QuartzJobs
{
    public class CheckDialogueMarkUpJob : IJob
    {
        // private readonly ElasticClient _log;
        private readonly IGenericRepository _repository;
        private readonly RecordsContext _context;

        public CheckDialogueMarkUpJob(IServiceScopeFactory factory)
        {
            _repository = factory.CreateScope().ServiceProvider.GetRequiredService<IGenericRepository>();
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
        }
        // {
        //     _repository = factory.CreateScope().ServiceProvider.GetRequiredService<IGenericRepository>();
        //     _log = log;
        //    // _context = context;
        // }

        public async Task Execute(IJobExecutionContext context)
        {
            //  List<int> values = new List<int>() {
            //     5, 8, 1, 4, 8, 6, 4, 2, 9, 0, 10, 11
            // };

            // int periodLength = 4;

            // var temp = Enumerable
            //     .Range(0, values.Count - periodLength)
            //     .Select(n => values.Skip(n).Take(periodLength).Average())
            //     .ToList();

            // System.Console.WriteLine($"{JsonConvert.SerializeObject(temp)}");


            // System.Console.WriteLine($"{JsonConvert.SerializeObject(temp)}");


            try
            {
                Console.WriteLine("start");
                var periodTime = 5 * 60; 
                var periodFrame = 10;

                var endTime = DateTime.UtcNow.AddMinutes(-3000);
                var id = Guid.Parse("600cf351-a259-4cba-835d-cd417015ce6d");

                var frames = _context.FrameAttributes
                    .Include(p => p.FileFrame)
                    .Where(p => p.FileFrame.StatusNNId == 6 && p.FileFrame.Time <= endTime && p.FileFrame.ApplicationUserId == id)
                    .OrderBy(p => p.FileFrame.Time)
                    .ToList();

                System.Console.WriteLine($"{frames.Count()}");
                //var applicationUserIds = frames.Select(p => p.FileFrame.ApplicationUserId).ToList();
                var applicationUserIds = new List<Guid>{id};
                System.Console.WriteLine($"{applicationUserIds.Count()}");
                foreach (var applicationUserId in applicationUserIds)
                {
                    var framesUser = frames.Where(p => p.FileFrame.ApplicationUserId == applicationUserId)
                        .OrderBy(p => p.FileFrame.Time)
                        .ToList();

                    System.Console.WriteLine($"Working with application user {applicationUserId} and frames {framesUser.Count()}");
                    for (int i = 0; i< framesUser.Count(); i ++)
                    {  
                        var skipFrames = Math.Max(0, i + 1 - periodFrame);
                        var takeFrame = Math.Min(i + 1, periodFrame); 
                        var framesCompare = framesUser.Skip(skipFrames).Take(takeFrame).ToList();

                        var faceId = FindFaceId(framesCompare, periodTime);
                        framesUser[i].FileFrame.FaceId = faceId;
                        
                        //System.Console.WriteLine($"{faceId}");
                        // System.Console.WriteLine("");
                        // System.Console.WriteLine("");
                    }
                    System.Console.WriteLine("");
                    System.Console.WriteLine("");

                    var xx = framesUser.GroupBy(p => p.FileFrame.FaceId)
                        .Where(p => p.Count() > 2)
                        .Select(x => new {
                            FaceId = x.Key,
                            BegTime = x.Min(q => q.FileFrame.Time),
                            EndTime = x.Max(q => q.FileFrame.Time),
                            BegFileName = x.Min(q => q.FileFrame.FileName),
                            EndFileName = x.Max(q => q.FileFrame.FileName),
                        }).ToList();

                    System.Console.WriteLine($"{JsonConvert.SerializeObject(xx)}");

                }
                



                // for (var i = 0; i < frames.Count(); i++)
                // {
                //     var framesCompare = frames
                //         .Where(p => p.Time <= )
                //     for (int j = Math.Max())
                // }

                
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"{e}");
            }
        }

        private Guid? FindFaceId(List<FrameAttribute> frameAttribute, int periodTime, double treshold = 0.5)
        {
            var frameCompare = frameAttribute.Last();
            frameAttribute = frameAttribute.Where(p => p.FileFrame.Time >= frameCompare.FileFrame.Time.AddMinutes(-periodTime)).ToList();
            
            var index = frameAttribute.Count() - 1;
            var lastFrame = frameAttribute[index];

            var i = index - 1;
            var faceIds = new List<Guid?>();
            while (i >= 0)
            {
                var cos = Cos(JsonConvert.DeserializeObject<List<double>>(lastFrame.Descriptor),
                            JsonConvert.DeserializeObject<List<double>>(frameAttribute[i].Descriptor));
                // System.Console.WriteLine($"{cos}, {i}");
                if (cos > treshold) //return frameAttribute[i].FileFrame.FaceId;
                {
                    faceIds.Add(frameAttribute[i].FileFrame.FaceId);
                }

                i --;
            }
            if (faceIds.Any())
            {
                return faceIds.GroupBy(x => x).OrderByDescending(x => x.Count()).First().Key;
            }
            else
            {
                return Guid.NewGuid();
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