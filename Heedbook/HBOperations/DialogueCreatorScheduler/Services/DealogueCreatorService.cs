using System;
using System.Collections.Generic;
using System.Linq;
using DialogueCreatorScheduler.Models;
using HBData.Models;

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
                if (i < intervals.Count() - 2)
                {
                    var frameExample = frames.Where(p => p.FrameAttribute.Any() && p.FaceId == intervals[i].FaceId ).First();
                    var clientId = _det.FindId(frameExample, clients);
                    var curFrame = frames.Where(p => p.Time >= intervals[i].BegTime && p.Time <= intervals[i].BegTime).First();
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
                else
                {
                    if (intervals[i].EndTime <= DateTime.UtcNow.AddHours(-3))
                    {
                         var clientId = _det.FindId(frames.Where(p => p.FrameAttribute.Any() && p.FaceId == intervals[i].FaceId ).First(), 
                        clients);
                        var curFrame = frames.Where(p => p.Time >= intervals[i].BegTime && p.Time <= intervals[i].BegTime).First();
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

            frames.Where(p => p.Time <= updateTime)
                .ToList()
                .ForEach(p => p.StatusNNId=6);

            return dialogues;
        }
    }
}