using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBLib.Utils;
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
    public class FaceController : ControllerBase
    {
        private readonly INotificationHandler _handler;
        
        public FaceController(INotificationHandler handler)
        {
            _handler = handler;
        }

        [HttpPost]
        [SwaggerOperation(Description =
            "Send message to queue")]
        public async Task<IActionResult> FaceAnalyzeRun([FromBody] FaceAnalyzeRun message)
        {
            //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            //var message = "dialogueaudios/01d70dc6-ea24-4f16-940a-53308bc1eca3.wav";
            _handler.EventRaised(message);
            return Ok();
        }
    }
}
