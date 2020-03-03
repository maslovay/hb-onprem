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

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class PersonDetectionController : ControllerBase
    {
        private readonly INotificationHandler _handler;
        private readonly RecordsContext _context;
        public PersonDetectionController(INotificationHandler handler, RecordsContext context )
        {
            _handler = handler;
            _context = context;
        }

        [HttpPost("PersonDetectionRun")]
        [SwaggerOperation(Description = "Calculate dialogue satisfaction score")]
        public async Task PersonDetectionRun([FromBody] PersonDetectionRun message)
        {
            _handler.EventRaised(message);
        }

        [HttpPost("PersonDetectionAllUsersRun")]
        [SwaggerOperation(Description = "Calculate dialogue satisfaction score")]
        public async Task PersonDetectionAllDevicesRun()
        {
            
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