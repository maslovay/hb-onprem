using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using HBData.Repository;
using UserOperations.Models;
using HBData.Models;
using System.Net.Http;
using HBLib;

namespace UserOperations.Services
{
    public class FillingFileFrameService
    {
        private readonly IGenericRepository _repository;
        private readonly HttpClient _client;
        private readonly URLSettings _urlsettings;
        public FillingFileFrameService(IGenericRepository repository,
            URLSettings urlsettings)
        {
            _repository = repository;
            _client = new HttpClient();
            _urlsettings = urlsettings;
        }
        public async System.Threading.Tasks.Task<object> FillingFileFrameAsync(List<FileFramePostModel> frames)
        {
            if (frames.Where(p => p.Age == 0 && p.Gender == null).Any())
            {
                var minTime = frames.Min(p => p.Time);
                var maxTime = frames.Max(p => p.Time);
                var deviceId = frames.FirstOrDefault().DeviceId;
                var videos = (await _repository.FindByConditionAsync<FileVideo>(x => x.DeviceId == deviceId 
                    && x.BegTime <= minTime 
                    && x.EndTime >= maxTime));
                
                if (videos.Any())
                {
                    var values = new Dictionary<string, string>{
                        { "path", $"videos/{videos.FirstOrDefault().FileName}" },
                    };

                    var content = new FormUrlEncodedContent(values);
                    var response = await _client.PostAsync($"{_urlsettings}/user/FramesFromVideo", content);
                    var responseString = await response.Content.ReadAsStringAsync();
                    return "success";
                }
                else
                {
                    throw new Exception("No videos found");
                }
            }
            else
            {               
                if(frames == null || frames.Count == 0)
                    throw new Exception("List of frames is empty");

                var framesWithMaxArea = frames
                    .GroupBy(p => p.Time)
                    .Select(p => p.OrderByDescending(q => q.FaceArea).First())
                    .ToList();
                    
                var device = _repository.GetWithIncludeOne<Device>(p => p.DeviceId == frames.FirstOrDefault().DeviceId, o => o.Company);
                    
                if(device.Company.IsExtended)
                    return null;

                var fileFrames = new List<FileFrame>();
                var frameAttributes = new List<FrameAttribute>();
                var frameEmotions = new List<FrameEmotion>();

                foreach (var frameWithMaxArea  in framesWithMaxArea)
                {
                    if(frameWithMaxArea.Yaw == null 
                        || frameWithMaxArea.Smile == null || Double.IsNaN((double)frameWithMaxArea.Smile)
                        || frameWithMaxArea.DeviceId == null 
                        || frameWithMaxArea.Time == null)
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
                            Top = frameWithMaxArea.Top == null ? 0 : Convert.ToInt16(frameWithMaxArea.Top),
                            Width = frameWithMaxArea.FaceArea == null ? 0 : Convert.ToInt32(Math.Sqrt((double)frameWithMaxArea.FaceArea)),
                            Height = frameWithMaxArea.FaceArea == null ? 0 : Convert.ToInt32(Math.Sqrt((double)frameWithMaxArea.FaceArea)),
                            Left = frameWithMaxArea.Left == null ? 0 : Convert.ToInt16(frameWithMaxArea.Left)
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
            }
            return "success";
        }
    }
}