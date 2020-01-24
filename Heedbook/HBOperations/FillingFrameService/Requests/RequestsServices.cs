using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HBData;
using HBData.Models;
using Microsoft.EntityFrameworkCore;
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

        public List<FileFrame> FileFrames(DialogueCreationRun message)
        {
            return _context.FileFrames
                .Include(p => p.FrameAttribute)
                .Include(p => p.FrameEmotion)
                .Where(item =>
                    item.ApplicationUserId == message.ApplicationUserId
                    && item.Time >= message.BeginTime
                    && item.Time <= message.EndTime)
                .ToList();
        }

        public FileFrame FindFileAvatar(DialogueCreationRun message, List<FileFrame> frames)
        {
            FileFrame fileAvatar;
            if ( !string.IsNullOrWhiteSpace(message.AvatarFileName))
            {
                fileAvatar = frames.Where(item => item.FileName == message.AvatarFileName).FirstOrDefault();
                if (fileAvatar == null) fileAvatar = frames.Where(p => p.FrameAttribute.Any()).FirstOrDefault();
            }
            else
            {
                fileAvatar = frames.Where(p => p.FrameAttribute.Any()).FirstOrDefault();
            }
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