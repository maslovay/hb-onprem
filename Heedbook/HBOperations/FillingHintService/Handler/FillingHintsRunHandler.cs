using System.IO;
using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace FillingHintService.Handler
{
    public class FillingHintsRunHandler: IIntegrationEventHandler<FillingHintsRun>
    {
        private readonly FillingHints _fillingHints;
        
        public FillingHintsRunHandler(FillingHints fillingHints)
        {
            _fillingHints = fillingHints;
        }
        
        public async Task Handle(FillingHintsRun @event)
        {
            await _fillingHints.Run(@event.DialogueId);
        }
    }
}