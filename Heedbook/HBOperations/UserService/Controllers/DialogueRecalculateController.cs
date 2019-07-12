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
        public async Task<IActionResult> CheckRelatedDialogueData(Guid dialogueId)
        {
            _log.SetFormat("{DialogueId}");
            _log.SetArgs(dialogueId);
            var result = "";
            try
            {           
                var dialogue = _context.Dialogues
                    .Include(p=>p.DialogueAudio)                    
                    .Include(p=>p.DialogueVisual)                    
                    .Include(p=>p.DialogueClientProfile)
                    .Include(p=>p.DialogueFrame)
                    .FirstOrDefault(p=>p.DialogueId == dialogueId);

                if (dialogue == null) return BadRequest("Such dialogue do not exist in PostgresDB");
                var dialogueVideoFileExist = await _sftpClient.IsFileExistsAsync($"{_sftpSettings.DestinationPath}dialoguevideos/{dialogueId}.mkv");  
                _log.Info($"Video file exist - {dialogueVideoFileExist}");
                if(dialogueVideoFileExist)
                {
                    var dialogueAudioFileExist = await _sftpClient.IsFileExistsAsync($"{_sftpSettings.DestinationPath}dialogueaudios/{dialogueId}.wav");   
                    _log.Info($"Audio file exist - {dialogueAudioFileExist}");
                    if(dialogueAudioFileExist)
                    {
                        var speechResult = _context.FileAudioDialogues.FirstOrDefault(p => p.DialogueId == dialogueId);
                        _log.Info($"Audio analyze result - {speechResult ==null}");

                        if(speechResult == null)
                        {
                            result += "Starting AudioAnalyze, ";                        
                            var @event = new AudioAnalyzeRun
                            {
                                Path = $"dialogueaudios/{dialogueId}.wav"
                            };
                            _notificationPublisher.Publish(@event);
                        }

                        _log.Info($"Tone analyze result - {dialogue.DialogueAudio == null}");
                        if(!dialogue.DialogueAudio.Any() || dialogue.DialogueAudio == null)
                        {
                            result += "Starting ToneAnalyze, ";
                            var @event = new ToneAnalyzeRun
                            {
                                Path = $"dialogueaudios/{dialogueId}.wav"
                            };
                            _notificationPublisher.Publish(@event);
                        }    
                    }
                    else
                    {
                        _log.Info("Starting video to sound");
                        result += "Starting VideoToSound, ";
                        var @event = new VideoToSoundRun
                        {
                            Path = $"dialoguevideos/{dialogueId}.mkv"
                        };
                        _notificationPublisher.Publish(@event);
                    }                

                    _log.Info($"Filling frame result - {dialogue.DialogueVisual ==null && dialogue.DialogueClientProfile ==null && dialogue.DialogueFrame ==null}");
                    if(dialogue.DialogueVisual ==null && dialogue.DialogueClientProfile ==null && dialogue.DialogueFrame ==null)
                    {
                        result += "Starting FillingFrames, ";
                        var @event = new DialogueCreationRun
                        {
                            ApplicationUserId = dialogue.ApplicationUserId,
                            DialogueId = dialogue.DialogueId,
                            BeginTime = dialogue.BegTime,
                            EndTime = dialogue.EndTime
                        };
                        _notificationPublisher.Publish(@event);
                    }                     
                }
                else
                {  
                    _log.Info("Starting dialogue video assemble");
                    result += "Starting DialogueVideoAssemble, ";                
                    var @event = new DialogueVideoAssembleRun
                    {
                        ApplicationUserId = dialogue.ApplicationUserId,
                        DialogueId = dialogue.DialogueId,
                        BeginTime = dialogue.BegTime,
                        EndTime = dialogue.EndTime
                    };
                    _notificationPublisher.Publish(@event);
                } 

                if (dialogue.StatusId != 3)    
                {
                    dialogue.StatusId = 6;
                    dialogue.CreationTime = DateTime.UtcNow;
                    dialogue.Comment = "";
                    _context.SaveChanges();
                }      
                _log.Info("Function finished");            
                
                return Ok(result);
            }
            catch(Exception e)
            {
                _log.Fatal("Exception occured {e}");
                return BadRequest(e);
            }
        }
    }
}