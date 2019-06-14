using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBLib;
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
        private readonly SftpSettings _sftpSettings;
        private readonly INotificationPublisher _notificationPublisher;

        public DialogueRecalculateController(INotificationHandler handler, RecordsContext context, 
                                                ElasticClient log, SftpClient sftpClient, 
                                                INotificationPublisher notificationPublisher,
                                                SftpSettings sftpSettings)
        {
            _handler = handler;
            _context = context;
            _log = log;
            _sftpClient = sftpClient;
            _notificationPublisher = notificationPublisher;
            _sftpSettings = sftpSettings;
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
                _log.Info($"DialogueId: {DialogueId}");
                var dialogue = _context.Dialogues
                    .Include(p=>p.DialogueAudio)                    
                    .Include(p=>p.DialogueInterval)
                    .Include(p=>p.DialogueVisual)                    
                    .Include(p=>p.DialogueClientProfile)
                    .Include(p=>p.DialogueFrame)
                    .FirstOrDefault(p=>p.DialogueId == Guid.Parse(DialogueId));

                if(dialogue==null) return BadRequest("such Dialogue not exist in Data Base");

                var dialogueVideoFileExist = await _sftpClient.IsFileExistsAsync($"{_sftpSettings.DestinationPath}dialoguevideos/{DialogueId}.mkv");  
                
                if(dialogue!=null 
                    && dialogueVideoFileExist 
                    && (dialogue.StatusId == 3 || dialogue.StatusId == 6 || dialogue.StatusId == 8))
                {
                    _log.Info($"DialogueVideoAssemble - Success | ");                    
                    result += $"DialogueVideoAssemble - Success | ";                             
                }
                else
                {  
                    _log.Info($"DialogueVideoAssemble - not Success | ");
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
                
                var dialogueAudioFileExist = await _sftpClient.IsFileExistsAsync($"{_sftpSettings.DestinationPath}dialogueaudios/{DialogueId}.wav");

                if(dialogue.DialogueAudio!=null
                    &&dialogueAudioFileExist)
                {
                    _log.Info($"VideoToSound - Success | ");       
                    result += $"VideoToSound - Success | ";     
                    
                    var speechResult = _context.FileAudioDialogues.FirstOrDefault(p => p.DialogueId == Guid.Parse(DialogueId));
                   
                    if(speechResult!=null && speechResult.STTResult!=null)
                    {
                        _log.Info($"GoogleRecognition - Success | ");
                        result += $"GoogleRecognition - Success | ";                 
                    }
                    else
                    {                        
                        _log.Info($"GoogleRecognition - not success | ");
                        result += $"GoogleRecognition - not success | ";
                        var @event = new AudioAnalyzeRun
                        {
                            Path = $"dialogueaudios/{DialogueId}.wav"
                        };
                        _notificationPublisher.Publish(@event);
                    }

                    if(dialogue.DialogueInterval != null)
                    {
                        _log.Info($"TonAnalyze - Success | "); 
                        result += $"TonAnalyze - Success | ";                
                    }
                    else
                    {
                        _log.Info($"TonAnalyze - not Success | ");
                        result += $"TonAnalyze - not Success | ";
                        var @event = new ToneAnalyzeRun
                        {
                            Path = $"dialogueaudios/{DialogueId}.wav"
                        };
                        _notificationPublisher.Publish(@event);
                    }    
                }
                else
                {
                    _log.Info($"VideoToSound - not Success | ");
                    result += $"VideoToSound - not Success | ";
                    _log.Info($"GoogleRecognition - not success | ");
                    result += $"GoogleRecognition - not success | ";
                    _log.Info($"TonAnalyze - not Success | ");
                    result += $"TonAnalyze - not Success | ";

                    var @event = new VideoToSoundRun
                    {
                        Path = $"dialoguevideos/{DialogueId}.mkv"
                    };
                    _notificationPublisher.Publish(@event);
                }                

                var dialogueAvatarExist = await _sftpClient.IsFileExistsAsync($"{_sftpSettings.DestinationPath}useravatars/{DialogueId}.jpg");                

                if(dialogueAvatarExist
                    && dialogue.DialogueVisual!=null
                    && dialogue.DialogueClientProfile!=null
                    && dialogue.DialogueFrame!=null)
                {
                    _log.Info($"FillingFrame - Success | ");
                    result += $"FillingFrame - Success | "; 
                }
                else
                {
                    _log.Info($"FillingFrame - not Success | ");
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