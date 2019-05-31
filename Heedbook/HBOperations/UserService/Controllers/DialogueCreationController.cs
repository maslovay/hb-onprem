using System;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using MemoryDbEventBus;
using MemoryDbEventBus.Events;
using Microsoft.AspNetCore.Mvc;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class DialogueCreationController : Controller
    {
        private readonly IGenericRepository _genericRepository;
        private readonly INotificationPublisher _publisher;
        private readonly IMemoryDbPublisher _memoryDbPublisher;

        public DialogueCreationController(INotificationPublisher publisher,
            IMemoryDbPublisher memoryPublisher,
            IGenericRepository genericRepository)
        {
            _publisher = publisher;
            _genericRepository = genericRepository;
            _memoryDbPublisher = memoryPublisher;
        }

        [HttpPost("dialogueCreation")]
        [SwaggerOperation(Description = "Dialogue creation. Merge videos and frames in one video.")]
        public async Task DialogueCreation([FromBody] DialogueCreationRun message)
        {
            var languageId = _genericRepository.GetWithInclude<ApplicationUser>(p =>
                                                        p.Id == message.ApplicationUserId,
                                                    link => link.Company)
                                               .First().Company.LanguageId;
            Console.WriteLine(languageId);
            var dialogue = new Dialogue
            {
                DialogueId = message.DialogueId,
                ApplicationUserId = message.ApplicationUserId,
                BegTime = message.BeginTime,
                EndTime = message.EndTime,
                CreationTime = DateTime.UtcNow,
                LanguageId = languageId,
                StatusId = 6
            };
            _genericRepository.Create(dialogue);
            _genericRepository.Save();
            Console.WriteLine("start send message to rabbit");
            var dialogueVideoMerge = new DialogueVideoMergeRun
            {
                ApplicationUserId = message.ApplicationUserId,
                DialogueId = message.DialogueId,
                BeginTime = message.BeginTime,
                EndTime = message.EndTime
            };
            _publisher.Publish(dialogueVideoMerge);
            _publisher.Publish(message);


            var dialogueCreatedEvent = new DialogueCreatedEvent()
            {
                Id = message.DialogueId
            };
            
            _memoryDbPublisher.Publish(dialogueCreatedEvent);
            Console.WriteLine("finished");
        }
        
        
        [HttpPut("changeInStatistic")]
        [SwaggerOperation(Description = "Changes InStatistic field for a dialog.")]
        public async Task ChangeInStatistic(Guid dialogueId, bool inStatistic)
        {
            var dialog = _genericRepository.Get<Dialogue>().FirstOrDefault(d => d.DialogueId == dialogueId);

            if (dialog == default(Dialogue))
                throw new Exception($"Can't find a dialog with ID = {dialogueId}!");

            dialog.InStatistic = inStatistic;

            _genericRepository.Update(dialog);
            await _genericRepository.SaveAsync();
        }
    }
}