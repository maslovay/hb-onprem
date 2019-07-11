using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FillingFrameService.Exceptions;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using HBMLHttpClient.Model;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMqEventBus.Events;
using Renci.SshNet.Common;
using HBLib;
using HBData;
using Microsoft.EntityFrameworkCore;

namespace FillingFrameService
{
    public class DialogueCreation
    {
        private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        private readonly SftpClient _sftpClient;
        private readonly ElasticClientFactory _elasticClientFactory;


        public DialogueCreation(IServiceScopeFactory factory,
            SftpClient client,
            ElasticClientFactory elasticClientFactory)
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _sftpClient = client;
            _elasticClientFactory = elasticClientFactory;
        }

        public async Task Run(DialogueCreationRun message)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{DialogueId}");
            _log.SetArgs(message.DialogueId);
            _log.Info("Function started");

            try
            {
                var frames =
                    _context.FileFrames
                        .Include(p => p.FrameAttribute)
                        .Include(p => p.FrameEmotion)
                        .Where(item =>
                            item.ApplicationUserId == message.ApplicationUserId
                            && item.Time >= message.BeginTime
                            && item.Time <= message.EndTime)
                        .ToList();
                
                var emotions = frames.Where(p => p.FrameEmotion.Any())
                    .Select(p => p.FrameEmotion.First())
                    .ToList();

                var attributes = frames.Where(p => p.FrameAttribute.Any())
                    .Select(p => p.FrameAttribute.First())
                    .ToList();

                if (emotions.Any() && attributes.Any())
                {
                    var dialogueFrames = emotions.Select(item => new DialogueFrame
                        {
                            DialogueId = message.DialogueId,
                            AngerShare = item.AngerShare,
                            FearShare = item.FearShare,
                            DisgustShare = item.DisgustShare,
                            ContemptShare = item.ContemptShare,
                            NeutralShare = item.NeutralShare,
                            SadnessShare = item.SadnessShare,
                            SurpriseShare = item.SurpriseShare,
                            HappinessShare = item.HappinessShare,
                            YawShare = item.YawShare,
                            Time = item.FileFrame.Time
                        })
                        .ToList();

                    var genderCount = attributes.Count(item => item.Gender == "Male");
                    _log.Info($"Gender count {genderCount}");

                    var dialogueClientProfile = new DialogueClientProfile
                    {
                        DialogueId = message.DialogueId,
                        Gender = genderCount > 0 ? "male" : "female",
                        Age = attributes.Average(item => item.Age),
                        Avatar = $"{message.DialogueId}.jpg"
                    };

                    var yawShare = emotions.Average(item => item.YawShare);

                    var dialogueVisual = new DialogueVisual
                    {
                        DialogueId = message.DialogueId,
                        AngerShare = emotions.Average(item => item.AngerShare),
                        FearShare = emotions.Average(item => item.FearShare),
                        DisgustShare = emotions.Average(item => item.DisgustShare),
                        ContemptShare = emotions.Average(item => item.ContemptShare),
                        NeutralShare = emotions.Average(item => item.NeutralShare),
                        SadnessShare = emotions.Average(item => item.SadnessShare),
                        SurpriseShare = emotions.Average(item => item.SurpriseShare),
                        HappinessShare = emotions.Average(item => item.HappinessShare),
                        AttentionShare = yawShare >= -10 && yawShare <= 10 ? 100 : 0
                    };

                    var insertTasks = new List<Task>
                    {
                        _context.DialogueVisuals.AddAsync(dialogueVisual),
                        _context.DialogueClientProfiles.AddAsync(dialogueClientProfile),
                        _context.DialogueFrames.AddRangeAsync(dialogueFrames)
                        // _repository.CreateAsync(dialogueVisual),
                        // _repository.CreateAsync(dialogueClientProfile),
                        // _repository.BulkInsertAsync(dialogueFrames)
                    };

                    var attribute = attributes.First();
                    var avatarFileName = string.IsNullOrEmpty(message.AvatarFileName) ? attribute.FileFrame.FileName : message.AvatarFileName;

                    var localPath =
                        await _sftpClient.DownloadFromFtpToLocalDiskAsync("frames/" + avatarFileName);

                    var faceRectangle = JsonConvert.DeserializeObject<FaceRectangle>(attribute.Value);
                    var rectangle = new Rectangle
                    {
                        Height = faceRectangle.Height,
                        Width = faceRectangle.Width,
                        X = faceRectangle.Top,
                        Y = faceRectangle.Left
                    };

                    var stream = FaceDetection.CreateAvatar(localPath, rectangle);
                    stream.Seek(0, SeekOrigin.Begin);
                    await _sftpClient.UploadAsMemoryStreamAsync(stream, "clientavatars/", $"{message.DialogueId}.jpg");
                    stream.Close();
                    await Task.WhenAll(insertTasks);
                    _context.SaveChanges();
                    _log.Info("Function finished");
                }
            }
            catch (SftpPathNotFoundException e)
            {
                _log.Fatal($"exception occured while trying to download file {e}");
            }
            catch (Exception e)
            {
                _log.Info($"exception occured {e}");
                throw new DialogueCreationException(e.Message, e);
            }
        }
    }
}