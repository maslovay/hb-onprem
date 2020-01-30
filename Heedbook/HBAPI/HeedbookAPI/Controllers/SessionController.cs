using System;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;
using Swashbuckle.AspNetCore.Annotations;
using UserOperations.Models.Session;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        [SwaggerOperation(Description = "This method can open or close session for applicationuser")]
        [SwaggerResponse(400, "Wrong action / Exception occured", typeof(string))]
        [SwaggerResponse(200, "Session succesfuly opened or closed")]
        public Response SessionStatus([FromBody] SessionParams data) =>
            _sessionService.SessionStatus(data);
        
        [HttpGet("SessionStatus")]
        [SwaggerOperation(Description = "Returns begin time and StatusId for last Session")]
        [SwaggerResponse(400, "Exception occured", typeof(string))]
        [SwaggerResponse(200, "Last session exist")]
        public object SessionStatus([FromQuery] Guid applicationUserId) =>
            _sessionService.SessionStatus(applicationUserId);
        
        [HttpPost("AlertNotSmile")]
        [SwaggerOperation(Description = "Save \"Client Does not smile\" alert in base")]
        [SwaggerResponse(400, "Exception occured", typeof(string))]
        [SwaggerResponse(200, "Alert saved")]
        public string AlertNotSmile([FromBody] Guid applicationUserId) =>
            _sessionService.AlertNotSmile(applicationUserId);
    }
}