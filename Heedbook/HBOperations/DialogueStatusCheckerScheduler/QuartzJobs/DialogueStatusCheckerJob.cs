using System;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace QuartzExtensions.Jobs
{
    public class DialogueStatusCheckerJob : IJob
    {
        private readonly ElasticClient _log;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly IGenericRepository _repository;

        public DialogueStatusCheckerJob(IServiceScopeFactory factory,
            INotificationPublisher notificationPublisher,
            ElasticClient log)
        {
            _repository = factory.CreateScope().ServiceProvider.GetRequiredService<IGenericRepository>();
            _notificationPublisher = notificationPublisher;
            _log = log;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _log.Info("Function dialogue status checker started.");
                var dialogues = await _repository.FindByConditionAsync<Dialogue>(item => item.StatusId == 6);
                if (!dialogues.Any())
                {
                    _log.Info("No dialogues.");
                    return;
                }

                foreach (var dialogue in dialogues)
                {
                    var dialogueFrame = _repository
                                       .Get<DialogueFrame>().Any(item => item.DialogueId == dialogue.DialogueId);
                    var dialogueAudio = _repository
                                       .Get<DialogueAudio>().Any(item => item.DialogueId == dialogue.DialogueId);
                    var dialogueInterval = _repository
                                          .Get<DialogueInterval>().Any(item => item.DialogueId == dialogue.DialogueId);
                    var dialogueVisual = _repository
                                        .Get<DialogueVisual>().Any(item => item.DialogueId == dialogue.DialogueId);
                    var dialogueClientProfiles = _repository
                                                .Get<DialogueClientProfile>().Any(item =>
                                                     item.DialogueId == dialogue.DialogueId);

                    if (dialogueFrame && dialogueAudio && dialogueInterval && dialogueVisual &&
                        dialogueClientProfiles)
                    {
                        _log.Info($"Everything is Ok. Dialogue id {dialogue.DialogueId}");
                        dialogue.StatusId = 7;
                        _repository.Update(dialogue);
                        var @event = new FillingSatisfactionRun
                        {
                            DialogueId = dialogue.DialogueId
                        };
                        _notificationPublisher.Publish(@event);
                    }
                    else
                    {
                        if ((DateTime.Now - dialogue.CreationTime).Hours < 2) continue;
                        _log.Error($"Error dialogue. Dialogue id {dialogue.DialogueId}");
                        dialogue.StatusId = 8;
                        _repository.Update(dialogue);
                    }
                }

                _repository.Save();
                _log.Info("Function  ended.");
            }
            catch (Exception e)
            {
                _log.Fatal($"exception occurs {e}");
            }
        }
    }
}