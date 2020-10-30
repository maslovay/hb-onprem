using HBData;
using HBData.Models;
using HBLib;
using HBLib.Model;
using HBLib.Utils;
using LemmaSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DialoguesRecalculateScheduler.Settings;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;


namespace DialoguesRecalculateScheduler.QuartzJobs
{
    public class DialoguesRecalculateSchedulerJob : IJob
    {
        private ElasticClient _log;
        private RecordsContext _context;
        private readonly IServiceScopeFactory _factory;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        private readonly DialoguesRecalculateSchedulerSettings _settings;
        private readonly INotificationPublisher _notificationPublisher;

        
        public DialoguesRecalculateSchedulerJob(IServiceScopeFactory factory,
            ElasticClient log,
            INotificationPublisher notificationPublisher,
            SftpClient sftpClient,
            SftpSettings sftpSettings,
            DialoguesRecalculateSchedulerSettings settings)
        {
            _factory = factory;
            _log = log;
            _sftpClient = sftpClient;
            _sftpSettings = sftpSettings;
            _notificationPublisher = notificationPublisher;
            _settings = settings;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using (var scope = _factory.CreateScope())
            {
                _log.Info("Dialogues recalculate scheduler started.");
                try
                {
                    _context = scope.ServiceProvider.GetRequiredService<RecordsContext>();

                    var dialogues = _context.Dialogues
                        .Include(p => p.DialogueAudio)
                        .Include(p => p.DialogueVisual)
                        .Include(p => p.DialogueClientProfile)
                        .Include(p => p.DialogueFrame)
                        .Where(d => d.StatusId == 8 
                                    && d.BegTime >= DateTime.Today.AddDays(-_settings.CheckDeepnessInDays)
                                    && !d.Comment.ToLower().Contains("too many holes in dialogue"))
                        .ToArray();

                    var cntr = 0;

                    while (cntr < dialogues.Count())
                    {
                        await CheckRelatedDialogueData(dialogues.Skip(cntr).Take(_settings.DialoguePacketSize));
                        cntr += _settings.DialoguePacketSize;
                        Thread.Sleep(_settings.Pause * 1000);
                    }

                    _context.SaveChanges();
                    _log.Info("Scheduler ended.");
                }
                catch (Exception e)
                {
                    _log.Fatal($"Exception occured {e}");
                }
            }
        }

        public async Task CheckRelatedDialogueData( IEnumerable<Dialogue> dialoguesPacket )
        {
            foreach (var dialogue in dialoguesPacket)
            {
                var dialogueId = dialogue.DialogueId;
                
                _log.SetFormat("{DialogueId}");
                _log.SetArgs(dialogueId);
                try
                {
                    var dialogueVideoFileExist =
                        await _sftpClient.IsFileExistsAsync(
                            $"{_sftpSettings.DestinationPath}dialoguevideos/{dialogueId}.mkv");
                    _log.Info($"Video file exists - {dialogueVideoFileExist}");

                    if (dialogueVideoFileExist)
                    {
                        var dialogueAudioFileExist =
                            await _sftpClient.IsFileExistsAsync(
                                $"{_sftpSettings.DestinationPath}dialogueaudios/{dialogueId}.wav");
                        _log.Info($"Audio file exists - {dialogueAudioFileExist}");
                        if (dialogueAudioFileExist)
                        {
                            var speechResult =
                                _context.FileAudioDialogues.FirstOrDefault(p => p.DialogueId == dialogueId);
                            _log.Info($"Audio analyze result - {speechResult == null}");

                            if (speechResult == null)
                            {
                                var @event = new AudioAnalyzeRun
                                {
                                    Path = $"dialogueaudios/{dialogueId}.wav"
                                };
                                _notificationPublisher.Publish(@event);
                            }

                            _log.Info($"Tone analyze result - {dialogue.DialogueAudio == null}");
                            if (!dialogue.DialogueAudio.Any() || dialogue.DialogueAudio == null)
                            {
                                var @event = new ToneAnalyzeRun
                                {
                                    Path = $"dialogueaudios/{dialogueId}.wav"
                                };
                                _notificationPublisher.Publish(@event);
                            }
                        }
                        else
                        {
                            _log.Info("Starting video to sound");
                            var @event = new VideoToSoundRun
                            {
                                Path = $"dialoguevideos/{dialogueId}.mkv"
                            };
                            _notificationPublisher.Publish(@event);
                        }

                        _log.Info(
                            $"Filling frame result - {(!dialogue.DialogueVisual.Any() || !dialogue.DialogueClientProfile.Any() || !dialogue.DialogueFrame.Any())}");
                        if (!dialogue.DialogueVisual.Any() || !dialogue.DialogueClientProfile.Any() ||
                            !dialogue.DialogueFrame.Any())
                        {
                            _log.Info("Starting FillingFrames");
                            var @event = new DialogueCreationRun
                            {
                                ApplicationUserId = dialogue.ApplicationUserId,
                                DialogueId = dialogue.DialogueId,
                                BeginTime = dialogue.BegTime,
                                EndTime = dialogue.EndTime
                            };
                            _notificationPublisher.Publish(@event);
                        }
                    }
                    else
                    {
                        _log.Info("Starting dialogue video assemble");
                        var @event = new DialogueVideoAssembleRun
                        {
                            ApplicationUserId = dialogue.ApplicationUserId,
                            DialogueId = dialogue.DialogueId,
                            BeginTime = dialogue.BegTime,
                            EndTime = dialogue.EndTime
                        };
                        _notificationPublisher.Publish(@event);
                    }

                    if (dialogue.StatusId != 3)
                    {
                        dialogue.StatusId = 6;
                        dialogue.CreationTime = DateTime.UtcNow;
                        dialogue.Comment = "";
                        _context.SaveChanges();
                    }

                    _log.Info("Function finished");
                }
                catch (Exception e)
                {
                    _log.Fatal($"Exception occured {e}");
                }
            }
        }

    }
}