using System;
using System.Collections.Generic;
using System.Linq;
using DialogueCreatorScheduler.Models;
using HBData.Models;
using Newtonsoft.Json;

namespace DialogueCreatorScheduler.Services
{
    public class DialogueCreatorService
    {
        private readonly PersonDetectionService _det;

        public DialogueCreatorService(PersonDetectionService det)
        {
            _det = det;
        }

        public List<Dialogue> Dialogues(List<FaceInterval> intervals, ref List<FileFrame> frames, List<Client> clients)
        {
            var dialogues = new List<Dialogue>();
            DateTime updateTime = DateTime.UtcNow.AddDays(-100);
            for (int i = 0; i < intervals.Count(); i++)
            {
                System.Console.WriteLine($"Processing interval {i}");
                if (i < intervals.Count() - 2)
                {
                    System.Console.WriteLine("Case 1");
                    var frameExample = frames.Where(p => p.FrameAttribute.Any() && p.FaceId == intervals[i].FaceId ).FirstOrDefault();
                    if (frameExample != null)
                    {
                        var clientId = _det.FindId(frameExample, clients);

                        System.Console.WriteLine($"Client id is {clientId}");
                        
                        var curFrame = frames.Where(p => p.Time >= intervals[i].BegTime && p.Time <= intervals[i].BegTime).FirstOrDefault();
                        if (curFrame != null)
                        {
                            dialogues.Add(new Dialogue{
                                DialogueId = Guid.NewGuid(),
                                BegTime = intervals[i].BegTime,
                                EndTime = intervals[i].EndTime,
                                CreationTime = DateTime.UtcNow,
                                DeviceId = curFrame.DeviceId,
                                ApplicationUserId = curFrame.ApplicationUserId,
                                StatusId = 6,
                                LanguageId = 1,
                                InStatistic = true,
                                ClientId = clientId,
                                Comment = frameExample.FrameAttribute.FirstOrDefault().Gender
                            });
                            updateTime = intervals[i].EndTime;
                        }
                    }
                }
                else
                {
                    System.Console.WriteLine($"Case 2, End time = {intervals[i].EndTime}, curtime - 3 hour = { DateTime.UtcNow.AddHours(-3)}");
                    if (intervals[i].EndTime <= DateTime.UtcNow.AddHours(-3))
                    {
                        var frameExample = frames.Where(p => p.FrameAttribute.Any() && p.FaceId == intervals[i].FaceId ).FirstOrDefault();
                        System.Console.WriteLine($"Frame example {JsonConvert.SerializeObject(frameExample)}");
                        if (frameExample != null)
                        {
                            var clientId = _det.FindId(frameExample, clients);
                            var curFrame = frames.Where(p => p.Time >= intervals[i].BegTime && p.Time <= intervals[i].BegTime).FirstOrDefault();
                            System.Console.WriteLine($"Current frame example {JsonConvert.SerializeObject(curFrame)}");
                            if (curFrame != null)
                            {
                                dialogues.Add(new Dialogue{
                                    DialogueId = Guid.NewGuid(),
                                    BegTime = intervals[i].BegTime,
                                    EndTime = intervals[i].EndTime,
                                    CreationTime = DateTime.UtcNow,
                                    DeviceId = curFrame.DeviceId,
                                    ApplicationUserId = curFrame.ApplicationUserId,
                                    StatusId = 6,
                                    LanguageId = 1,
                                    InStatistic = true,
                                    ClientId = clientId
                                });
                                updateTime = intervals[i].EndTime;
                            }
                        }
                    } 
                }
            }

            frames.Where(p => p.Time <= updateTime)
                .ToList()
                .ForEach(p => p.StatusNNId=7);

            System.Console.WriteLine(JsonConvert.SerializeObject(dialogues));

            return dialogues;
        }
    }
}