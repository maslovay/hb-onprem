using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace PersonOnlineDetectionService.Handler
{
    public class PersonOnlineDetectionHandler : IIntegrationEventHandler<PersonOnlineDetectionRun>
    {
        private readonly PersonOnlineDetection _personOnlineDetection;

        public PersonOnlineDetectionHandler(PersonOnlineDetection personOnlineDetection)
        {
            _personOnlineDetection = personOnlineDetection;
        }

        public async Task Handle(PersonOnlineDetectionRun @event)
        {
            await _personOnlineDetection.Run(@event);
        }
    }
}