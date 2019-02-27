using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Notifications.Base;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FramesFromVideoController: ControllerBase
    {
        private readonly INotificationPublisher _publisher;
        
        public FramesFromVideoController(INotificationPublisher publisher)
        {
            _publisher = publisher;
        }
        
        [HttpPost]
        public async Task CutVideoToFrames([FromBody] FramesFromVideoRun message)
        {
            _publisher.Publish(message);
        }
    }
}