using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace FillingFrameService.Handlers
{
    public class DialogueCreationMessageHandler: IIntegrationEventHandler<DialogueCreationMessage>
    {
        private readonly DialogueCreation _dialogueCreation;

        public DialogueCreationMessageHandler(DialogueCreation dialogueCreation)
        {
            _dialogueCreation = dialogueCreation;
        }

        public async Task Handle(DialogueCreationMessage @event)
        {
            await _dialogueCreation.Run(@event);
        }
    }
}