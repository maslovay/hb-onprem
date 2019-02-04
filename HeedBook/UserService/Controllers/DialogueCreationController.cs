using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Notifications.Base;
using RabbitMqEventBus.Events;

namespace UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DialogueCreationController : Controller
    {
        private readonly INotificationHandler _handler;

        public DialogueCreationController(INotificationHandler handler)
        {
            _handler = handler;
        }

        [HttpPost]
        public async Task DialogueCreation([FromBody] DialogueCreationMessage message)
        {
            _handler.EventRaised(message);
        }
    }
}