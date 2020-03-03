using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class VideoToSoundController : Controller
    {
        private readonly INotificationHandler _handler;

        public VideoToSoundController(INotificationHandler handler)
        {
            _handler = handler;
        }

        [HttpPost]
        [SwaggerOperation(Description = "Extract audio from video")]
        public void VideoToSound([FromBody] VideoToSoundRun message)
        {
            _handler.EventRaised(message);
        }
    }
}