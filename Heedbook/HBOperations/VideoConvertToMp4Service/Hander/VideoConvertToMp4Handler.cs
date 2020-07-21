using System.Threading.Tasks;
using RabbitMqEventBus.Base;
using RabbitMqEventBus.Events;

namespace VideoConvertToMp4.Hander
{
    public class VideoConvertToMp4Handler : IIntegrationEventHandler<VideoConvertToMp4Run>
    {
        private readonly VideoConvertToMp4 _videoConvert;

        public VideoConvertToMp4Handler(VideoConvertToMp4 videoConvert)
        {
            _videoConvert = videoConvert;
        }

        public async Task Handle(VideoConvertToMp4Run @event)
        {
            await _videoConvert.Run(@event.DialogueId);
        }
    }
}