using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace FillingFrameService.Handler
{
    public class DialogueCreationRunHandler : IIntegrationEventHandler<DialogueCreationRun>
    {
        private readonly DialogueCreation _dialogueCreation;

        public DialogueCreationRunHandler(DialogueCreation dialogueCreation)
        {
            _dialogueCreation = dialogueCreation;
        }

        public async Task Handle(DialogueCreationRun @event)
        {
            await _dialogueCreation.Run(@event);
        }
    }
}