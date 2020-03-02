using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace DialogueStatusCheckerScheduler.Tests.Handlers
{
    public class FillingSatisfactionRunHandler : IIntegrationEventHandler<FillingSatisfactionRun>
    {
        private StubService _serv;
        
        public FillingSatisfactionRunHandler(StubService serv)
        {
            _serv = serv;
        }

        public async Task Handle(FillingSatisfactionRun @event)
        {
            _serv.SetFlag();
        }
    }
}