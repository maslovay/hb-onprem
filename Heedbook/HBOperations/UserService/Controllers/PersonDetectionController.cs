using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class PersonDetectionController : ControllerBase
    {
        private readonly INotificationHandler _handler;

        public PersonDetectionController(INotificationHandler handler)
        {
            _handler = handler;
        }

        [HttpPost]
        [SwaggerOperation(Description = "Calculate dialogue satisfaction score")]
        public async Task PersonDetectionRun([FromBody] PersonDetectionRun message)
        {
            _handler.EventRaised(message);
        }
    }
}