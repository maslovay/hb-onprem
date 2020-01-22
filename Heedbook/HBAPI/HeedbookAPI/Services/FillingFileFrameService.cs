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

            var frameWithMaxArea = frames.OrderByDescending(p => p.FaceArea).FirstOrDefault();

            if(frameWithMaxArea.Age == null 
                || frameWithMaxArea.Gender == null 
                || frameWithMaxArea.Yaw == null 
                || frameWithMaxArea.Smile == null || Double.IsNaN((double)frameWithMaxArea.Smile)
                || frameWithMaxArea.DeviceId == null 
                || frameWithMaxArea.Time == null
                || frameWithMaxArea.Descriptor == null)
                throw new Exception("One of the fields of the frame with max Area is empty");

            var fileFrame = new FileFrame
                {
                    FileFrameId = Guid.NewGuid(),
                    ApplicationUserId = frameWithMaxArea?.ApplicationUserId,
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
                    FaceLength = frames.Count,
                    DeviceId = (Guid)frameWithMaxArea.DeviceId
                };
            var frameAttribute = new FrameAttribute
            {
                FrameAttributeId = Guid.NewGuid(),
                FileFrameId = fileFrame.FileFrameId,
                Gender = frameWithMaxArea.Gender,
                Age = (double)frameWithMaxArea.Age,
                Value = "",
                Descriptor = frameWithMaxArea.Descriptor
            };
            var frameEmotions = new FrameEmotion
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
            };
            _repository.Create<FileFrame>(fileFrame);
            _repository.Create<FrameAttribute>(frameAttribute);
            _repository.Create<FrameEmotion>(frameEmotions);
            System.Console.WriteLine(JsonConvert.SerializeObject(fileFrame));
            System.Console.WriteLine(JsonConvert.SerializeObject(frameAttribute));
            System.Console.WriteLine(JsonConvert.SerializeObject(frameEmotions));
            _repository.Save();
            return "success";
        }
    }
}