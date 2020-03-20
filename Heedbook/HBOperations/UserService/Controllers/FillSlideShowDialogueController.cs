using HBData;
using HBLib.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Base;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Globalization;
using System.Linq;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
 //   [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class FillSlideShowDialogueController : Controller
    {
        private readonly RecordsContext _context;
        private readonly INotificationHandler _handler;
        private readonly CheckTokenService _service;

        public FillSlideShowDialogueController(INotificationHandler handler, CheckTokenService service,
                                                    RecordsContext context)
        {
            _handler = handler;
            _service = service;
            _context = context;
        }

        [HttpPost("FillSlideShowDialogue")]
        [SwaggerOperation(Description = "Fill in SlideShowSessions DialogueId")]
        public IActionResult FillSlideShowDialogue([FromBody] FillSlideShowDialogueRun message)
        {
           // if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            _handler.EventRaised(message);
            return Ok();
        }

        [HttpPost("FillSlideShowDialogueAll")]
        [SwaggerOperation(Description = "Fill in SlideShowSessions DialogueId")]
        public IActionResult FillSlideShowDialoguesAll([FromQuery] string begTime)
        {
          //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");

            var dateFormat = "HH:mm:ss dd.MM.yyyy";
            var timeBeg = DateTime.ParseExact(begTime, dateFormat, CultureInfo.InvariantCulture);
            var dialogues = _context.Dialogues.Where(x => x.BegTime >= timeBeg && x.StatusId == 3).Select(x => x.DialogueId).ToList();
            foreach (var dId in dialogues)
            {
                var @eventFillSlideShowDialogue = new FillSlideShowDialogueRun
                {
                    DialogueId = dId
                };
                _handler.EventRaised(eventFillSlideShowDialogue);
            }
            return Ok($"runned for {dialogues.Count()} dialogues");
        }
    }
}