using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBLib.Utils;
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
    [ApiController]
    public class DialogueMergeController : Controller
    {
        private readonly RecordsContext _context;
        private readonly INotificationHandler _handler;
//        private readonly ElasticClient _log;


        public DialogueMergeController(INotificationHandler handler, RecordsContext context/*, ElasticClient log*/)
        {
            _handler = handler;
            _context = context;
//            _log = log;
        }

        [HttpPost]
        [SwaggerOperation(Description = "Method for merge some dialogues with status 3 in the range")]
        public async Task<IActionResult> MergeDialogues([FromQuery] String applicationUserId,
            [FromQuery] String begTime,
            [FromQuery] String endTime)
        {
            try
            {  
//                _log.Info("Function Video save info started");
                var dateFormat = "yyyyMMddHHmmss";
                var userId = Guid.Parse(applicationUserId);
                var timeBeg = DateTime.ParseExact(begTime, dateFormat, CultureInfo.InvariantCulture).AddHours(-3);
                var timeEnd = DateTime.ParseExact(endTime, dateFormat, CultureInfo.InvariantCulture).AddHours(-3);

                System.Console.WriteLine($"applicationUserId: {userId}");
                System.Console.WriteLine($"timeBeg: {timeBeg}");
                System.Console.WriteLine($"timeEnd: {timeEnd}");
                if(timeBeg == null || timeEnd == null || applicationUserId == null)
                    return BadRequest("One of the parameters is invalid!");

                var dialogues = _context.Dialogues
                    .Include(p => p.Client)
                    .Where(p => p.ApplicationUserId == userId
                        && p.StatusId == 3
                        && p.BegTime >= timeBeg
                        && p.EndTime <= timeEnd)
                    .OrderBy(p => p.BegTime)
                    .ToList();
                System.Console.WriteLine($"dialogues.Count: {dialogues.Count}");
                if(dialogues == null || dialogues.Count == 0)
                    return BadRequest("No exist dialogues in this range!");
                dialogues.ForEach(p => p.StatusId = 8);

                var newDialogueId = Guid.NewGuid();
                var maxBegTime = MaxTime(timeBeg, dialogues.FirstOrDefault().BegTime);
                var minEndTime = MinTime(timeEnd, dialogues.LastOrDefault().EndTime);
                System.Console.WriteLine($"maxBegTime: {maxBegTime}");
                System.Console.WriteLine($"minEndTime: {minEndTime}");
                System.Console.WriteLine($"{newDialogueId}");
                var firstDialogue = dialogues.FirstOrDefault();
                var newDialogue = new Dialogue
                {
                    DialogueId = newDialogueId,
                    ClientId = firstDialogue.ClientId,
                    PersonFaceDescriptor = firstDialogue.PersonFaceDescriptor,
                    CreationTime = DateTime.UtcNow,
                    BegTime = maxBegTime,
                    EndTime = minEndTime,
                    ApplicationUserId = userId,
                    LanguageId = firstDialogue.LanguageId,
                    StatusId = 6,
                    InStatistic = true
                };
                _context.Dialogues.Add(newDialogue);
                

                var dialogueVideoAssembleRun = new DialogueVideoAssembleRun
                {
                    ApplicationUserId = userId,
                    DialogueId = newDialogueId,
                    BeginTime = maxBegTime,
                    EndTime = minEndTime
                };
                var dialogueCreationRun = new DialogueCreationRun 
                {
                    ApplicationUserId = userId,
                    DialogueId = newDialogueId,
                    BeginTime = maxBegTime,
                    EndTime = minEndTime
                };

                _handler.EventRaised(dialogueVideoAssembleRun);
                _handler.EventRaised(dialogueCreationRun);

                _context.SaveChanges();
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