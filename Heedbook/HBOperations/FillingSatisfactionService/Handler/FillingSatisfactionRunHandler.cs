using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace FillingSatisfactionService.Handler
{
    public class FillingSatisfactionRunHandler: IIntegrationEventHandler<FillingSatisfactionRun>
    {
        private readonly FillingSatisfaction _fillingSatisfaction;

        public FillingSatisfactionRunHandler(FillingSatisfaction fillingSatisfaction)
        {
            _fillingSatisfaction = fillingSatisfaction;
        }
        public async Task Handle(FillingSatisfactionRun @event)
        {
           await _fillingSatisfaction.Run(@event.DialogueId);
        }
    }
}