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
        // private readonly RecordsContext _context;
        private readonly SftpClient _sftpClient;
        private readonly ElasticClientFactory _elasticClientFactory;
        private readonly FillingFrameServices _filling;
        private readonly RequestsService _requests;


        public DialogueCreation(
            // IServiceScopeFactory factory,
            SftpClient client,
            FillingFrameServices filling,
            RequestsService requests,
            ElasticClientFactory elasticClientFactory)
        {
            // _context = factory.CreateScope().ServiceProvider.GetRequiredService<RecordsContext>();
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

            _log.Info($"Processing message {JsonConvert.SerializeObject(message)}");

            try
            {
                var isExtended = _requests.IsExtended(message);
                var client = _requests.Client(message.ClientId);
                var frames = _requests.FileFrames(message);
                _log.Info($"Total frames is {frames.Count()}");
               
                // var frameVideo = new FileVideo();
                // if (!isExtended)
                // {
                //     frameVideo = _requests.FileVideo(message);
                // }

                var emotions = frames.Where(p => p.FrameEmotion.Any())
                    .Select(p => p.FrameEmotion.First())
                    .ToList();

                var attributes = frames.Where(p => p.FrameAttribute.Any())
                    .Select(p => p.FrameAttribute.First())
                    .ToList();

                if (emotions.Any() && attributes.Any())
                {
                    var fileAvatar = _requests.FindFileAvatar(message, frames, isExtended, _log);
                    _log.Info($"Avatar is {JsonConvert.SerializeObject(fileAvatar)}");
                    // var frameVideo = new FileVideo();
                    // if (!isExtended)
                    // {
                    //     frameVideo = _requests.FileVideo(message, fileAvatar);
                    // }

                    var dialogueFrames = _filling.FillingDialogueFrame(message, emotions);
                    var dialogueClientProfile = _filling.FillingDialogueClientProfile(message, attributes);
                    var dialogueVisual = _filling.FiilingDialogueVisuals(message, emotions);

                    var insertTasks = new List<Task>
                    {
                        _requests.AddFramesAsync(dialogueFrames),
                        _requests.AddVisualsAsync(dialogueVisual),
                        _requests.AddClientProfileAsync(dialogueClientProfile),
                        // _context.DialogueVisuals.AddAsync(dialogueVisual),
                        // _context.DialogueClientProfiles.AddAsync(dialogueClientProfile),
                        // _context.DialogueFrames.AddRangeAsync(dialogueFrames),
                        _filling.FillingAvatarAsync(message, frames, isExtended, fileAvatar, client)
                    };

                    await Task.WhenAll(insertTasks);
                    // _context.SaveChanges();
                    _requests.SaveChanges();
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