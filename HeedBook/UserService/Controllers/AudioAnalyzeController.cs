using System;
using Microsoft.AspNetCore.Mvc;
using Notifications.Base;
using RabbitMqEventBus.Events;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class AudioAnalyzeController: Controller
    {
        private readonly INotificationHandler _handler;
        public AudioAnalyzeController(INotificationHandler handler)
        {
            _handler = handler;
        }

        [HttpPost("audio-analyze")]
        public void AudioAnalyze([FromBody] AudioAnalyzeRun message)
        {
            _handler.EventRaised(message);
        }

        [HttpPost("tone-analyze")]
        public void ToneAnalyze([FromBody] ToneAnalyzeRun toneAnalyzeRun)
        {
            _handler.EventRaised(toneAnalyzeRun);
        }
    }
}
