using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Notifications.Base;
using RabbitMqEventBus.Events;

namespace UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FramesFromVideoController: ControllerBase
    {
        private readonly INotificationHandler _handler;
        
        public FramesFromVideoController(INotificationHandler handler)
        {
            _handler = handler;
        }
        
        [HttpPost]
        public async Task CutVideoToFrames([FromBody] FramesFromVideoMessage message)
        {
            _handler.EventRaised(message);
        }
    }
}