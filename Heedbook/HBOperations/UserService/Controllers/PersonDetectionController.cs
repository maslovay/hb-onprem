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

namespace UserService.Controllers
{
    [Route("user/[controller]")]
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

        [HttpPost]
        [SwaggerOperation(Description = "Calculate dialogue satisfaction score")]
        public async Task PersonDetectionRun([FromBody] PersonDetectionRun message)
        {
            _handler.EventRaised(message);
        }

        [HttpPost]
        [SwaggerOperation(Description = "Calculate dialogue satisfaction score")]
        public async Task PersonDetectionAllUsersRun()
        {
            
            var begTime = DateTime.UtcNow.AddDays(-30);
            var users = _context.Dialogues.Where(p => p.BegTime > begTime)
                .Select(p => p.ApplicationUserId)
                .ToList();
            var message = new PersonDetectionRun();
            message.ApplicationUserIds = users;
            _handler.EventRaised(message);
        }
    }
}