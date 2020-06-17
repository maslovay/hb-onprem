using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace PersonDetectionAkBarsService.Handler
{
    public class PersonDetectionRunHandler : IIntegrationEventHandler<PersonDetectionRun>
    {
        private readonly PersonDetection _personDetection;

        public PersonDetectionRunHandler(PersonDetection personDetection)
        {
            _personDetection = personDetection;
        }

        public async Task Handle(PersonDetectionRun @event)
        {
            await _personDetection.Run(@event);
        }
    }
}