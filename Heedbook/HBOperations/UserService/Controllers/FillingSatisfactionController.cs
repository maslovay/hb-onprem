using System.Threading.Tasks;
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
    public class FillingSatisfactionController : ControllerBase
    {
        private readonly INotificationHandler _handler;

        public FillingSatisfactionController(INotificationHandler handler)
        {
            _handler = handler;
        }

        [HttpPost]
        [SwaggerOperation(Description = "Calculate dialogue satisfaction score")]
        public async Task FillingSatisfactionRun([FromBody] FillingSatisfactionRun message)
        {
            _handler.EventRaised(message);
        }
    }
}