using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AsrHttpClient;
using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib.Model;
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
        private readonly INotificationHandler _handler;
        
        public TestController(RecordsContext context, IGenericRepository repository, INotificationHandler handler )
        {
            _context = context;
            _repository = repository;
            _handler = handler;
        }

        [HttpGet("[action]/{timelInHours}")]
        public async Task<ActionResult<IEnumerable<Dialogue>>> CheckIfAnyAssembledDialogues( int timelInHours )
        {
            var dialogs = _repository.GetWithInclude<Dialogue>(
                d => d.EndTime >= DateTime.Now.AddHours(-timelInHours)
                     && d.EndTime < DateTime.Now
                     && d.StatusId == 3);

            if (dialogs.Any())
                return Ok($"Assembled dialogues present for last {timelInHours} hours: {dialogs.Count()}");

            return NotFound($"NO assembled dialogues present for last {timelInHours} hours!!!");
        }
        

        [HttpGet("[action]")]
        public async Task<ObjectResult> RecognizedWords(Guid dialogueId)
        {
            try
            {
                var dialogue = _repository.Get<Dialogue>().FirstOrDefault(d => d.DialogueId == dialogueId);
                var sttFad = _repository.Get<FileAudioDialogue>().FirstOrDefault(fad => fad.DialogueId == dialogueId);
                var sttResult = sttFad.STTResult;
                var asrResults = JsonConvert.DeserializeObject<List<AsrResult>>(sttResult);

                var recognized = new List<WordRecognized>(100);

                asrResults.ForEach(word =>
                {
                    recognized.Add(new WordRecognized
                    {
                        Word = word.Word,
                        StartTime = word.Time.ToString(CultureInfo.InvariantCulture),
                        EndTime = (word.Time + word.Duration).ToString(CultureInfo.InvariantCulture)
                    });
                });

                var recognizedWords = recognized.Select(r => r.Word).ToList();
                var share = GetPositiveShareInText(recognizedWords);
                
                return Ok("Share: " + share);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private string GetPositiveShareInText(List<string> recognizedWords)
        {
            var sentence = string.Join(" ", recognizedWords);
            
            
            var posShareStrg = RunPython.Run("GetPositiveShare.py",
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "sentimental"), "3",
                sentence, null);

            if (!String.IsNullOrEmpty(posShareStrg.Item2.Trim()))
                throw new Exception("RunPython err string: " + posShareStrg.Item2);

            return double.Parse(posShareStrg.Item1.Trim()).ToString(); //double.Parse(posShareStrg.Item1.Trim(), CultureInfo.CurrentCulture);
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

       [HttpGet("[action]")]
       public async Task ResendVideosForFraming(string fileNamesString)
       {
           var names = fileNamesString.Split(',');

           int i = 0;

           foreach (var name in names)
           {
               if ( i % 30 == 0 )
                   Thread.Sleep(60000);
               ++i;
               await ResendVideoForFraming(name);
           }
       }
       
       [HttpGet("[action]")]
       public async Task ResendVideoForFraming(string fileName)
       {
           var message = new FramesFromVideoRun();
           message.Path = $"videos/{fileName}";
           Console.WriteLine($"Sending message {JsonConvert.SerializeObject(message)}");
           _handler.EventRaised(message);
       }
    }
}


      