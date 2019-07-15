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
        
        public TestController(RecordsContext context, IGenericRepository repository)
        {
            _context = context;
            _repository = repository;
        }

        [HttpPost("[action]")]
        public async Task Test2()
        {
            
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
        public async Task<ObjectResult> RecognizedWords()
        {
            var sttResult = String.Empty;//"[{\"Time\":0.84,\"Duration\":0.2,\"Word\":\"и\"},{\"Time\":2.25,\"Duration\":0.59,\"Word\":\"стрелка\"},{\"Time\":4.07,\"Duration\":0.36,\"Word\":\"и\"},{\"Time\":8.09,\"Duration\":0.35,\"Word\":\"не\"},{\"Time\":8.82,\"Duration\":0.35,\"Word\":\"дают\"},{\"Time\":9.46,\"Duration\":0.12,\"Word\":\"ни\"},{\"Time\":9.62,\"Duration\":0.08,\"Word\":\"о\"},{\"Time\":9.79,\"Duration\":0.24,\"Word\":\"чем\"},{\"Time\":12.66,\"Duration\":0.24,\"Word\":\"я\"},{\"Time\":13.33,\"Duration\":0.89,\"Word\":\"могу\"},{\"Time\":14.22,\"Duration\":0.21,\"Word\":\"это\"},{\"Time\":14.49,\"Duration\":0.27,\"Word\":\"все\"},{\"Time\":14.94,\"Duration\":0.39,\"Word\":\"равно\"},{\"Time\":16.23,\"Duration\":0.24,\"Word\":\"не\"},{\"Time\":16.47,\"Duration\":0.48,\"Word\":\"понимать\"},{\"Time\":16.95,\"Duration\":0.3,\"Word\":\"меня\"},{\"Time\":17.46,\"Duration\":0.24,\"Word\":\"когда\"},{\"Time\":17.7,\"Duration\":0.09,\"Word\":\"я\"},{\"Time\":17.79,\"Duration\":0.45,\"Word\":\"встаю\"},{\"Time\":19.31,\"Duration\":0.24,\"Word\":\"и\"},{\"Time\":22.82,\"Duration\":0.37,\"Word\":\"иду\"},{\"Time\":23.37,\"Duration\":0.51,\"Word\":\"видимо\"},{\"Time\":24.69,\"Duration\":0.16,\"Word\":\"не\"},{\"Time\":24.85,\"Duration\":0.26,\"Word\":\"стал\"},{\"Time\":25.11,\"Duration\":0.54,\"Word\":\"одеваться\"},{\"Time\":26.1,\"Duration\":0.21,\"Word\":\"я\"},{\"Time\":26.31,\"Duration\":0.36,\"Word\":\"понял\"},{\"Time\":26.67,\"Duration\":0.15,\"Word\":\"что\"},{\"Time\":27.09,\"Duration\":0.21,\"Word\":\"он\"},{\"Time\":27.31,\"Duration\":0.48,\"Word\":\"работает\"},{\"Time\":28.86,\"Duration\":0.45,\"Word\":\"никто\"},{\"Time\":29.31,\"Duration\":0.12,\"Word\":\"не\"},{\"Time\":29.43,\"Duration\":0.3,\"Word\":\"ждёт\"},{\"Time\":30.21,\"Duration\":0.51,\"Word\":\"забурлила\"},{\"Time\":30.75,\"Duration\":0.12,\"Word\":\"и\"},{\"Time\":31.08,\"Duration\":0.24,\"Word\":\"я\"},{\"Time\":31.32,\"Duration\":0.12,\"Word\":\"не\"},{\"Time\":31.44,\"Duration\":0.2,\"Word\":\"могу\"},{\"Time\":31.67,\"Duration\":0.16,\"Word\":\"ни\"},{\"Time\":31.83,\"Duration\":0.24,\"Word\":\"туда\"},{\"Time\":32.07,\"Duration\":0.15,\"Word\":\"ни\"},{\"Time\":32.22,\"Duration\":0.3,\"Word\":\"сюда\"},{\"Time\":34.86,\"Duration\":0.18,\"Word\":\"я\"},{\"Time\":35.04,\"Duration\":0.3,\"Word\":\"вообще\"},{\"Time\":35.34,\"Duration\":0.12,\"Word\":\"не\"},{\"Time\":35.47,\"Duration\":0.51,\"Word\":\"понимаю\"},{\"Time\":36.45,\"Duration\":0.29,\"Word\":\"я\"},{\"Time\":36.75,\"Duration\":0.17,\"Word\":\"а\"},{\"Time\":36.91,\"Duration\":0.47,\"Word\":\"вообще\"},{\"Time\":37.38,\"Duration\":0.69,\"Word\":\"конечно\"},{\"Time\":39.83,\"Duration\":0.31,\"Word\":\"тебя\"},{\"Time\":42.18,\"Duration\":0.43,\"Word\":\"какой\"},{\"Time\":42.78,\"Duration\":0.43,\"Word\":\"седоки\"},{\"Time\":44.34,\"Duration\":0.46,\"Word\":\"открыто\"},{\"Time\":45.29,\"Duration\":0.2,\"Word\":\"при\"},{\"Time\":45.52,\"Duration\":0.32,\"Word\":\"всех\"}]";
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

        private string GetPositiveShareInText(List<string> recognizedWords)
        {
            var sentence = string.Join(" ", recognizedWords);
            
            
            var posShareStrg = RunPython.Run("GetPositiveShare.py",
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "sentimental"), "3",
                sentence, null);

            if (!String.IsNullOrEmpty(posShareStrg.Item2.Trim()))
                throw new Exception("RunPython err string: " + posShareStrg.Item2);

            return posShareStrg.ToString(); //double.Parse(posShareStrg.Item1.Trim(), CultureInfo.CurrentCulture);
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


      