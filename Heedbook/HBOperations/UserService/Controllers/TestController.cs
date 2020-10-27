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
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using HBLib;
using HBMLHttpClient.Model;
using System.Drawing;
using Microsoft.AspNetCore.Authorization;
using RabbitMqEventBus;
using System.Diagnostics;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
  //  [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class TestController : Controller
    {
        private readonly IGenericRepository _repository;
        private readonly INotificationHandler _handler;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        private readonly FFMpegWrapper _wrapper;
        private readonly CheckTokenService _service;
        private readonly INotificationPublisher _publisher;
        private readonly ElasticClient _elasticClient;

        public TestController(IGenericRepository repository,
            SftpSettings sftpSettings,
            FFMpegWrapper wrapper,
            INotificationHandler handler, SftpClient sftpClient, CheckTokenService service,
            INotificationPublisher publisher,
            ElasticClient elasticClient)
        {
            _repository = repository;
            _handler = handler;
            _sftpClient = sftpClient;
            _sftpSettings = sftpSettings;
            _wrapper = wrapper;
            _service = service;
            _publisher = publisher;
            _elasticClient = elasticClient;
        }

        [HttpPost("[action]")]
        public IActionResult TestHookRequest(object message)
        {
           // if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            System.Console.WriteLine(JsonConvert.SerializeObject(message));
            return Ok();
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> CreateAvatar(string fileName)
        {
          //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            var frame = _repository.GetAsQueryable<FileFrame>()
                .Include(p => p.FrameAttribute)
                .Where(p => p.FileName == fileName)
                .FirstOrDefault();

            var video = _repository.GetAsQueryable<FileVideo>().Where(p => p.BegTime <= frame.Time && p.EndTime >= frame.Time && p.DeviceId == frame.DeviceId).FirstOrDefault();
            var dt = frame.Time;
            var seconds = dt.Subtract(video.BegTime).TotalSeconds;
            System.Console.WriteLine($"Seconds - {seconds}, FileVideo - {video.FileName}");

            var localVidePath =
                await _sftpClient.DownloadFromFtpToLocalDiskAsync("videos/" + video.FileName);
            System.Console.WriteLine(localVidePath);
            var localPath = Path.Combine(_sftpSettings.DownloadPath, frame.FileName);
            System.Console.WriteLine($"Avatar path - {localPath}");
            var output = await _wrapper.GetFrameNSeconds(localVidePath, localPath, Convert.ToInt32(seconds));
            System.Console.WriteLine(output);

            var faceRectangle = JsonConvert.DeserializeObject<FaceRectangle>(frame.FrameAttribute.FirstOrDefault().Value);
            var rectangle = new Rectangle
            {
                Height = faceRectangle.Height,
                Width = faceRectangle.Width,
                X = faceRectangle.Top,
                Y = faceRectangle.Left
            };

            var stream = FaceDetection.CreateAvatar(localPath, rectangle);
            stream.Seek(0, SeekOrigin.Begin);
            await _sftpClient.UploadAsMemoryStreamAsync(stream, "test/", $"{frame.FileName}");
            stream.Close();
            return Ok();
        }

        [HttpGet("[action]/{timelInHours}")]
        public async Task<ActionResult<IEnumerable<Dialogue>>> CheckIfAnyAssembledDialogues( int timelInHours )
        {
           // if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
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
          //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
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
          //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
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
        public async Task<IActionResult> Test1(DialogueCreationRun message)
        {
           // if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
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
            return Ok();
        }

       [HttpPost]
       [SwaggerOperation(Description = "Save video from frontend and trigger all process")]
       public async Task<IActionResult> Test()
       {
          //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            try
           {
               //var applicationUserId = "010039d5-895b-47ad-bd38-eb28685ab9aa";
               var begTime = DateTime.Now.AddDays(-3);

               var dialogues = _repository.GetAsQueryable<Dialogue>()
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
               _repository.Save();
               System.Console.WriteLine("Конец");
               return Ok();
           }
           catch (Exception e)
           {
               return BadRequest(e);
           }
       }

       [HttpGet("[action]")]
       public async Task<IActionResult> ResendVideosForFraming(string fileNamesString)
       {
          //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            var names = fileNamesString.Split(',');

           int i = 0;

           foreach (var name in names)
           {
               if ( i % 30 == 0 )
                   Thread.Sleep(60000);
               ++i;
               await ResendVideoForFraming(name);
           }
           return Ok();
        }
       
       [HttpGet("[action]")]
       public async Task<IActionResult> ResendVideoForFraming(string fileName)
       {
          //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            var message = new FramesFromVideoRun
            {
                Path = $"videos/{fileName}"
            };
            Console.WriteLine($"Sending message {JsonConvert.SerializeObject(message)}");
           _handler.EventRaised(message);
            return Ok();
        }
        ///Method for get Cell Value
        private static string GetCellValue(SpreadsheetDocument document, Cell cell)
        {
            SharedStringTablePart stringTablePart = document.WorkbookPart.SharedStringTablePart;
            string value = cell.CellValue.InnerXml;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                return stringTablePart.SharedStringTable.ChildElements[Int32.Parse(value)].InnerText;
            }
            else
            {
                return value;
            }
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> SendCommandToTabletLoadTest(TabletLoadRun model)
        {
            _publisher.Publish(model);
            System.Console.WriteLine($"model sended");
            return Ok("model sended!");
        }
        [HttpGet("[action]")]
        public async Task<string> CheckDBConnections()
        {
            var phraseTypes = _repository.GetAsQueryable<PhraseType>()
                .ToList();
            return JsonConvert.SerializeObject(phraseTypes);
        }
        [HttpGet("[action]")]
        public async Task<string> CheckSftpConnection()
        {
            var fileExist = await _sftpClient.IsFileExistsAsync($"gif/bf0fbd4b-e85d-4dbb-b806-bb6b9f87fe8f.gif");
            return $"file exist: {fileExist}";
        }
        [HttpPost("VideoToGif")]
        [SwaggerOperation(Description = "Extract audio from video")]
        public IActionResult VideoToGif([FromBody] VideoContentToGifRun message)
        {
          //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            _publisher.Publish(message);
            Debug.WriteLine("sended message");
            return Ok();
        }
        [HttpPost("send VideoToSound model")]
        public async Task SendVideoToSoundModel(string path)
        {
            _publisher.Publish(new VideoToSoundRun(){Path=path});
        }
        [HttpPost("send CheckElasticClient")]
        public async Task CheckElasticClient(string message)
        {
            _elasticClient.SetFormat("{Path}");
            _elasticClient.Fatal(message);
        }
        [HttpPost("send VideoContentToGifRun")]
        public async Task CheckContentToGif(string path)
        {
            var model = new VideoContentToGifRun
            {
                Path = path
            };
            _publisher.Publish(model);
        }
        [HttpPost("[action]")]
        public async Task CheckToneAnalyzeRun(string path)
        {
            var model = new ToneAnalyzeRun
            {
                Path = path
            };
            _publisher.Publish(model);
        }
        [HttpPost("[action]")]
        public async Task CheckPersonOnlineDetectionRun(PersonOnlineDetectionRun model)
        {
            _publisher.Publish(model);
        }
        [HttpPost("[action]")]
        public async Task CheckFillSlideShowDialogueRun(FillSlideShowDialogueRun model)
        {
            _publisher.Publish(model);
        }
        [HttpPost("[action]")]
        public async Task CheckFillingHintsRun(FillingHintsRun model)
        {
            _publisher.Publish(model);
        }
    }
}


      