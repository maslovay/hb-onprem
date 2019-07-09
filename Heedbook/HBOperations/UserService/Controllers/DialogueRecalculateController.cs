using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AsrHttpClient;
using HBData;
using HBData.Models;
using HBData.Repository;
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
        private readonly IGenericRepository _repository;
        private readonly INotificationHandler _handler;
        private readonly ElasticClient _log;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        private readonly INotificationPublisher _notificationPublisher;

        public DialogueRecalculateController(INotificationHandler handler, RecordsContext context, 
                                                ElasticClient log, SftpClient sftpClient, 
                                                INotificationPublisher notificationPublisher,
                                                SftpSettings sftpSettings, IGenericRepository repository)
        {
            _handler = handler;
            _context = context;
            _log = log;
            _sftpClient = sftpClient;
            _notificationPublisher = notificationPublisher;
            _sftpSettings = sftpSettings;
            _repository = repository;
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
                
                if(dialogueVideoFileExist)
                {
                    _log.Info($"DialogueVideoAssemble - Success | ");     
                    var dialogueAudioFileExist = await _sftpClient.IsFileExistsAsync($"{_sftpSettings.DestinationPath}dialogueaudios/{DialogueId}.wav");

                    if(dialogue.DialogueAudio!=null
                        && dialogueAudioFileExist)
                    {
                        _log.Info($"VideoToSound - Success | ");    
                        
                        var speechResult = _context.FileAudioDialogues.FirstOrDefault(p => p.DialogueId == Guid.Parse(DialogueId));
                        if(speechResult!=null && speechResult.STTResult!=null)
                        {
                            _log.Info($"GoogleRecognition - Success | ");                
                        }
                        else
                        {                        
                            _log.Info($"GoogleRecognition - not success | ");
                            var @event = new AudioAnalyzeRun
                            {
                                Path = $"dialogueaudios/{DialogueId}.wav"
                            };
                            _notificationPublisher.Publish(@event);
                        }

                        if(dialogue.DialogueInterval != null)
                        {
                            _log.Info($"TonAnalyze - Success | ");             
                        }
                        else
                        {
                            _log.Info($"TonAnalyze - not Success | ");
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
                        _log.Info($"GoogleRecognition - not success | ");
                        _log.Info($"TonAnalyze - not Success | ");

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
                    }
                    else
                    {
                        _log.Info($"FillingFrame - not Success | ");
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
                    _log.Info($"DialogueVideoAssemble - not Success | ");    
                    _log.Info($"VideoToSound - not Success | ");
                    _log.Info($"GoogleRecognition - not success | ");
                    _log.Info($"TonAnalyze - not Success | ");     
                    _log.Info($"FillingFrame - not Success | ");                   
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
                
                return Ok();
            }
            catch(Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return BadRequest();
            }
        }
        
        [HttpGet("[action]")]
        public void RecalcPositiveShare()
        {
            var result = 0.0;

            var dialogs = _repository.GetWithInclude<Dialogue>(f => f.CreationTime >= DateTime.Now.AddDays(-5)
                                                                    && f.DialogueSpeech.All(ds => ds.PositiveShare == 0.0),
                f => f.DialogueSpeech, f => f.DialogueAudio).OrderByDescending(f => f.CreationTime).ToArray();

            _log.Info($"RecalcPositiveShare(): DDialogues to analyze {dialogs.Length}");   
            Console.WriteLine($"Dialogues to analyze {dialogs.Length}");
            
            foreach (var ff in dialogs)
            {
                var fads = _repository.Get<FileAudioDialogue>().Where(x => x.DialogueId == ff.DialogueId && x.STTResult != null && x.STTResult.Length > 0);

                foreach (var fad in fads)
                {
                    StringBuilder words = new StringBuilder();

                    var asrResults = JsonConvert.DeserializeObject<List<AsrResult>>(fad.STTResult);
                    if (asrResults.Any())
                    {
                        asrResults.ForEach(word =>
                        {
                            words.Append(" ");
                            words.Append(word.Word);
                        });
                    }

                    foreach (var speech in ff.DialogueSpeech)
                    {
                        try
                        {
                            if (speech == null)
                                continue;

                            Console.WriteLine($"Speech for dialogue {ff.DialogueId}");
                            _log.Info($"RecalcPositiveShare(): Speech for dialogue {ff.DialogueId}");

                            var posShareStrg =
                                RunPython.Run("GetPositiveShare.py",
                                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "3",
                                    words.ToString(), _log);

                            Console.WriteLine($"Speech for dialogue {ff.DialogueId} pos share result: {posShareStrg}");
                            _log.Info($"RecalcPositiveShare(): Speech for dialogue {ff.DialogueId} pos share result: {posShareStrg}");
                            result = double.Parse(posShareStrg.Item1.Trim().Replace("\n", string.Empty));
                            
                            if (result > 0)
                            {
                                speech.PositiveShare = result;
                                _repository.Update(speech);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Info($"RecalcPositiveShare() exception: " + ex.Message);
                        }
                    }
                }

                _repository.Save();
            }
        }
    }
}