using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace MessengerReporterService.Handler
{
    public class MessengerMessageRunHandler : IIntegrationEventHandler<MessengerMessageRun>
    {
        private readonly MessengerReporter _messengerReporter;

        public MessengerMessageRunHandler(MessengerReporter messengerReporter)
        {
            _messengerReporter = messengerReporter;
        }

        public async Task Handle(MessengerMessageRun @event)
        {
            await _messengerReporter.Run(@event);
        }
    }
}