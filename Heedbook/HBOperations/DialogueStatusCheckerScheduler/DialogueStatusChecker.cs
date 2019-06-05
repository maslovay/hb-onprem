﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using MemoryDbEventBus.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace DialogueStatusCheckerScheduler
{
    public class DialogueStatusChecker
    {
        private readonly ElasticClient _log;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly IGenericRepository _repository;

        public DialogueStatusChecker(IServiceScopeFactory factory,
            INotificationPublisher notificationPublisher,
            ElasticClient log)
        {
            _repository = factory.CreateScope().ServiceProvider.GetRequiredService<IGenericRepository>();
            _notificationPublisher = notificationPublisher;
            _log = log;
        }

        public async Task<EventStatus> Run(Guid dialogueId)
        {
            try
            {
                _log.Info("Function dialogue status checker started.");

                var dialogue = _repository.GetWithInclude<Dialogue>(
                    d => d.DialogueId == dialogueId && d.StatusId == 6,
                    d => d.DialogueFrame,
                    d => d.DialogueAudio,
                    d => d.DialogueInterval,
                    d => d.DialogueVisual,
                    d => d.DialogueClientProfile).FirstOrDefault();
                
                if (dialogue == null)
                    return EventStatus.Fail;

                var dialogueFrame = dialogue.DialogueFrame?.Any() ?? false;
                var dialogueAudio = dialogue.DialogueAudio?.Any() ?? false;
                var dialogueInterval = dialogue.DialogueInterval?.Any() ?? false;
                var dialogueVisual = dialogue.DialogueVisual?.Any() ?? false;
                var dialogueClientProfiles = dialogue.DialogueClientProfile?.Any() ?? false;

                if (dialogueFrame && dialogueAudio && dialogueInterval && dialogueVisual && dialogueClientProfiles)
                {
                    _log.Info($"Everything is Ok. Dialogue id {dialogue.DialogueId}");
                    dialogue.StatusId = 3;
                    _repository.Update(dialogue);
                    var @event = new FillingSatisfactionRun
                    {
                        DialogueId = dialogue.DialogueId
                    };
                    _notificationPublisher.Publish(@event);
                }
                else
                {
                    if ((DateTime.Now - dialogue.CreationTime).Hours < 2) 
                        return EventStatus.InQueue;
                    
                    _log.Error($"Error dialogue. Dialogue id {dialogue.DialogueId}");
                    dialogue.StatusId = 8;
                    _repository.Update(dialogue);
                }

                _repository.Save();

                return EventStatus.Passed;
            }
            catch (Exception e)
            {
                _log.Fatal($"exception occurs {e}");
            }

            return EventStatus.Fail;
        }
    }
}