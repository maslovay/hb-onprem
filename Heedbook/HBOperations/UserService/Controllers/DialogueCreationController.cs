using System;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class DialogueCreationController : Controller
    {
        private readonly IGenericRepository _genericRepository;
        private readonly INotificationPublisher _publisher;
        private readonly CheckTokenService _service;

        public DialogueCreationController(
            INotificationPublisher publisher,
            IGenericRepository genericRepository, 
            CheckTokenService service)
        {
            _publisher = publisher;
            _genericRepository = genericRepository;
            _service = service;
        }

        [HttpPost("dialogueCreation")]
        [SwaggerOperation(Description = "Dialogue creation. Merge videos and frames in one video.")]
        public async Task<IActionResult> DialogueCreation([FromBody] DialogueCreationRun message)
        {
            if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            var languageId = _genericRepository.GetWithInclude<ApplicationUser>(p =>
                                                        p.Id == message.ApplicationUserId,
                                                    link => link.Company)
                                               .First().Company.LanguageId;
            Console.WriteLine(languageId);
            var dialogue = new Dialogue
            {
                DialogueId = message.DialogueId,
                ApplicationUserId = message.ApplicationUserId,
                DeviceId = message.DeviceId,
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
            Console.WriteLine("finished");
            return Ok();
        }
        
        
        [HttpPut("changeInStatistic")]
        [SwaggerOperation(Description = "Changes InStatistic field for a dialog.")]
        public async Task<IActionResult> ChangeInStatistic(Guid dialogueId, bool inStatistic)
        {
            if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            var dialog = _genericRepository.Get<Dialogue>().FirstOrDefault(d => d.DialogueId == dialogueId);

            if (dialog == default(Dialogue))
                throw new Exception($"Can't find a dialog with ID = {dialogueId}!");

            dialog.InStatistic = inStatistic;

            _genericRepository.Update(dialog);
            await _genericRepository.SaveAsync();
            return Ok();
        }
    }
}