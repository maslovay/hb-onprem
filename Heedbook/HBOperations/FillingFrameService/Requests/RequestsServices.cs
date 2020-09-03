using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HBData;
using HBData.Models;
using HBLib.Utils;
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
            var device = _context.Devices
                .Include(p => p.Company)
                .Where(p => p.DeviceId == message.DeviceId)
                .FirstOrDefault();

            return (device != null) ? device.Company.IsExtended : false;
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
                if (fileAvatar == null) fileAvatar = frames.Where(p => p.FrameAttribute.Any() && p.FileExist).FirstOrDefault();
            }
            else
            {
                fileAvatar = frames.Count > 4 ? frames[4] : frames.FirstOrDefault();
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

        public async System.Threading.Tasks.Task AddVisualsAsync(DialogueVisual visuals)
        {
            await _context.DialogueVisuals.AddAsync(visuals);
        }

        public async System.Threading.Tasks.Task AddFramesAsync(List<DialogueFrame> frames)
        {
            await _context.DialogueFrames.AddRangeAsync(frames);
        }

        public async System.Threading.Tasks.Task AddClientProfileAsync(DialogueClientProfile profile)
        {
            await _context.DialogueClientProfiles.AddAsync(profile);
        }

        public async System.Threading.Tasks.Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

         public void AddVisuals(DialogueVisual visuals)
        {
            _context.DialogueVisuals.Add(visuals);
            _context.SaveChanges();
        }

        public void AddFrames(List<DialogueFrame> frames)
        {
            _context.DialogueFrames.AddRange(frames);
            _context.SaveChanges();
        }

        public void AddClientProfile(DialogueClientProfile profile)
        {
            _context.DialogueClientProfiles.Add(profile);
            _context.SaveChanges();
        }

        public void SaveChanges()
        {
            lock(_context)
            {
                _context.SaveChanges();
            }
        }
    }


}
