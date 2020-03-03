using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class FramesFromVideoController : ControllerBase
    {
        private readonly INotificationPublisher _publisher;

        public FramesFromVideoController(INotificationPublisher publisher)
        {
            _publisher = publisher;
        }

        [HttpPost]
        [SwaggerOperation(Description = "Extract frames from video each 3 seconds")]
        public async Task CutVideoToFrames([FromBody] FramesFromVideoRun message)
        {
            _publisher.Publish(message);
        }
    }
}