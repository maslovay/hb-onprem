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
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class PersonDetectionController : ControllerBase
    {
        private readonly INotificationHandler _handler;
        private readonly RecordsContext _context;
        private readonly CheckTokenService _service;
        public PersonDetectionController(INotificationHandler handler, RecordsContext context, CheckTokenService service)
        {
            _handler = handler;
            _context = context;
            _service = service;
        }

        [HttpPost("PersonDetectionRun")]
        [SwaggerOperation(Description = "Calculate dialogue satisfaction score")]
        public async Task PersonDetectionRun([FromBody] PersonDetectionRun message)
        {
            _service.CheckIsUserAdmin();
            _handler.EventRaised(message);
        }

        [HttpPost("PersonDetectionAllUsersRun")]
        [SwaggerOperation(Description = "Calculate dialogue satisfaction score")]
        public async Task PersonDetectionAllDevicesRun()
        {
            _service.CheckIsUserAdmin();
            var begTime = DateTime.UtcNow.AddDays(-30);
            var devices = _context.Dialogues.Where(p => p.BegTime > begTime)
                .Select(p => p.DeviceId)
                .ToList();
            var message = new PersonDetectionRun
            {
                DeviceIds = devices
            };
            _handler.EventRaised(message);
        }
    }
}