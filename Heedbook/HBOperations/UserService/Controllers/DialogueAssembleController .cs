using System;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBData.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class DialogueAssembleController : Controller
    {
        private readonly IGenericRepository _genericRepository;
        private readonly INotificationPublisher _publisher;
        private readonly RecordsContext _context;

        public DialogueAssembleController(INotificationPublisher publisher,
            IGenericRepository genericRepository,
            RecordsContext context)
        {
            _publisher = publisher;
            _genericRepository = genericRepository;
            _context = context;
        }

        [HttpPost("DialogueAssemble")]
        [SwaggerOperation(Description = "Dialogue creation. Assemble videos and frames in one video.")]
        public async Task DialogueAssemble([FromBody] DialogueCreationRun message)
        {
            var user = _context.ApplicationUsers.Include(p=>p.Company)
                .FirstOrDefault(p => p.Id == message.ApplicationUserId);
            int? languageId;
            if (user?.Company == null)
                languageId = null;
            languageId = user.Company.LanguageId;
            
            Console.WriteLine(languageId);
            var dialogue = new Dialogue
            {
                DialogueId = message.DialogueId,
                ApplicationUserId = message.ApplicationUserId,
                BegTime = message.BeginTime,
                EndTime = message.EndTime,
                CreationTime = DateTime.UtcNow,
                LanguageId = languageId,
                StatusId = 6,
                Comment = "Test dialog!!!"
            };
            try
            {
                if (!_context.Dialogues.Any(p => p.DialogueId == dialogue.DialogueId))
                {
                    _context.Dialogues.Add(dialogue);
                    _context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
           
            Console.WriteLine("start send message to rabbit");
            var dialogueVideoMerge = new DialogueVideoAssembleRun
            {
                ApplicationUserId = message.ApplicationUserId,
                DialogueId = message.DialogueId,
                BeginTime = message.BeginTime,
                EndTime = message.EndTime
            };
            _publisher.Publish(dialogueVideoMerge);
            _publisher.Publish(message);
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