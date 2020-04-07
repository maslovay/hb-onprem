using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace ClientAzureCheckingService.Handler
{
    public class ClientAzureCheckingRunHandler : IIntegrationEventHandler<ClientAzureCheckingRun>
    {
        private readonly ClientAzureChecking _clientChecking;

        public ClientAzureCheckingRunHandler(ClientAzureChecking clientChecking)
        {
            _clientChecking = clientChecking;
        }

        public async Task Handle(ClientAzureCheckingRun @event)
        {
            await _clientChecking.Run(@event.Path);
        }
    }
}