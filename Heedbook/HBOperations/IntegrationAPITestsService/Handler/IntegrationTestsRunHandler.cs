using System.Threading.Tasks;
using IntegrationAPITestsService;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace IntegrationAPITestsService.Handler
{
    public class IntegrationTestsRunHandler : IIntegrationEventHandler<IntegrationTestsRun>
    {
        private readonly IntegrationTests _integrationTests;

        public IntegrationTestsRunHandler(IntegrationTests integrationTests)
        {
            _integrationTests = integrationTests;
        }

        public async Task Handle(IntegrationTestsRun @event)
        {
            System.Console.WriteLine($"event command: {@event.Command}");
            await _integrationTests.Run(@event.Command);
        }
    }
}