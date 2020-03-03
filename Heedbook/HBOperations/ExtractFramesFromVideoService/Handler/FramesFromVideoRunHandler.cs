using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace ExtractFramesFromVideo.Handler
{
    public class FramesFromVideoRunHandler : IIntegrationEventHandler<FramesFromVideoRun>
    {
        private readonly FramesFromVideo _framesFromVideo;

        public FramesFromVideoRunHandler(FramesFromVideo framesFromVideo)
        {
            _framesFromVideo = framesFromVideo;
        }

        public async Task Handle(FramesFromVideoRun @event)
        {
            await _framesFromVideo.Run(@event.Path);
        }
    }
}