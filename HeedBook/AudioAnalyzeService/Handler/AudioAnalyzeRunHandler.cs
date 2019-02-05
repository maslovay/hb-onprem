using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace AudioAnalyzeService.Handler
{
    public class AudioAnalyzeRunHandler: IIntegrationEventHandler<AudioAnalyzeRun>
    {
        private readonly AudioAnalyze _audioAnalyze;

        public AudioAnalyzeRunHandler(AudioAnalyze audioAnalyze)
        {
            _audioAnalyze = audioAnalyze;
        }
        
        public async Task Handle(AudioAnalyzeRun @event)
        {
            await _audioAnalyze.Run(@event.Path);
        }
    }
}