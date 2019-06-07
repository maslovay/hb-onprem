using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
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
    public class TestController : Controller
    {
        private readonly RecordsContext _context;
        public TestController(RecordsContext context)
        {
            _context = context;
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
                   .Where(p => p.BegTime > begTime  && (p.StatusId ==6 || p.StatusId == 3) ).ToList();

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


      