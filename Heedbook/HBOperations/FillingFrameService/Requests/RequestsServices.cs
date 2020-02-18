using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HBData;
using HBData.Models;
using HBMLHttpClient.Model;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RabbitMqEventBus.Events;

namespace  FillingFrameService.Requests
{
    public class RequestsService
    {
        private readonly RecordsContext _context;

        public RequestsService(RecordsContext context)
        {
            _context = context;
        }

        public bool IsExtended(DialogueCreationRun message)
        {
            return _context.Devices
                .Include(p => p.Company)
                .Where(p => p.DeviceId == message.DeviceId)
                .FirstOrDefault().Company.IsExtended;
        }

        public Client Client(Guid? clientId)
        {
            return _context.Clients
                .Where(p => p.ClientId == clientId)
                .FirstOrDefault();
        }

        public List<FileFrame> FileFrames(DialogueCreationRun message)
        {
            return _context.FileFrames
                .Include(p => p.FrameAttribute)
                .Include(p => p.FrameEmotion)
                .Where(item =>
                    item.IsFacePresent == true 
                    && item.DeviceId == message.DeviceId
                    && item.Time >= message.BeginTime
                    && item.Time <= message.EndTime)
                .ToList();
        }

        public FileFrame FindFileAvatar(DialogueCreationRun message, List<FileFrame> frames, bool isExtended)
        {
            FileFrame fileAvatar;
            if ( !string.IsNullOrWhiteSpace(message.AvatarFileName) && isExtended)
            {
                fileAvatar = frames.Where(item => item.FileName == message.AvatarFileName).FirstOrDefault();
                if (fileAvatar == null) fileAvatar = frames.Where(p => p.FrameAttribute.Any()).FirstOrDefault();
            }
            else
            {
                try
                {
                    fileAvatar = frames
                        .Where(p => p.FrameAttribute.FirstOrDefault().Value != null)
                        .OrderByDescending(p => JsonConvert.DeserializeObject<FaceRectangle>(p.FrameAttribute.FirstOrDefault().Value).Height)
                        .FirstOrDefault();
                }
                catch (Exception e)
                {
                    System.Console.WriteLine($"Exeption occured  {e}");
                    fileAvatar = frames.FirstOrDefault();
                }
                // .ForEach(p => p.Value= JsonConvert.DeserializeObject<FaceRectangle>(fileAvatar.FrameAttribute.FirstOrDefault().Value));
            }
            System.Console.WriteLine(JsonConvert.SerializeObject(fileAvatar));
            return fileAvatar;
        }

        public FileVideo FileVideo(DialogueCreationRun message, FileFrame fileAvatar)
        {
            return _context.FileVideos
                .Where(p => p.DeviceId == message.DeviceId &&
                    p.BegTime <= fileAvatar.Time && p.EndTime >= fileAvatar.Time
                    ).FirstOrDefault();
        }
    }


}