using System;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
  //  [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class DialogueAssembleController : Controller
    {
        private readonly IGenericRepository _genericRepository;
        private readonly INotificationPublisher _publisher;
        private readonly CheckTokenService _service;

        public DialogueAssembleController(INotificationPublisher publisher,
            IGenericRepository genericRepository,
            CheckTokenService service)
        {
            _publisher = publisher;
            _genericRepository = genericRepository;
            _service = service;
        }

        [HttpPost("DialogueAssemble")]
        [SwaggerOperation(Description = "Dialogue creation. Assemble videos and frames in one video.")]
        public async Task<IActionResult> DialogueAssemble([FromBody] DialogueCreationRun message)
        {
          //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            var device = _genericRepository.GetWithInclude<Device>(p => p.DeviceId == message.DeviceId, p=>p.Company)
                .FirstOrDefault();
            int? languageId;
            if (device?.Company == null)
                languageId = null;
            languageId = device.Company.LanguageId;
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
                if (!_genericRepository.GetAsQueryable<Dialogue>().Any(p => p.DialogueId == dialogue.DialogueId))
                {
                    _genericRepository.Create<Dialogue>(dialogue);
                    _genericRepository.Save();
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
                EndTime = message.EndTime,
                DeviceId = message.DeviceId
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
          //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
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