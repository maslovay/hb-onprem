using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace DialogueVideoAssembleService.Handler
{
    public class DialogueVideoAssembleRunHandler : IIntegrationEventHandler<DialogueVideoAssembleRun>
    {
        private readonly DialogueVideoAssemble _dialogueCreation;

        public DialogueVideoAssembleRunHandler(DialogueVideoAssemble dialogueCreation)
        {
            _dialogueCreation = dialogueCreation;
        }

        public async Task Handle(DialogueVideoAssembleRun @event)
        {
            await _dialogueCreation.Run(@event);
        }
    }
}