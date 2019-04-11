using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class FillingHintController : ControllerBase
    {
        private readonly INotificationHandler _handler;
        public FillingHintController(INotificationHandler handler)
        {
            _handler = handler;
        }

        [HttpPost]
        [SwaggerOperation(Description = "Detect hints based on dialogue scores")]
        public async Task FillingHintRun([FromBody] FillingHintsRun message)
        {
            _handler.EventRaised(message);
        }
    }
}