using System.Collections.Generic;
using System.Linq;
using DialogueCreatorScheduler.Models;
using HBData.Models;
using Newtonsoft.Json;

namespace DialogueCreatorScheduler.Services  
{
    public class FaceIntervalsService
    {
        private readonly DialogueSettings _dialogueSettings;
        public FaceIntervalsService(DialogueSettings dialogueSettings)
        {
            _dialogueSettings = dialogueSettings;
        }
        public List<FaceInterval> CreateFaceIntervals(List<FileFrame> frames)
        {
            System.Console.WriteLine(JsonConvert.SerializeObject(_dialogueSettings));
            var faceIntervals = new List<FaceInterval>();
            if (!frames.Any()) return faceIntervals;
            
            for (int i = 0; i <frames.Count(); i++ )
            {
                if (i == 0)
                {
                    faceIntervals.Add(new FaceInterval{
                        BegTime = frames[i].Time,
                        EndTime = frames[i].Time,
                        FaceId = frames[i].FaceId
                    });
                }
                else
                {
                    if (frames[i].FaceId == frames[i-1].FaceId)
                    {
                        faceIntervals.Last().EndTime = frames[i].Time;
                    }
                    else
                    {
                        faceIntervals.Add(new FaceInterval{
                            BegTime = frames[i].Time,
                            EndTime = frames[i].Time,
                            FaceId = frames[i].FaceId
                        });
                    }
                }
            }
            return faceIntervals;
        }


        public List<FaceInterval> UpdateFaceIntervals(List<FaceInterval> faceIntervals)
        {
            faceIntervals = faceIntervals.OrderBy(p => p.BegTime).ToList();
            var updateInterval = faceIntervals.First();
            // to do: update for
            while(updateInterval.EndTime != faceIntervals.Max(p => p.EndTime))
            {
                var currentFaceIntervals = faceIntervals.Where(p => p.FaceId == updateInterval.FaceId && p.BegTime >= updateInterval.BegTime).ToList();
                if (currentFaceIntervals.Count() == 1)
                {
                    updateInterval = faceIntervals.Where(p => p.EndTime > updateInterval.EndTime)
                        .OrderBy(p => p.BegTime).First();
                }
                else
                {
                    var pause = faceIntervals.Where(
                            p => p.BegTime >= currentFaceIntervals[0].EndTime &&
                            p.EndTime <= currentFaceIntervals[1].BegTime)
                        .ToList();

                    if (currentFaceIntervals[1].BegTime.Subtract(currentFaceIntervals[0].EndTime).TotalSeconds < _dialogueSettings.PauseDuration)
                    {
                        pause.ForEach(p => p.FaceId = updateInterval.FaceId);
                        System.Console.WriteLine("Merged 2");
                        // next
                        System.Console.WriteLine(JsonConvert.SerializeObject(updateInterval));
                        updateInterval = currentFaceIntervals[1];
                        System.Console.WriteLine(JsonConvert.SerializeObject(updateInterval));
                    }
                    else if (currentFaceIntervals[1].BegTime.Subtract(currentFaceIntervals[0].EndTime).TotalSeconds > _dialogueSettings.MaxDialoguePauseDuration)
                    {
                        //create dialogue
                        updateInterval = faceIntervals.Where(p => p.EndTime > updateInterval.EndTime)
                            .OrderBy(p => p.BegTime).First();
                    }
                    else
                    {
                        if (pause.GroupBy(p => p.FaceId).Sum(p => p.Max(q => q.EndTime)
                            .Subtract(p.Min(q =>q.BegTime)).TotalSeconds) < _dialogueSettings.MinDialogueDuration)
                        {
                            pause.ForEach(p => p.FaceId = updateInterval.FaceId);
                            System.Console.WriteLine("Merged 1");
                            System.Console.WriteLine(JsonConvert.SerializeObject(updateInterval));
                            updateInterval = currentFaceIntervals[1];
                            System.Console.WriteLine(JsonConvert.SerializeObject(updateInterval));
                        }
                        else
                        {
                            // Research
                            updateInterval = faceIntervals.Where(p => p.EndTime > updateInterval.EndTime)
                            .OrderBy(p => p.BegTime).First();
                        }
                    }
                }
            }
            return faceIntervals;
        }

        public List<FaceInterval> MergeFaceIntervals(List<FaceInterval> intervals)
        {
            var result = new List<FaceInterval>();
            for (int i = 0; i< intervals.Count(); i++)
            {
                if (i == 0) result.Add(intervals[i]);
                else
                {
                    if (intervals[i].FaceId == intervals[i-1].FaceId)
                    {
                        result.Last().EndTime = intervals[i].EndTime;
                    }
                    else
                    {
                        result.Add(intervals[i]);
                    }
                }
            }

            return result;
        }
    }
}