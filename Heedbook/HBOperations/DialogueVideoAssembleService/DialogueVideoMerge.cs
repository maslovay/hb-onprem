using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
namespace DialogueVideoAssembleService
{
    public class DialogueVideoMerge
    {
        private const String VideoFolder = "videos";
        private const String FrameFolder = "frames";
        private const String VideoType = "video";
        private const String FrameType = "frame";
        private readonly ElasticClient _log;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly IGenericRepository _repository;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;

        public DialogueVideoMerge(
            INotificationPublisher notificationPublisher,
            IServiceScopeFactory factory,
            SftpClient client,
            SftpSettings sftpSettings,
            ElasticClient log
        )
        {
            _repository = factory.CreateScope().ServiceProvider.GetRequiredService<IGenericRepository>();
            _sftpClient = client;
            _sftpSettings = sftpSettings;
            _log = log;
            _notificationPublisher = notificationPublisher;
        }
        
        public static FileFrame LastFrame(FileVideo video, List<FileFrame> frames)
        {
            return frames.Where(p => p.Time >= video.BegTime && p.Time <= video.EndTime).OrderByDescending(p => p.Time)
                .FirstOrDefault();
        }

        public async Task Run(DialogueVideoMergeRun message)
        {
            _log.SetFormat("{ApplicationUserId}, {DialogueId}");
            _log.SetArgs(message.ApplicationUserId, message.DialogueId);
        }
    }
}