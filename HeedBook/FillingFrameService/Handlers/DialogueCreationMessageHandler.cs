using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace FillingFrameService.Handlers
{
    public class DialogueCreationMessageHandler: IIntegrationEventHandler<DialogueCreationRun>
    {
        private readonly DialogueCreation _dialogueCreation;

        public DialogueCreationMessageHandler(DialogueCreation dialogueCreation)
        {
            _dialogueCreation = dialogueCreation;
        }

        public async Task Handle(DialogueCreationRun @event)
        {
            await _dialogueCreation.Run(@event);
        }
    }
}