using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace FaceAnalyzeService.Handler
{
    public class FaceAnalyzeMessageHandler: IIntegrationEventHandler<FaceAnalyzeRun>
    {
        private readonly FaceAnalyze _faceAnalyze;

        public FaceAnalyzeMessageHandler(FaceAnalyze faceAnalyze)
        {
            _faceAnalyze = faceAnalyze;
        }
        
        public async Task Handle(FaceAnalyzeRun @event)
        {
            await _faceAnalyze.Run(@event.Path);
        }
    }
}