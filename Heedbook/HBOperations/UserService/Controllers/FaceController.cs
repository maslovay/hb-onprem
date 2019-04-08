using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Notifications.Base;
using RabbitMqEventBus.Events;
using RabbitMqEventBus.Models;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class FaceController : ControllerBase
    {
        private readonly INotificationHandler _handler;
        public FaceController(INotificationHandler handler)
        {
            _handler = handler;
        }

        [HttpPost]
        public async Task FaceAnalyzeRun([FromBody] FaceAnalyzeRun message)
        {
            _handler.EventRaised(message);
        }
    }
}
