using System.Threading.Tasks;
using HBLib.Utils;
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
        private readonly CheckTokenService _service;

        public FillingSatisfactionController(INotificationHandler handler, CheckTokenService service)
        {
            _handler = handler;
            _service = service;
        }

        [HttpPost]
        [SwaggerOperation(Description = "Calculate dialogue satisfaction score")]
        public async Task FillingSatisfactionRun([FromBody] FillingSatisfactionRun message)
        {
            _service.CheckIsUserAdmin();
            _handler.EventRaised(message);
        }
    }
}