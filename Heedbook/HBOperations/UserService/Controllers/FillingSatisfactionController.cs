using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using Microsoft.AspNetCore.Mvc;
using Notifications.Base;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class FillingSatisfactionController : ControllerBase
    {
        private readonly INotificationHandler _handler;
        private readonly IGenericRepository _repository;
        private readonly INotificationPublisher _publisher;

        public FillingSatisfactionController(INotificationHandler handler, IGenericRepository repository, INotificationPublisher publisher)
        {
            _handler = handler;
            _repository = repository;
            _publisher = publisher;
        }

        [HttpPost("[action]")]
        [SwaggerOperation(Description = "Calculate dialogue satisfaction score")]
        public async Task FillingSatisfactionRun([FromBody] FillingSatisfactionRun message)
        {
            _handler.EventRaised(message);
        }
        [HttpPost("[action]")]
        [SwaggerOperation(Description = "Recalculate dialogue satisfactions for period")]
        public IActionResult FillingSatisfactionForPeriod([FromQuery(Name = "begTime")] string begT, 
            [FromQuery(Name = "endTime")] string endT)
        {
            if(begT is null) return BadRequest("begT is null");
            if(endT is null) return BadRequest("ensT is null");
            var stringFormat = "yyyyMMdd";
            var begTime = DateTime.ParseExact(begT, stringFormat, CultureInfo.InvariantCulture);
            var endTime = DateTime.ParseExact(endT, stringFormat, CultureInfo.InvariantCulture);
            System.Console.WriteLine($"{begTime} - {endTime}");
             var dialogues = _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime.Date >= begTime.Date
                    && p.BegTime.Date <= endTime.Date)
                .ToList();
            System.Console.WriteLine($"dialogues count: {dialogues.Count}");
            foreach(var d in dialogues)
            {          
                System.Console.WriteLine(d.DialogueId);      
                var message = new FillingSatisfactionRun(){DialogueId = d.DialogueId};
                _publisher.Publish(message);
                Thread.Sleep(100);
            }    
            return Ok("All dialogues for this period sent on recalculate satisfaction");        
        }
    }
}