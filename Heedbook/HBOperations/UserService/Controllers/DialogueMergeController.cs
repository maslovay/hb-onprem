using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
   // [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class DialogueMergeController : Controller
    {
        private readonly INotificationHandler _handler;
        private readonly CheckTokenService _service;
        // private readonly ElasticClient _log;
        private readonly IGenericRepository _repository;
        public DialogueMergeController(INotificationHandler handler, 
            CheckTokenService service,
            /*, ElasticClient log*/
            IGenericRepository repository)
        {
            _handler = handler;
            _service = service;
            // _log = log;
            _repository = repository;
        }

        [HttpPost]
        [SwaggerOperation(Description = "Method for merge some dialogues with status 3 in the range")]
        public async Task<IActionResult> MergeDialogues([FromQuery] String Email,
            [FromQuery] String DeviceCode,
            [FromQuery] String begTime,
            [FromQuery] String endTime)
        {
            // if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            try
            {  
            // _log.Info("Function Video save info started");
                var dateFormat = "HH:mm:ss dd.MM.yyyy";

                Guid? userId = null;
                if(Email != null)
                    userId = _repository.GetAsQueryable<ApplicationUser>().FirstOrDefault(p => p.Email == Email).Id;
                Guid? deviceId = null;
                if(DeviceCode != null) 
                    deviceId = _repository.GetAsQueryable<Device>().FirstOrDefault(p => p.Code == DeviceCode).DeviceId;

                var timeBeg = DateTime.ParseExact(begTime, dateFormat, CultureInfo.InvariantCulture).AddHours(-3);
                var timeEnd = DateTime.ParseExact(endTime, dateFormat, CultureInfo.InvariantCulture).AddHours(-3);

                if(timeBeg == null || timeEnd == null || (userId == null && deviceId == null))
                    return BadRequest("One of the parameters is invalid!");

                List<Dialogue> dialogues = new List<Dialogue>();
                if(userId != null)
                {
                    dialogues = _repository.GetAsQueryable<Dialogue>()
                    .Include(p => p.Client)
                    .Where(p => p.ApplicationUserId == userId
                        && p.StatusId == 3
                        && p.BegTime >= timeBeg
                        && p.EndTime <= timeEnd)
                    .OrderBy(p => p.BegTime)
                    .ToList();
                }
                else if(deviceId != null)
                {
                    dialogues = _repository.GetAsQueryable<Dialogue>()
                    .Include(p => p.Client)
                    .Where(p => p.DeviceId == deviceId
                        && p.StatusId == 3
                        && p.BegTime >= timeBeg
                        && p.EndTime <= timeEnd)
                    .OrderBy(p => p.BegTime)
                    .ToList();
                }
                else 
                    return BadRequest("userId and deviceId is invalid!");
                    
                if(dialogues == null || dialogues.Count == 0)
                    return BadRequest("No exist dialogues in this range!");

                dialogues.ForEach(p => p.StatusId = 8);

                var newDialogueId = Guid.NewGuid();
                var maxBegTime = MaxTime(timeBeg, dialogues.FirstOrDefault().BegTime);
                var minEndTime = MinTime(timeEnd, dialogues.OrderBy(p => p.EndTime).LastOrDefault().EndTime);
                
                var firstDialogue = dialogues.FirstOrDefault();
                var newDialogue = new Dialogue
                {
                    DialogueId = newDialogueId,
                    ClientId = firstDialogue.ClientId,
                    PersonFaceDescriptor = firstDialogue.PersonFaceDescriptor,
                    CreationTime = DateTime.UtcNow,
		            DeviceId = firstDialogue.DeviceId,
                    BegTime = maxBegTime,
                    EndTime = minEndTime,
                    ApplicationUserId = userId,
                    LanguageId = firstDialogue.LanguageId,
                    StatusId = 6,
                    InStatistic = true
                };
                _repository.Create<Dialogue>(newDialogue);                

                var dialogueVideoAssembleRun = new DialogueVideoAssembleRun
                {
                    ApplicationUserId = userId,
                    DialogueId = newDialogueId,
		            DeviceId = newDialogue.DeviceId,
                    BeginTime = maxBegTime,
                    EndTime = minEndTime
                };
                var dialogueCreationRun = new DialogueCreationRun 
                {
                    ApplicationUserId = userId,
                    DialogueId = newDialogueId,
		            DeviceId = firstDialogue.DeviceId,
                    BeginTime = maxBegTime,
                    EndTime = minEndTime
                };
                _repository.Save();
                _handler.EventRaised(dialogueVideoAssembleRun);
                _handler.EventRaised(dialogueCreationRun);
                
                return Ok();
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e);
//                _log.Fatal("Exception occured {e}");
                return BadRequest(e.Message);
            }
        }
        private DateTime MaxTime(DateTime dt1, DateTime dt2)
        {
            if (dt1 > dt2) return dt1;
            return dt2;
        }

        private DateTime MinTime(DateTime dt1, DateTime dt2)
        {
            if (dt1 > dt2) return dt2;
            return dt1;
        }
    }
}
