using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Notifications.Base;
using RabbitMqEventBus.Events;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class FillingSatisfactionController : ControllerBase
    {
        private readonly INotificationHandler _handler;
        public FillingSatisfactionController(INotificationHandler handler)
        {
            _handler = handler;
        }

        [HttpPost]
        public async Task FillingSatisfactionRun([FromBody] FillingSatisfactionRun message)
        {
            _handler.EventRaised(message);
        }
    }
}