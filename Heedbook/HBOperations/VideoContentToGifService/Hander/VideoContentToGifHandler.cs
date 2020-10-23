using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;
using VideoContentToGifService;

namespace VideoToGifService.Hander
{
    public class VideoContentToGifHandler : IIntegrationEventHandler<VideoContentToGifRun>
    {
        private readonly VideoContentToGif _videoToSound;

        public VideoContentToGifHandler(VideoContentToGif videoToSound)
        {
            _videoToSound = videoToSound;
        }

        public async Task Handle(VideoContentToGifRun @event)
        {
            await _videoToSound.Run(@event.Path);
        }
    }
}