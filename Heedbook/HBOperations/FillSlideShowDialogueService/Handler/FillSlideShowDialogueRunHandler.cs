using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace FillSlideShowDialogueService.Handler
{
    public class FillSlideShowDialogueRunHandler : IIntegrationEventHandler<FillSlideShowDialogueRun>
    {
        private readonly FillSlideShowDialogue _fillSlideShowDialogue;

        public FillSlideShowDialogueRunHandler(FillSlideShowDialogue fillSlideShowDialogue)
        {
            _fillSlideShowDialogue = fillSlideShowDialogue;
        }

        public async Task Handle(FillSlideShowDialogueRun @event)
        {
            await _fillSlideShowDialogue.Run(@event);
        }
    }
}