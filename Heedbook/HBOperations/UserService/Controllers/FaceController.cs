using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class FaceController : ControllerBase
    {
        private readonly INotificationHandler _handler;

        public FaceController(INotificationHandler handler)
        {
            _handler = handler;
        }

        [HttpPost]
        [SwaggerOperation(Description =
            "Analyze frame. Detect faces and calculate emotions and face attributes such as gender and age")]
        public async Task FaceAnalyzeRun([FromBody] FaceAnalyzeRun message)
        {
            _handler.EventRaised(message);
        }
    }
}