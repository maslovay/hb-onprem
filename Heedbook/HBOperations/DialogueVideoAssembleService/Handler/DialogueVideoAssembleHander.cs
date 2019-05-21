using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace DialogueVideoAssembleService.Handler
{
    public class DialogueVideoMergeRunHandler : IIntegrationEventHandler<DialogueVideoMergeRun>
    {
        private readonly DialogueVideoMerge _dialogueCreation;

        public DialogueVideoMergeRunHandler(DialogueVideoMerge dialogueCreation)
        {
            _dialogueCreation = dialogueCreation;
        }

        public async Task Handle(DialogueVideoMergeRun @event)
        {
            await _dialogueCreation.Run(@event);
        }
    }
}