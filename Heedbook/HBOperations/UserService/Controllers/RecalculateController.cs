using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HBData;
using Microsoft.AspNetCore.Mvc;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class RecalculateController : ControllerBase
    {
        private readonly INotificationHandler _handler;
        private readonly RecordsContext _context;

        public RecalculateController(INotificationHandler handler, RecordsContext context)
        {
            _handler = handler;
            _context = context;
        }

        [HttpPost]
        [SwaggerOperation(Description = "Calculate dialogue satisfaction score")]
        public void RecalculateSatisfaction()
        {
           var dialogues = _context.Dialogues.Where(p => p.StatusId == 3).ToList();
            foreach( var dialogue in  dialogues)
            {
                var message = new FillingSatisfactionRun{
                    DialogueId = dialogue.DialogueId
                };
                _handler.EventRaised(message);
                Thread.Sleep(1000);
                System.Console.WriteLine($"Processing dialogue {dialogue.DialogueId}");
            }
        }
    }
}