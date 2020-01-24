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

        public FileVideo FileVideo(DialogueCreationRun message)
        {
            var formatString = "yyyyMMddHHmmss";
            var dt = DateTime.ParseExact(message.AvatarFileName.Split('_')[2] ,formatString, CultureInfo.InvariantCulture);
            return _context.FileVideos
                .Where(p => p.DeviceId == message.DeviceId &&
                    p.BegTime <= dt && p.EndTime >= dt
                    ).FirstOrDefault();
        }
    }


}