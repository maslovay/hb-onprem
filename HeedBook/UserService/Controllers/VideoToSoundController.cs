using Microsoft.AspNetCore.Mvc;
using Notifications.Base;
using RabbitMqEventBus.Events;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class VideoToSoundController : Controller
    {
        private readonly INotificationHandler _handler;

        public VideoToSoundController(INotificationHandler handler)
        {
            _handler = handler;
        }

        [HttpPost]
        public void VideoToSound([FromBody] VideoToSoundRun message)
        {
            _handler.EventRaised(message);
        }
    }
}
