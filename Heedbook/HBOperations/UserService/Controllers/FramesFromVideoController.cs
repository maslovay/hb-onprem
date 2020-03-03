using System.Threading.Tasks;
using HBLib.Utils;
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
        private readonly CheckTokenService _service;

        public FramesFromVideoController(INotificationPublisher publisher, CheckTokenService service)
        {
            _publisher = publisher;
            _service = service;
        }

        [HttpPost]
        [SwaggerOperation(Description = "Extract frames from video each 3 seconds")]
        public async Task CutVideoToFrames([FromBody] FramesFromVideoRun message)
        {
            _service.CheckIsUserAdmin();
            _publisher.Publish(message);
        }
    }
}