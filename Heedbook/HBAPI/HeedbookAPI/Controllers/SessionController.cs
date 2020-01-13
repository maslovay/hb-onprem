using System;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using Swashbuckle.AspNetCore.Annotations;
using UserOperations.Models.Session;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ControllerExceptionFilter]
    public class SessionController : Controller
    {
        private readonly SessionService _sessionService;

        public SessionController(
            SessionService sessionService
            )
        {
            _sessionService = sessionService;
        }

        [HttpPost("SessionStatus")]
        [SwaggerOperation(Description = "This method can open-close session for applicationUser or Device. Device token required")]
        [SwaggerResponse(400, "Wrong action / Exception occured", typeof(string))]
        [SwaggerResponse(200, "Session succesfuly opened-closed")]
        public Response SessionStatus([FromBody] SessionParams data, [FromHeader] string Authorization) =>
           _sessionService.SessionStatus(data, Authorization);

   
        
        [HttpGet("SessionStatus")]
        [SwaggerOperation(Description = "Returns begin time and StatusId for last Session (for device or for user on device). DeviceId required in params. Device token required")]
        [SwaggerResponse(400, "Exception occured", typeof(string))]
        [SwaggerResponse(200, "Last session exist")]
        public object SessionStatus([FromQuery] Guid deviceId, [FromQuery] Guid? applicationUserId, [FromHeader] string Authorization) =>
            _sessionService.SessionStatus(deviceId, applicationUserId, Authorization);
        
        [HttpPost("AlertNotSmile")]
        [SwaggerOperation(Description = "Save \"Client Does not smile\" alert in base")]
        [SwaggerResponse(400, "Exception occured", typeof(string))]
        [SwaggerResponse(200, "Alert saved")]
        public string AlertNotSmile([FromBody] Guid applicationUserId) =>
            _sessionService.AlertNotSmile(applicationUserId);
    }
}