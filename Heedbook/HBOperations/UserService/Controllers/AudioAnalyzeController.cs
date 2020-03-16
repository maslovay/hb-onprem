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
    public class AudioAnalyzeController : Controller
    {
        private readonly INotificationHandler _handler;
        private readonly CheckTokenService _service;

        public AudioAnalyzeController(INotificationHandler handler, CheckTokenService service)
        {
            _handler = handler;
            _service = service;
        }

        [HttpPost("audio-analyze")]
        [SwaggerOperation(Description = "Speech recognition for audio file in message")]
        public IActionResult AudioAnalyze([FromBody] AudioAnalyzeRun message)
        {
            if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            _handler.EventRaised(message);
            return Ok();
        }

        [HttpPost("tone-analyze")]
        [SwaggerOperation(Description = "Tone analyze for audio file in message")]
        public IActionResult ToneAnalyze([FromBody] ToneAnalyzeRun toneAnalyzeRun)
        {
            if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            _handler.EventRaised(toneAnalyzeRun);
            return Ok();
        }
    }
}