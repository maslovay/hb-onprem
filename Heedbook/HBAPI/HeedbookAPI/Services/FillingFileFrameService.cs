using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using HBData.Repository;
using UserOperations.Models;
using HBData.Models;

namespace UserOperations.Services
{
    public class FillingFileFrameService
    {
        private readonly IGenericRepository _repository;
        public FillingFileFrameService(IGenericRepository repository)
        {
            _repository = repository;
        }
        public object FillingFileFrame(List<FileFramePostModel> frames)
        {               
            if(frames == null || frames.Count == 0)
                throw new Exception("List of frames is empty");

            var framesWithMaxArea = frames
                .GroupBy(p => p.Time)
                .Select(p => p.OrderByDescending(q => q.FaceArea).First())
                .ToList();
            
            var fileFrames = new List<FileFrame>();
            var frameAttributes = new List<FrameAttribute>();
            var frameEmotions = new List<FrameEmotion>();

            foreach (var frameWithMaxArea  in framesWithMaxArea)
            {

                if(frameWithMaxArea.Age == null 
                    || frameWithMaxArea.Gender == null 
                    || frameWithMaxArea.Yaw == null 
                    || frameWithMaxArea.Smile == null || Double.IsNaN((double)frameWithMaxArea.Smile)
                    || frameWithMaxArea.DeviceId == null 
                    || frameWithMaxArea.Time == null
                    || frameWithMaxArea.Descriptor == null)
                    throw new Exception("One of the fields of the frame with max Area is empty");
                var applicationUserId = frameWithMaxArea?.ApplicationUserId == null ? Guid.Empty : frameWithMaxArea?.ApplicationUserId;
                var fileFrame = new FileFrame
                {
                    FileFrameId = Guid.NewGuid(),
                    ApplicationUserId = frameWithMaxArea?.ApplicationUserId,
                    FileName = $"{applicationUserId}_{frameWithMaxArea?.DeviceId}_{frameWithMaxArea?.Time.ToString("yyyyMMddHHmmss")}.jpg",
                    FileExist = false,
                    FileContainer = "frames",
                    StatusId = 6,
                    StatusNNId = 6,
                    Time = frameWithMaxArea.Time,
                    IsFacePresent = frameWithMaxArea.Age != null 
                        && frameWithMaxArea.Gender != null
                        && frameWithMaxArea.Yaw != null
                        && frameWithMaxArea.Smile != null
                        && frameWithMaxArea.DeviceId != null
                        && frameWithMaxArea.Descriptor != null,
                    FaceLength = 1,
                    DeviceId = (Guid)frameWithMaxArea.DeviceId
                };
                fileFrames.Add(fileFrame);
                frameAttributes.Add(new FrameAttribute
                {
                    FrameAttributeId = Guid.NewGuid(),
                    FileFrameId = fileFrame.FileFrameId,
                    Gender = frameWithMaxArea.Gender,
                    Age = (double)frameWithMaxArea.Age,
                    Value = JsonConvert.SerializeObject(new {
                        Top = 0,
                        Width = Convert.ToInt32(Math.Sqrt((double)frameWithMaxArea.FaceArea)),
                        Height = Convert.ToInt32(Math.Sqrt((double)frameWithMaxArea.FaceArea)),
                        Left =0
                    }),
                    Descriptor = JsonConvert.SerializeObject(frameWithMaxArea.Descriptor)
                });
                frameEmotions.Add(new FrameEmotion
                {
                    FrameEmotionId = Guid.NewGuid(),
                    FileFrameId = fileFrame.FileFrameId,
                    AngerShare = 0,
                    ContemptShare = 0,
                    DisgustShare = 0,
                    HappinessShare = frameWithMaxArea.Smile,
                    NeutralShare = 1.0 - frameWithMaxArea.Smile,
                    SadnessShare = 0,
                    SurpriseShare = 0,
                    FearShare = 0,
                    YawShare = frameWithMaxArea.Yaw
                });
            }
            System.Console.WriteLine("1");
            _repository.CreateRange<FileFrame>(fileFrames);
            _repository.CreateRange<FrameAttribute>(frameAttributes);
            _repository.CreateRange<FrameEmotion>(frameEmotions);
            System.Console.WriteLine(JsonConvert.SerializeObject(fileFrames));
            _repository.Save();
            return "success";
        }
    }
}