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
    [Route("api/[controller]")]
    [ApiController]
    public class FaceController : ControllerBase
    {
        private readonly INotificationHandler _handler;
        
        public FaceController(INotificationHandler handler)
        {
            _handler = handler;
        }
        
        [HttpPost]
        public async Task FaceRecognition([FromBody] FaceRecognitionMessage message)
        {
            var @event = new FaceRecognitionRun
            {
                Data = message
            };
            _handler.EventRaised(@event);
        }

        [HttpGet]
        public async Task FaceRecognition()
        {
            var @event = new FaceRecognitionRun
            {
                Data = new FaceRecognitionMessage
                {
                    ApplicationUserId = Guid.NewGuid(),
                    BlobContainer = String.Empty,
                    BlobName = String.Empty,
                    CreationTime = DateTime.Now,
                    FacesLength = 12,
                    Id = Guid.NewGuid().ToString("N"),
                    IsFacePresent = true,
                    Status = VideoStatus.Active,
                    Time = DateTime.Now
                }
            };
        }
        [HttpPost]
        public async Task FaceAnalyzeRun([FromBody] FaceAnalyzeMessage message)
        {
            _handler.EventRaised(message);
        }
    }
}
