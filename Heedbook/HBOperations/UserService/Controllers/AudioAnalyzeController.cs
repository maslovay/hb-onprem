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
        public void AudioAnalyze([FromBody] AudioAnalyzeRun message)
        {
            _service.CheckIsUserAdmin();
            _handler.EventRaised(message);
        }

        [HttpPost("tone-analyze")]
        [SwaggerOperation(Description = "Tone analyze for audio file in message")]
        public void ToneAnalyze([FromBody] ToneAnalyzeRun toneAnalyzeRun)
        {
            _service.CheckIsUserAdmin();
            _handler.EventRaised(toneAnalyzeRun);
        }
    }
}