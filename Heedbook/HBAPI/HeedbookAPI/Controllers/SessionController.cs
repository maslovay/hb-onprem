using System;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using Swashbuckle.AspNetCore.Annotations;
using UserOperations.Models.Session;
using Microsoft.AspNetCore.Authorization;
using UserOperations.Models;
using UserOperations.Utils;

/// <summary>
/// called from devices and by web socket
/// </summary>
namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
 //   [Authorize(AuthenticationSchemes = "Bearer")]
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
        public Response SessionStatus([FromBody] SessionParams data) =>
           _sessionService.SessionStatus(data);

   
        
        [HttpGet("SessionStatus")]
        [SwaggerOperation(Description = "Returns begin time and StatusId for last Session (for device or for user on device). DeviceId required in params. Device or user token required")]
        [SwaggerResponse(400, "Exception occured", typeof(string))]
        [SwaggerResponse(200, "Last session exist")]
        public object SessionStatus([FromQuery] Guid? deviceId, [FromQuery] Guid? applicationUserId) =>
            _sessionService.SessionStatus(deviceId, applicationUserId);
        
        [HttpPost("AlertNotSmile")]
        [SwaggerOperation(Description = "Save \"Client Does not smile\" alert in base")]
        [SwaggerResponse(400, "Exception occured", typeof(string))]
        [SwaggerResponse(200, "Alert saved")]
        public string AlertNotSmile([FromBody] AlertModel alertModel) =>
            _sessionService.AlertNotSmile(alertModel);
    }
}