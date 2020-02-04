using System.Threading.Tasks;
using IntegrationAPITestsService;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace IntegrationAPITestsService.Handler
{
    public class IntegrationTestsRunHandler : IIntegrationEventHandler<IntegrationAPITestsRun>
    {
        private readonly IntegrationTests _integrationTests;

        public IntegrationTestsRunHandler(IntegrationTests integrationTests)
        {
            _integrationTests = integrationTests;
        }

        public async Task Handle(IntegrationAPITestsRun @event)
        {
            System.Console.WriteLine($"IntegrationAPITestsServise take command: {@event.Command}");
            await _integrationTests.Run(@event.Command);
        }
    }
}