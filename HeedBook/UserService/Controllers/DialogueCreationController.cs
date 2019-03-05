using System;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using Microsoft.AspNetCore.Mvc;
using Notifications.Base;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;

namespace UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DialogueCreationController : Controller
    {
        private readonly INotificationPublisher _publisher;
        private readonly IGenericRepository _genericRepository;
        public DialogueCreationController(INotificationPublisher publisher,
        IGenericRepository genericRepository)
        {
            _publisher = publisher;
            _genericRepository = genericRepository;
        }
        
        [HttpPost("dialogueCreation")]
        public async Task DialogueCreation([FromBody] DialogueCreationRun message)
        {
            var languageId = _genericRepository.GetWithInclude<ApplicationUser>(p => 
                    p.Id == message.ApplicationUserId,
                    link => link.Company)
                .First().Company.LanguageId;
            System.Console.WriteLine(languageId);
            var dialogue = new Dialogue
            {
                DialogueId = message.DialogueId,
                ApplicationUserId = message.ApplicationUserId,
                BegTime = message.BeginTime,
                EndTime = message.EndTime,
                CreationTime = DateTime.UtcNow,
                LanguageId = languageId
            };
            _genericRepository.Create(dialogue);
            _genericRepository.Save();
            System.Console.WriteLine("start send message to rabbit");
            var dialogueVideoMerge = new DialogueVideoMergeRun
            {
                ApplicationUserId = message.ApplicationUserId,
                DialogueId = message.DialogueId,
                BeginTime = message.BeginTime,
                EndTime = message.EndTime
            };
            _publisher.Publish(dialogueVideoMerge);
            _publisher.Publish(message);
            System.Console.WriteLine("finished");
        }
    }
}