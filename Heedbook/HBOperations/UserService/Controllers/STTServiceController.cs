using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBLib.Utils;
using HBMLHttpClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        private readonly CheckTokenService _service;

        public STTController(INotificationHandler handler, HbMlHttpClient client, CheckTokenService service)
        {
            _handler = handler;
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _service = service;
        }

        [HttpPost]
        [SwaggerOperation(Description =
            "Run local stt for dialogue")]
        public async Task<IActionResult> FaceAnalyzeRun([FromBody] STTMessageRun message)
        {
          //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            _handler.EventRaised(message);
            return Ok();
        }

    }
}