using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBData.Repository;
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
    public class TestController : Controller
    {
        private readonly IGenericRepository _repository;
        private readonly RecordsContext _context;
        
        public TestController(RecordsContext context, IGenericRepository repository
        )
        {
            _context = context;
            _repository = repository;
        }

        [HttpPost("[action]")]
        public async Task Test2()
        {
            
        }


        [HttpGet("[action]")]
        public async Task<ActionResult<IEnumerable<Dialogue>>> GetLast20ProcessedDialogues()
        {
            var dialogs = _repository.GetWithInclude<Dialogue>(
                    d => d.EndTime >= DateTime.Now.AddDays(-1) && d.EndTime < DateTime.Now && d.StatusId == 3,
                    d => d.DialogueSpeech,
                    d => d.DialogueVisual,
                    d => d.DialogueAudio,
                    d => d.DialogueWord)
                .OrderByDescending(d => d.EndTime)
                .Take(30);

            return Ok(dialogs.ToList());
        }

        [HttpPost("[action]")]
        public async Task Test1(DialogueCreationRun message)
        {
            var frameIds =
                _repository.Get<FileFrame>().Where(item =>
                        item.ApplicationUserId == message.ApplicationUserId
                        && item.Time >= message.BeginTime
                        && item.Time <= message.EndTime)
                    .Select(item => item.FileFrameId)
                    .ToList();
            var emotions =
                _repository.GetWithInclude<FrameEmotion>(item => frameIds.Contains(item.FileFrameId),
                    item => item.FileFrame).ToList();

            var dt1 = DateTime.Now;
            var attributes =
                _repository.GetWithInclude<FrameAttribute>(item => frameIds.Contains(item.FileFrameId),
                    item => item.FileFrame).ToList();

            var dt2 = DateTime.Now;
            
            Console.WriteLine($"Delta: {dt2-dt1}");
        }

       [HttpPost]
       [SwaggerOperation(Description = "Save video from frontend and trigger all process")]
       public async Task<IActionResult> Test()
       {
           try
           {
               //var applicationUserId = "010039d5-895b-47ad-bd38-eb28685ab9aa";
               var begTime = DateTime.Now.AddDays(-3);

               var dialogues = _context.Dialogues
                   .Include(p => p.DialogueFrame)
                   .Include(p => p.DialogueAudio)
                   .Include(p => p.DialogueInterval)
                   .Include(p => p.DialogueVisual)
                   .Include(p => p.DialogueClientProfile)
                   .Where(item => item.StatusId == 6)
                   .ToList();

               System.Console.WriteLine(dialogues.Count());
               foreach (var dialogue in dialogues)
               {
                   var url = $"https://slavehb.northeurope.cloudapp.azure.com/user/DialogueRecalculate?dialogueId={dialogue.DialogueId}";
                   var request = WebRequest.Create(url);

                   request.Credentials = CredentialCache.DefaultCredentials;
                   request.Method = "GET";
                   request.ContentType = "application/json-patch+json";

                   var responce = await request.GetResponseAsync();
                   System.Console.WriteLine($"Response -- {responce}");

                   Thread.Sleep(1000);
               }
            //    dialogues.ForEach(p=>p.StatusId = 6);
               dialogues.ForEach(p => p.CreationTime = DateTime.UtcNow);
               _context.SaveChanges();
               System.Console.WriteLine("Конец");
               return Ok();
           }
           catch (Exception e)
           {
               return BadRequest(e);
           }
       }
    }

}


      