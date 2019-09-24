using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
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
        private readonly GoogleConnector _googleConnector;
        private readonly DescriptorCalculations _calc;
        private readonly INotificationHandler _handler;
        private readonly SftpClient _sftpClient;
        private readonly FFMpegWrapper _wrapper;


        public TestController(RecordsContext context, IGenericRepository repository, GoogleConnector googleConnector, 
            DescriptorCalculations calc, INotificationHandler handler, SftpClient sftpClient, FFMpegWrapper wrapper)
        {
            _context = context;
            _repository = repository;
            _googleConnector = googleConnector;
            _calc = calc;
            _handler = handler;
            _sftpClient = sftpClient;
            _wrapper = wrapper;
        }

        [HttpGet("[action]/{timelInHours}")]
        public async Task<ActionResult<IEnumerable<Dialogue>>> CheckIfAnyAssembledDialogues(int timelInHours)
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
        public async Task<ObjectResult> RecognizedWords(Guid dialogueId)
        {
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

        [HttpGet("[action]/{appUserId}")]
        public async Task GetPersonId(Guid appUserId)
        {
            var begTime = DateTime.Now.AddDays(-2);
            var dialogues = _context.Dialogues
                .Where(p => p.ApplicationUserId == appUserId)
                .Where(p => !String.IsNullOrEmpty(p.PersonFaceDescriptor) && p.BegTime >= begTime)
                .OrderBy(p => p.BegTime)
                .ToList();
                
            foreach (var curDialogue in dialogues.Where(p => p.PersonId == null).ToList())
            {
                var dialoguesProceeded = dialogues.Where(p => p.ApplicationUserId == curDialogue.ApplicationUserId && p.PersonId != null).ToList();
                curDialogue.PersonId = FindId(curDialogue, dialoguesProceeded);
            }
                
            _context.SaveChanges();
        }
        
        private Guid? FindId(Dialogue curDialogue, List<Dialogue> dialogues, double threshold=0.42)
        {
            if (!dialogues.Any()) return Guid.NewGuid();
            foreach (var dialogue in dialogues)
            {
                var cosResult = _calc.Cos(curDialogue.PersonFaceDescriptor, dialogue.PersonFaceDescriptor);
                System.Console.WriteLine($"Cos distance is -- {cosResult}");
                if (cosResult > threshold) return dialogue.PersonId;
            }
            return Guid.NewGuid();

        }

        private string GetPositiveShareInText(List<string> recognizedWords)
        {
            var sentence = string.Join(" ", recognizedWords);


            var posShareStrg = RunPython.Run("GetPositiveShare.py",
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "sentimental"), "3",
                sentence, null);

            if (!String.IsNullOrEmpty(posShareStrg.Item2.Trim()))
                throw new Exception("RunPython err string: " + posShareStrg.Item2);

            return
                double.Parse(posShareStrg.Item1.Trim())
                    .ToString(); //double.Parse(posShareStrg.Item1.Trim(), CultureInfo.CurrentCulture);
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

            Console.WriteLine($"Delta: {dt2 - dt1}");
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
                    var url =
                        $"https://slavehb.northeurope.cloudapp.azure.com/user/DialogueRecalculate?dialogueId={dialogue.DialogueId}";
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

        [HttpGet("[action]/{dialogueId}/{path}")]
        public async Task RunVoiceRecognition(Guid dialogueId, string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                var splitedString = path.Split('/');
                var fileName = path;
                //var dialogueId = Guid.Parse(Path.GetFileNameWithoutExtension(fileName));
                var dialogue = _context.Dialogues
                    .FirstOrDefault(p => p.DialogueId == dialogueId);

                if (dialogue != null)
                {
                    var fileAudios = _context.FileAudioDialogues.Where(p => p.DialogueId == dialogueId).ToList();
                    fileAudios.Where(p => p.StatusId != 6)
                        .ToList()
                        .ForEach(p => p.StatusId = 8);

                    var fileAudio = new FileAudioDialogue
                    {
                        DialogueId = dialogueId,
                        CreationTime = DateTime.UtcNow,
                        FileName = fileName,
                        StatusId = 3,
                        FileContainer = "dialogueaudios",
                        BegTime = dialogue.BegTime,
                        EndTime = dialogue.EndTime,
                        Duration = dialogue.EndTime.Subtract(dialogue.BegTime).TotalSeconds
                    };
                    await _googleConnector.CheckApiKey();

                    var recognized = new List<WordRecognized>();

                    if (Environment.GetEnvironmentVariable("INFRASTRUCTURE") == "Cloud")
                    {
                        var languageId = Int32.Parse("2");

                        var currentPath = Directory.GetCurrentDirectory();
                        var token = await _googleConnector.GetAuthorizationToken(currentPath);

                        var blobGoogleDriveName =
                            dialogueId + "_client" + Path.GetExtension(fileName);
                        await _googleConnector.LoadFileToGoogleDrive(blobGoogleDriveName, path, token);
                        await _googleConnector.MakeFilePublicGoogleCloud(blobGoogleDriveName, "./", token);
                        var transactionId =
                            await _googleConnector.Recognize(blobGoogleDriveName, languageId, dialogueId.ToString(),
                                true, true);
                        if (transactionId == null || transactionId.Name <= 0)
                        {
                            Console.WriteLine("transaction id is null. Possibly wrong api key");
                        }
                        else
                        {
                            fileAudio.TransactionId = transactionId.Name.ToString();


                            var sttResults = await _googleConnector.GetGoogleSTTResults(fileAudio.TransactionId);
                            while (sttResults.Response == null)
                            {
                                Thread.Sleep(60);
                                sttResults = await _googleConnector.GetGoogleSTTResults(fileAudio.TransactionId);
                            }

                            if (sttResults.Response.Results.Any())
                            {
                                sttResults.Response.Results
                                    .ForEach(res => res.Alternatives
                                        .ForEach(alt => alt.Words
                                            .ForEach(word =>
                                            {
                                                if (word == null)
                                                {
                                                    Console.WriteLine("word = NULL!");
                                                    return;
                                                }

                                                if (word.EndTime == null)
                                                {
                                                    Console.WriteLine("No word.EndTime!");
                                                    return;
                                                }

                                                if (word.StartTime == null)
                                                {
                                                    Console.WriteLine("No word.StartTime!");
                                                    return;
                                                }

                                                word.EndTime =
                                                    word.EndTime.Replace('s', ' ');
                                                // .Replace('.', ',');
                                                word.StartTime =
                                                    word.StartTime.Replace('s', ' ');
                                                // .Replace('.', ',');
                                                recognized.Add(word);
                                            })));
                                fileAudio.STTResult = JsonConvert.SerializeObject(recognized);
                            }
                            else
                            {
                                fileAudio.StatusId = 7;
                                fileAudio.STTResult = "[]";
                            }
                        }
                    }

                    _context.FileAudioDialogues.Add(fileAudio);
                    _context.SaveChanges();
                }
            }
        }

        [HttpGet("[action]")]
        public async Task MassVideoRestore()
        {
            var videoList = System.IO.File.ReadAllText("videoList.txt");
            
            var paths = videoList.Split('\n');

            foreach (var ftpAddress in paths)
            {
                Console.WriteLine($"File: {ftpAddress} ...");
                await VideoRestore(ftpAddress);
            }
        }
        
        
        [HttpGet("[action]")]
        public async Task VideoRestore(string ftpVideoPath)
        {
            try
            {  
                var regexDate = new Regex(@"(\d\d\d\d\d\d\d\d\d\d\d\d\d\d)");
                var regexGuid = new Regex(@"videos/(.*?)_");
                if ( !regexDate.IsMatch(ftpVideoPath) || !regexGuid.IsMatch(ftpVideoPath) )
                    return;
                
                var mtcDate = regexDate.Match(ftpVideoPath);
                var mtcGuid = regexGuid.Match(ftpVideoPath);
                var begTime = mtcDate.Groups[1].Value.ToString();
                var applicationUserId = Guid.Parse(mtcGuid.Groups[1].Value.ToString());
               
                var fileName = Path.GetFileName(ftpVideoPath);
                if (_context.FileVideos.Any(fv => fv.FileName == fileName))
                {
                    Console.Write("  no need to process");
                    return;
                }

                Console.Write("  processing");
                await _sftpClient.DownloadFromFtpToLocalDiskAsync(ftpVideoPath, "tmp");
                var duration = _wrapper.GetDuration("tmp/" + fileName);

                
                var languageId = _context.ApplicationUsers
                                         .Include(p => p.Company)
                                         .Include(p => p.Company.Language)
                                         .Where(p => p.Id == applicationUserId)
                                         .First().Company.Language.LanguageId;

                var stringFormat = "yyyyMMddHHmmss";
                var time = DateTime.ParseExact(begTime, stringFormat, CultureInfo.InvariantCulture);

                var videoFile = new FileVideo();
                videoFile.ApplicationUserId = applicationUserId;
                videoFile.BegTime = time;
                videoFile.CreationTime = DateTime.UtcNow;
                videoFile.Duration = duration;
                videoFile.EndTime = time.AddSeconds((Double) duration);
                videoFile.FileContainer = "videos";
                videoFile.FileExist = true;
                videoFile.FileName = fileName;
                videoFile.FileVideoId = Guid.NewGuid();
                videoFile.StatusId = 6;

                _context.FileVideos.Add(videoFile);
                _context.SaveChanges();

//                if (videoFile.FileExist)
//                {
//                    var message = new FramesFromVideoRun();
//                    message.Path = $"videos/{fileName}";
//                    Console.WriteLine($"Sending message {JsonConvert.SerializeObject(message)}");
//                    _handler.EventRaised(message);
//                }
//                else
//                {
//                    Console.WriteLine($"No such file videos/{fileName}");
//                }

                System.IO.File.Delete("tmp/" + fileName);
                
                Console.Write("  OK!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }
    }
}


      