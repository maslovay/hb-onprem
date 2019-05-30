using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using MemoryCacheService;
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
        private readonly IMemoryCache _memoryCache;
        
        public DialogueStatusChecker(IServiceScopeFactory factory,
            INotificationPublisher notificationPublisher,
            IMemoryCache memoryCache,
            ElasticClient log)
        {
            _repository = factory.CreateScope().ServiceProvider.GetRequiredService<IGenericRepository>();
            _notificationPublisher = notificationPublisher;
            _log = log;
            _memoryCache = memoryCache;
            
            Run();
        }

        private async Task Run()
        {
            try
            {
                _log.Info("Function dialogue status checker started.");
                while (true)
                {
                    var (id, dialogue) = _memoryCache.Dequeue<Dialogue>(x => x.StatusId == 0);
                    if (id == Guid.Empty) continue;

                    var dialogueFrame = _repository.Get<DialogueFrame>()
                        .Any(item => item.DialogueId == dialogue.DialogueId);
                    var dialogueAudio = _repository.Get<DialogueAudio>()
                        .Any(item => item.DialogueId == dialogue.DialogueId);
                    var dialogueInterval = _repository.Get<DialogueInterval>()
                        .Any(item => item.DialogueId == dialogue.DialogueId);
                    var dialogueVisual = _repository.Get<DialogueVisual>()
                        .Any(item => item.DialogueId == dialogue.DialogueId);
                    var dialogueClientProfiles = _repository.Get<DialogueClientProfile>()
                        .Any(item => item.DialogueId == dialogue.DialogueId);

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
                        if ((DateTime.Now - dialogue.CreationTime).Hours < 2) continue;
                        _log.Error($"Error dialogue. Dialogue id {dialogue.DialogueId}");
                        dialogue.StatusId = 8;
                        _repository.Update(dialogue);
                    }
              
                    _repository.Save();
                }
            }
            catch (Exception e)
            {
                _log.Fatal($"exception occurs {e}");
            }
        }
    }
}