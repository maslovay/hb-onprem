using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace ToneAnalyzeService.Handler
{
    public class ToneAnalyzeRunHandler : IIntegrationEventHandler<ToneAnalyzeRun>
    {
        private readonly ToneAnalyze _toneAnalyze;

        public ToneAnalyzeRunHandler(ToneAnalyze toneAnalyze)
        {
            _toneAnalyze = toneAnalyze;
        }

        public async Task Handle(ToneAnalyzeRun @event)
        {
            await _toneAnalyze.Run(@event.Path);
        }
    }
}