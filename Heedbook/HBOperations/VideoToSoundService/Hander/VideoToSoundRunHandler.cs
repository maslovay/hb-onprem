using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace VideoToSoundService.Hander
{
    public class VideoToSoundRunHandler : IIntegrationEventHandler<VideoToSoundRun2>
    {
        private readonly VideoToSound _videoToSound;

        public VideoToSoundRunHandler(VideoToSound videoToSound)
        {
            _videoToSound = videoToSound;
        }

        public async Task Handle(VideoToSoundRun2 @event)
        {
            await _videoToSound.Run(@event.Path);
        }
    }
}