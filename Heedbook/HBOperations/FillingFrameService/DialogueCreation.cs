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
using FillingFrameService.Services;
using FillingFrameService.Requests;

namespace FillingFrameService
{
    public class DialogueCreation
    {
        //private readonly ElasticClient _log;
        private readonly RecordsContext _context;
        private readonly SftpClient _sftpClient;
        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly FillingFrameServices _filling;
        private readonly RequestsService _requests;


        public DialogueCreation(IServiceScopeFactory factory,
            SftpClient client,
            FillingFrameServices filling,
            RequestsService requests,
            ElasticClientFactory elasticClientFactory)
        {
            _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
            _sftpClient = client;
            _filling = filling;
            _elasticClientFactory = elasticClientFactory;
            _requests = requests;
        }

        public async Task Run(DialogueCreationRun message)
        {
            var _log = _elasticClientFactory.GetElasticClient();
            _log.SetFormat("{DialogueId}");
            _log.SetArgs(message.DialogueId);
            _log.Info("Function started");

            try
            {
                var isExtended = _requests.IsExtended(message);
                var frames = _requests.FileFrames(message);
                FileVideo frameVideo;
                if (!isExtended)
                {
                    frameVideo = _requests.FileVideo(message);
                }

                var emotions = frames.Where(p => p.FrameEmotion.Any())
                    .Select(p => p.FrameEmotion.First())
                    .ToList();

                var attributes = frames.Where(p => p.FrameAttribute.Any())
                    .Select(p => p.FrameAttribute.First())
                    .ToList();

                if (emotions.Any() && attributes.Any())
                {
                    var dialogueFrames = _filling.FillingDialogueFrame(message, emotions);
                    var dialogueClientProfile = _filling.FillingDialogueClientProfile(message, attributes);
                    var dialogueVisual = _filling.FiilingDialogueVisuals(message, emotions);

                    var insertTasks = new List<Task>
                    {
                        _context.DialogueVisuals.AddAsync(dialogueVisual),
                        _context.DialogueClientProfiles.AddAsync(dialogueClientProfile),
                        _context.DialogueFrames.AddRangeAsync(dialogueFrames)
                    };






                    FrameAttribute attribute;
                    if (!string.IsNullOrWhiteSpace(message.AvatarFileName) )
                    {
                        attribute = frames.Where(item => item.FileName == message.AvatarFileName).Select(p => p.FrameAttribute.FirstOrDefault()).FirstOrDefault();
                        if (attribute == null) attribute = attributes.First();
                    }
                    else
                    {
                        attribute = attributes.First();
                    }

                    _log.Info($"Avatar file name is {attribute.FileFrame.FileName}");
                    var localPath =
                        await _sftpClient.DownloadFromFtpToLocalDiskAsync("frames/" + attribute.FileFrame.FileName);

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
                _log.Fatal($"exception occured {e}");
                throw new DialogueCreationException(e.Message, e);
            }
        }
    }
}