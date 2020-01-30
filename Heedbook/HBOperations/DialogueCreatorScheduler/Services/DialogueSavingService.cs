using System.Collections.Generic;
using System.Linq;
using HBData.Models;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace DialogueCreatorScheduler.Service
{
    public class DialogueSavingService
    {
        private readonly INotificationPublisher _publisher; 
        public DialogueSavingService(INotificationPublisher publisher)
        {
            _publisher = publisher;
        }

        public void Publish(List<Dialogue> dialogues)
        {
            var dialogueCreationList = dialogues.Select(p => new DialogueCreationRun{
                    DeviceId = p.DeviceId,
                    ApplicationUserId = p.ApplicationUserId,
                    DialogueId = p.DialogueId,
                    BeginTime = p.BegTime,
                    EndTime = p.EndTime,
                    AvatarFileName = null,
                    Gender = p.Comment
                }).ToList();

            var videoAssembleList = dialogues.Select(p => new DialogueVideoAssembleRun{
                ApplicationUserId = p.ApplicationUserId,
                DeviceId = p.DeviceId,
                DialogueId = p.DialogueId,
                BeginTime = p.BegTime,
                EndTime = p.EndTime
            });

            foreach (var message in dialogueCreationList)
            {
                _publisher.Publish(message);
            }
            foreach (var message in videoAssembleList)
            {
                _publisher.Publish(message);
            }
        }
    }
}