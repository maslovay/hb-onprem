using System.Threading.Tasks;
using IntegrationAPITestsService;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace UnitAPITestsService.Handler
{
    public class UnitAPITestsRunHandler : IIntegrationEventHandler<UnitAPITestsRun>
    {
        private readonly UnitTests _integrationTests;

        public UnitAPITestsRunHandler(UnitTests integrationTests)
        {
            _integrationTests = integrationTests;
        }

        public async Task Handle(UnitAPITestsRun @event)
        {
            System.Console.WriteLine($"UnitAPITestsServise take command: {@event.Command}");
            await _integrationTests.Run(@event.Command);
        }
    }
}