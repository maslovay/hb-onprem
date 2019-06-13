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
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class DialogueRecalculateController : Controller
    {
        private readonly RecordsContext _context;
        private readonly INotificationHandler _handler;
        private readonly ElasticClient _log;
        private readonly SftpClient _sftpClient;
        private readonly INotificationPublisher _notificationPublisher;

        public DialogueRecalculateController(INotificationHandler handler, RecordsContext context, 
                                                ElasticClient log, SftpClient sftpClient, 
                                                INotificationPublisher notificationPublisher)
        {
            _handler = handler;
            _context = context;
            _log = log;
            _sftpClient = sftpClient;
            _notificationPublisher = notificationPublisher;
        }

        [HttpGet]
        [SwaggerOperation(Description = "Recalculate dialogue")]
        public async Task<IActionResult> DialogueRecalculation([FromQuery] Guid dialogueId)
        {
            try
            {
                _log.Info("Function Dialogue recalculation started");
                var dialogue = _context.Dialogues
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.ApplicationUser.Company)
                    .Where(p => p.DialogueId == dialogueId)
                    .First();

                var languageId = dialogue.ApplicationUser.Company.LanguageId;

                var dialogueVideoMerge = new DialogueVideoAssembleRun
                {
                    ApplicationUserId = dialogue.ApplicationUserId,
                    DialogueId = dialogue.DialogueId,
                    BeginTime = dialogue.BegTime,
                    EndTime = dialogue.EndTime
                };
                _handler.EventRaised(dialogueVideoMerge);

                var fillingFrame = new DialogueCreationRun
                {
                    ApplicationUserId = dialogue.ApplicationUserId,
                    DialogueId = dialogue.DialogueId,
                    BeginTime = dialogue.BegTime,
                    EndTime = dialogue.EndTime
                }; 

                dialogue.StatusId = 6;
                dialogue.Comment = null;
                dialogue.CreationTime =DateTime.UtcNow;
                _context.SaveChanges();

                _handler.EventRaised(fillingFrame);
                _log.Info("Function Dialogue recalculation finished");
                return Ok();
            }
            catch (Exception e)
            {
                _log.Fatal($"Exception occured {e}");
                return BadRequest(e);
            }
        }

        [HttpPost("CheckRelatedDialogueData")]
        [SwaggerOperation(Description = "Re assemble dialogue")]
        public async Task<IActionResult> CheckRelatedDialogueData(string DialogueId)
        {
            var result = "";
            try
            {           
                if(DialogueId == null) return BadRequest("DialogueId is Empty");     
                System.Console.WriteLine($"DialogueId: {DialogueId}");
                var dialogue = _context.Dialogues.FirstOrDefault(p=>p.DialogueId == Guid.Parse(DialogueId));

                if(dialogue==null) return BadRequest("such Dialogue not exist in Data Base");

                var dialogueVideoFileExist = await _sftpClient.IsFileExistsAsync($"/home/nkrokhmal/storage/dialoguevideos/{DialogueId}.mkv");                
                System.Console.WriteLine($"{dialogue.BegTime} - {dialogue.EndTime}");

                if(dialogue!=null 
                    && dialogueVideoFileExist 
                    && (dialogue.StatusId == 3 || dialogue.StatusId == 6 || dialogue.StatusId == 8))
                {
                    System.Console.WriteLine($"DialogueVideoAssemble \t\tSuccess");   
                    result += $"DialogueVideoAssemble - Success | ";                             
                }
                else
                {      
                    System.Console.WriteLine($"DialogueVideoAssemble \t\tnot Success");
                    result += $"DialogueVideoAssemble - not Success | ";                    
                    var @event = new DialogueVideoAssembleRun
                    {
                        ApplicationUserId = dialogue.ApplicationUserId,
                        DialogueId = dialogue.DialogueId,
                        BeginTime = dialogue.BegTime,
                        EndTime = dialogue.EndTime
                    };
                    _notificationPublisher.Publish(@event);
                }
                
                
                var dialogueAudio = _context.DialogueAudios.FirstOrDefault(p=>p.DialogueId == Guid.Parse(DialogueId));
                var dialogueAudioFileExist = await _sftpClient.IsFileExistsAsync($"/home/nkrokhmal/storage/dialogueaudios/{DialogueId}.wav");

                if(dialogueAudio!=null
                    &&dialogueAudioFileExist)
                {
                    System.Console.WriteLine($"VideoToSound \t\t\tSuccess");           
                    result += $"VideoToSound - Success | ";         
                }
                else
                {
                    System.Console.WriteLine($"VideoToSound \t\t\tnot Success");
                    result += $"VideoToSound - not Success | ";
                    var @event = new VideoToSoundRun
                    {
                        Path = $"dialoguevideos/{DialogueId}.mkv"
                    };
                    _notificationPublisher.Publish(@event);
                }

                var speechResult = _context.FileAudioDialogues.FirstOrDefault(p => p.DialogueId == Guid.Parse(DialogueId));

                if(speechResult?.STTResult!=null)
                {
                    System.Console.WriteLine($"GoogleRecognition \t\tSuccess");   
                    result += $"GoogleRecognition - Success | ";                 
                }
                else
                {
                    System.Console.WriteLine($"GoogleRecognition \t\tnot success");
                    result += $"GoogleRecognition - not success | ";
                    var @event = new AudioAnalyzeRun
                    {
                        Path = $"dialogueaudios/{DialogueId}.wav"
                    };
                    _notificationPublisher.Publish(@event);
                }

                var dialogueIntervals = _context.DialogueIntervals.FirstOrDefault(p => p.DialogueId == Guid.Parse(DialogueId));
                var dialogueAudioResult = _context.DialogueAudios.FirstOrDefault(p => p.DialogueId == Guid.Parse(DialogueId));

                if(dialogueIntervals != null && dialogueAudioResult!=null)
                {
                    System.Console.WriteLine($"TonAnalyze \t\t\tSuccess");    
                    result += $"TonAnalyze - Success | ";                
                }
                else
                {
                    System.Console.WriteLine($"TonAnalyze \t\t\tnot Success");
                    result += $"TonAnalyze - not Success | ";
                    var @event = new ToneAnalyzeRun
                    {
                        Path = $"dialogueaudios/{DialogueId}.wav"
                    };
                    _notificationPublisher.Publish(@event);
                }

                var dialogueAvatarExist = await _sftpClient.IsFileExistsAsync($"/home/nkrokhmal/storage/useravatars/{DialogueId}.jpg");
                var dialogueVisuals = _context.DialogueVisuals.FirstOrDefault(p => p.DialogueId == Guid.Parse(DialogueId));
                var dialogueClientProfiles = _context.DialogueClientProfiles.FirstOrDefault(p => p.DialogueId == Guid.Parse(DialogueId));
                var dialogueFrames = _context.DialogueFrames.FirstOrDefault(p => p.DialogueId == Guid.Parse(DialogueId));   

                if(dialogueAvatarExist
                    && dialogueVisuals!=null
                    && dialogueClientProfiles!=null
                    && dialogueFrames!=null)
                {
                    System.Console.WriteLine($"FillingFrame \t\t\tSuccess\n"); 
                    result += $"FillingFrame - Success | "; 
                }
                else
                {
                    System.Console.WriteLine($"FillingFrame \t\t\tnot Success\n");
                    result += $"FillingFrame - not Success | ";
                    var @event = new DialogueCreationRun
                    {
                        ApplicationUserId = dialogue.ApplicationUserId,
                        DialogueId = dialogue.DialogueId,
                        BeginTime = dialogue.BegTime,
                        EndTime = dialogue.EndTime
                    };
                    _notificationPublisher.Publish(@event);
                }
                return Ok(result);
            }
            catch(Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return BadRequest();
            }
        }
    }
}