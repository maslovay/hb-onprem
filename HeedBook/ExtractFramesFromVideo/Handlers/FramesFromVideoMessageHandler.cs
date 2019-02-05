using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace ExtractFramesFromVideo.Handlers
{
    public class FramesFromVideoMessageHandler: IIntegrationEventHandler<FramesFromVideoRun>
    {
        private readonly FramesFromVideo _framesFromVideo;

        public FramesFromVideoMessageHandler(FramesFromVideo framesFromVideo)
        {
            _framesFromVideo = framesFromVideo;
        }
        
        public async Task Handle(FramesFromVideoRun @event)
        {
            await _framesFromVideo.Run(@event.Path);
        }
    }
}