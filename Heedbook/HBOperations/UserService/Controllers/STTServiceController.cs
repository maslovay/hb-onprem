using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBLib.Utils;
using HBMLHttpClient;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
   // [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class STTController : ControllerBase
    {
        private readonly INotificationHandler _handler;
        private readonly HbMlHttpClient _client;
        private readonly INotificationPublisher _publisher;
        private readonly CheckTokenService _service;

        public STTController(INotificationHandler handler, HbMlHttpClient client, CheckTokenService service, INotificationPublisher publisher)
        {
            _handler = handler;
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _service = service;
            _publisher = publisher;
        }

        [HttpPost("sttfirst")]
        [SwaggerOperation(Description =
            "Run local stt for dialogue")]
        public async Task<IActionResult> STTRun1([FromBody] STTMessageRun message)
        {
          //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            System.Console.WriteLine($"Sending message {JsonConvert.SerializeObject(message)}");
            _handler.EventRaised(message);
            System.Console.WriteLine("Sended");
            return Ok();
        }

        [HttpPost("sttsecond")]
        [SwaggerOperation(Description =
            "Run local stt for dialogue second variant")]
        public async Task<IActionResult> STTRun2([FromBody] STTMessageRun message)
        {
          //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            System.Console.WriteLine($"Sending message {JsonConvert.SerializeObject(message)}");
            _publisher.Publish(message);
            System.Console.WriteLine("Sended");
            return Ok();
        }

    }
}