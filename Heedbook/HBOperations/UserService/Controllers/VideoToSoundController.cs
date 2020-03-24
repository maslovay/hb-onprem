using HBLib.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
  //  [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class VideoToSoundController : Controller
    {
        private readonly INotificationHandler _handler;
        private readonly CheckTokenService _service;

        public VideoToSoundController(INotificationHandler handler, CheckTokenService service)
        {
            _handler = handler;
            _service = service;
        }

        [HttpPost]
        [SwaggerOperation(Description = "Extract audio from video")]
        public IActionResult VideoToSound([FromBody] VideoToSoundRun message)
        {
          //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            _handler.EventRaised(message);
            return Ok();
        }
    }
}