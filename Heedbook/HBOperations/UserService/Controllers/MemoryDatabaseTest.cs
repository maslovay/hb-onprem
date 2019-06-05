using System;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using MemoryDbEventBus;
using MemoryDbEventBus.Events;
using Microsoft.AspNetCore.Mvc;
using Notifications.Base;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class MemoryDatabaseTestController : ControllerBase
    {
        private readonly INotificationHandler _handler;
        private readonly IGenericRepository _repository;
        private readonly IMemoryCache _memoryCache;
        private readonly IMemoryDbPublisher _memoryPublisher;
        private readonly INotificationPublisher _notificationPublisher;
        
        public MemoryDatabaseTestController(INotificationHandler handler, IGenericRepository repository, 
            IMemoryCache memoryCache, IMemoryDbPublisher memoryPublisher, INotificationPublisher notificationPublisher)
        {
            _handler = handler;
            _memoryCache = memoryCache;
            _repository = repository;
            _memoryPublisher = memoryPublisher;
            _notificationPublisher = notificationPublisher;
        }

        [HttpGet("CheckMemoryDb/{count}")]
        [SwaggerOperation(Description = "Creates some messages to test redis")]
        public async Task<ObjectResult> CheckMemoryDbRun(int count)
        {
            _memoryCache.Clear();
            if (count <= 0)
                return BadRequest($"Count must be  > 0!");


            for (int i = 0; i < count; ++i)
            {
                var newEvent = new RedisTestEvent();
                newEvent.Id = Guid.NewGuid();
                _memoryPublisher.Publish(newEvent);
            }

            int memCached = _memoryCache.Count();
            _memoryCache.Clear();
            return Ok($"Messages sent: {count} Messages received: {memCached}");
        }
        
        [HttpGet("CreateTestDialogues/{appUserId}/{dateTime}/{count}")]
        [SwaggerOperation(Description = "Creates some dialogs to test redis")]
        public async Task<ObjectResult> CreateTestDialogues(Guid appUserId, DateTime dateTime, int count)
        {
            _memoryCache.Clear();
            if (count <= 0)
                return BadRequest($"Count must be  > 0!");


            for (int i = 0; i < count; ++i)
            {
                var newEvent = new RedisTestEvent();
                newEvent.Id = Guid.NewGuid();

                var newDialog = new DialogueCreationRun()
                {
                    DialogueId = Guid.NewGuid(),
                    ApplicationUserId = appUserId,
                    BeginTime = dateTime.AddMinutes(-30),
                    EndTime = dateTime.AddHours(1)
                };

                CreateDialog(newDialog);
            }
            

            return Ok($"Dialogs creation procedure finished");
        }

        private void CreateDialog(DialogueCreationRun message)
        {
            var languageId = _repository.GetWithInclude<ApplicationUser>(p =>
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
            _repository.Create(dialogue);
            _repository.Save();
            Console.WriteLine("start send message to rabbit");
            var dialogueVideoMerge = new DialogueVideoMergeRun
            {
                ApplicationUserId = message.ApplicationUserId,
                DialogueId = message.DialogueId,
                BeginTime = message.BeginTime,
                EndTime = message.EndTime
            };


            Task.Run(() =>
            {
                _notificationPublisher.Publish(dialogueVideoMerge);
                _notificationPublisher.Publish(message);


                var dialogueCreatedEvent = new DialogueCreatedEvent()
                {
                    Id = message.DialogueId
                };

                _memoryPublisher.Publish(dialogueCreatedEvent);
                Console.WriteLine("finished");
            });
        }

    }
}