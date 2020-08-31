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
using Microsoft.AspNetCore.Authorization;
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
   // [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class DialogueRecalculateController : Controller
    {
        private readonly IGenericRepository _repository;
        private readonly INotificationHandler _handler;
//        private readonly ElasticClient _log;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        private readonly INotificationPublisher _notificationPublisher;
        private readonly CheckTokenService _service;


        public DialogueRecalculateController(INotificationHandler handler,
            /* ElasticClient log,*/ 
            SftpClient sftpClient, 
            INotificationPublisher notificationPublisher,
            SftpSettings sftpSettings, 
            IGenericRepository repository, 
            CheckTokenService service)
        {
            _handler = handler;
//            _log = log;
            _sftpClient = sftpClient;
            _notificationPublisher = notificationPublisher;
            _sftpSettings = sftpSettings;
            _repository = repository;
            _service = service;
        }

        [HttpGet]
        [SwaggerOperation(Description = "Recalculate dialogue")]
        public async Task<IActionResult> DialogueRecalculation([FromQuery] Guid dialogueId)
        {
           // if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            try
            {
//                _log.Info("Function Dialogue recalculation started");
                var dialogue = _repository.GetAsQueryable<Dialogue>()
                    .Include(p => p.Device)
                    .Include(p => p.Device.Company)
                    .Where(p => p.DialogueId == dialogueId)
                    .First();

                var languageId = dialogue.Device.Company.LanguageId;
                var dialogueVideoMerge = new DialogueVideoAssembleRun
                {
                    ApplicationUserId = (Guid)dialogue.ApplicationUserId,
                    DialogueId = dialogue.DialogueId,
                    BeginTime = dialogue.BegTime,
                    EndTime = dialogue.EndTime
                };
                _handler.EventRaised(dialogueVideoMerge);

                var fillingFrame = new DialogueCreationRun
                {
                    ApplicationUserId = dialogue.ApplicationUserId,
                    DeviceId = dialogue.DeviceId,
                    DialogueId = dialogue.DialogueId,
                    ClientId = dialogue.ClientId,
                    BeginTime = dialogue.BegTime,
                    EndTime = dialogue.EndTime
                }; 

                dialogue.StatusId = 6;
                dialogue.Comment = null;
                dialogue.CreationTime =DateTime.UtcNow;
                _repository.Save();

                _handler.EventRaised(fillingFrame);
                // _log.Info("Function Dialogue recalculation finished");
                return Ok();
            }
            catch (Exception e)
            {
                // _log.Fatal($"Exception occured {e}");
                return BadRequest(e);
            }
        }

        [HttpPost("CheckRelatedDialogueData")]
        [SwaggerOperation(Description = "Re assemble dialogue")]
        public async Task<IActionResult> CheckRelatedDialogueData(Guid dialogueId)
        {
            // if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            // _log.SetFormat("{DialogueId}");
            // _log.SetArgs(dialogueId);
            var result = "";
            try
            {           
                var dialogue = _repository.GetAsQueryable<Dialogue>()
                    .Include(p=>p.DialogueAudio)                    
                    .Include(p=>p.DialogueVisual)                    
                    .Include(p=>p.DialogueClientProfile)
                    .Include(p=>p.DialogueFrame)
                    .FirstOrDefault(p=>p.DialogueId == dialogueId);

                if (dialogue == null) 
                    return BadRequest("Such dialogue does not exist in database!");
                
                var dialogueVideoFileExist = await _sftpClient.IsFileExistsAsync($"{_sftpSettings.DestinationPath}dialoguevideos/{dialogueId}.mkv");  
//                _log.Info($"Video file exist - {dialogueVideoFileExist}");
                
                if(dialogueVideoFileExist)
                {
                    var dialogueAudioFileExist = await _sftpClient.IsFileExistsAsync($"{_sftpSettings.DestinationPath}dialogueaudios/{dialogueId}.wav");   
//                    _log.Info($"Audio file exist - {dialogueAudioFileExist}");
                    if(dialogueAudioFileExist)
                    {
                        var speechResult = _repository.GetAsQueryable<FileAudioDialogue>()
                            .FirstOrDefault(p => p.DialogueId == dialogueId);
//                        _log.Info($"Audio analyze result - {speechResult ==null}");

                        if(speechResult == null)
                        {
                            result += "Starting AudioAnalyze, ";                        
                            var @event = new AudioAnalyzeRun
                            {
                                Path = $"dialogueaudios/{dialogueId}.wav"
                            };
                            _notificationPublisher.Publish(@event);
                        }

//                        _log.Info($"Tone analyze result - {dialogue.DialogueAudio == null}");
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
//                        _log.Info("Starting video to sound");
                        result += "Starting VideoToSound, ";
                        var @event = new VideoToSoundRun2
                        {
                            Path = $"dialoguevideos/{dialogueId}.mkv"
                        };
                        _notificationPublisher.Publish(@event);
                    }                

//                    _log.Info($"Filling frame result - {(!dialogue.DialogueVisual.Any() || !dialogue.DialogueClientProfile.Any() || !dialogue.DialogueFrame.Any())}");
                    if(!dialogue.DialogueVisual.Any() || !dialogue.DialogueClientProfile.Any() || !dialogue.DialogueFrame.Any())
                    {
                        result += "Starting FillingFrames, ";
                        var @event = new DialogueCreationRun
                        {
                            ApplicationUserId = dialogue.ApplicationUserId,
                            DeviceId = dialogue.DeviceId,
                            DialogueId = dialogue.DialogueId,
                            BeginTime = dialogue.BegTime,
                            EndTime = dialogue.EndTime,
                            ClientId = dialogue.ClientId
                        };
                        _notificationPublisher.Publish(@event);
                    }                     
                }
                else
                {  
//                    _log.Info("Starting dialogue video assemble");
                    result += "Starting DialogueVideoAssemble, ";
                    var @event = new DialogueVideoAssembleRun
                    {
                        ApplicationUserId = dialogue.ApplicationUserId,
                        DialogueId = dialogue.DialogueId,
                        BeginTime = dialogue.BegTime,
                        EndTime = dialogue.EndTime,
                        DeviceId = dialogue.DeviceId
                        
                    };
                    _notificationPublisher.Publish(@event);
                } 

                if (dialogue.StatusId != 3)
                {
                    dialogue.StatusId = 6;
                    dialogue.CreationTime = DateTime.UtcNow;
                    dialogue.Comment = "";
                    _repository.Save();
                }      
//                _log.Info("Function finished");
                
                return Ok(result);
            }
            catch(Exception e)
            {
//                _log.Fatal("Exception occured {e}");
                return BadRequest(e);
            }
        }
        
        [HttpGet("[action]")]
        public IActionResult RecalcPositiveShare()
        {
          //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            var result = 0.0;

            var dialogs = _repository.GetWithInclude<Dialogue>(f => f.CreationTime >= DateTime.Now.AddDays(-5)
                                                                    && f.DialogueSpeech.All(ds => ds.PositiveShare == 0.0),
                f => f.DialogueSpeech, f => f.DialogueAudio).OrderByDescending(f => f.CreationTime).ToArray();

//            _log.Info($"RecalcPositiveShare(): DDialogues to analyze {dialogs.Length}");   
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
//                            _log.Info($"RecalcPositiveShare(): Speech for dialogue {ff.DialogueId}");

                            var posShareStrg =
                                RunPython.Run("GetPositiveShare.py",
                                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "3",
                                    words.ToString(), null);

                            Console.WriteLine($"Speech for dialogue {ff.DialogueId} pos share result: {posShareStrg}");
//                            _log.Info($"RecalcPositiveShare(): Speech for dialogue {ff.DialogueId} pos share result: {posShareStrg}");
                            result = double.Parse(posShareStrg.Item1.Trim().Replace("\n", string.Empty));
                            
                            if (result > 0)
                            {
                                speech.PositiveShare = result;
                                _repository.Update(speech);
                            }
                        }
                        catch (Exception ex)
                        {
//                            _log.Info($"RecalcPositiveShare() exception: " + ex.Message);
                        }
                    }
                }

                _repository.Save();
            }
            return Ok();
        }
    }
}