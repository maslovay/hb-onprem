using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;
using HBData;
using HBData.Models;
using HBData.Repository;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using HBLib.Utils;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
   // [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class PersonDetectionController : ControllerBase
    {
        private readonly INotificationHandler _handler;
        private readonly IGenericRepository _repository;
        private readonly CheckTokenService _service;
        public PersonDetectionController(INotificationHandler handler, 
            IGenericRepository repository, 
            CheckTokenService service)
        {
            _handler = handler;
            _repository = repository;
            _service = service;
        }

        [HttpPost("PersonDetectionRun")]
        [SwaggerOperation(Description = "Calculate dialogue satisfaction score")]
        public async Task<IActionResult> PersonDetectionRun([FromBody] PersonDetectionRun message)
        {
          //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            _handler.EventRaised(message);
            return Ok();
        }

        [HttpPost("PersonDetectionAllUsersRun")]
        [SwaggerOperation(Description = "Calculate dialogue satisfaction score")]
        public async Task<IActionResult> PersonDetectionAllDevicesRun()
        {
          //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            var begTime = DateTime.UtcNow.AddDays(-30);
            var devices = _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime > begTime)
                .Select(p => p.DeviceId)
                .ToList();
            var message = new PersonDetectionRun
            {
                DeviceIds = devices
            };
            _handler.EventRaised(message);
            return Ok();
        }
    }
}