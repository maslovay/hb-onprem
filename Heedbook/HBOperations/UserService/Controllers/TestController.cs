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

namespace UserService.Controllers
{
    [Route("user/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class TestController : Controller
    {
        private readonly IGenericRepository _repository;
        private readonly RecordsContext _context;
        private readonly INotificationHandler _handler;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        private readonly FFMpegWrapper _wrapper;
        
        public TestController(RecordsContext context, IGenericRepository repository,
            SftpSettings sftpSettings,
            FFMpegWrapper wrapper,
            INotificationHandler handler, SftpClient sftpClient)
        {
            _context = context;
            _repository = repository;
            _handler = handler;
            _sftpClient = sftpClient;
            _sftpSettings = sftpSettings;
            _wrapper = wrapper;
        }

        [HttpPost("[action]")]
        public async Task CreateAvatar(string fileName)
        {
            var frame = _context.FileFrames
                .Include(p => p.FrameAttribute)
                .Where(p => p.FileName == fileName)
                .FirstOrDefault();

            var video = _context.FileVideos.Where(p => p.BegTime <= frame.Time && p.EndTime >= frame.Time && p.DeviceId == frame.DeviceId).FirstOrDefault();
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

        }

        
        
        [HttpGet("[action]/{timelInHours}")]
        public async Task<ActionResult<IEnumerable<Dialogue>>> CheckIfAnyAssembledDialogues( int timelInHours )
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
            
            Console.WriteLine($"Delta: {dt2-dt1}");
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

       [HttpGet("[action]")]
       public async Task ResendVideosForFraming(string fileNamesString)
       {
           var names = fileNamesString.Split(',');

           int i = 0;

           foreach (var name in names)
           {
               if ( i % 30 == 0 )
                   Thread.Sleep(60000);
               ++i;
               await ResendVideoForFraming(name);
           }
       }
       
       [HttpGet("[action]")]
       public async Task ResendVideoForFraming(string fileName)
       {
            var message = new FramesFromVideoRun
            {
                Path = $"videos/{fileName}"
            };
            Console.WriteLine($"Sending message {JsonConvert.SerializeObject(message)}");
           _handler.EventRaised(message);
       }
       [HttpGet("[action]")]
       public async Task AddCompanyDictionary(string fileName)
       {
           AddCpomanyPhrases();
       }
       
       private void AddCpomanyPhrases()
        {
            var filePath = "/home/oleg/Downloads/Phrases.xlsx";
            using(FileStream FS = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using(SpreadsheetDocument doc = SpreadsheetDocument.Open(filePath, false))
                {
                    System.Console.WriteLine();
                    WorkbookPart workbook = doc.WorkbookPart;
                    SharedStringTablePart sstpart = workbook.GetPartsOfType<SharedStringTablePart>().First();
                    SharedStringTable sst = sstpart.SharedStringTable;

                    WorksheetPart worksheet = workbook.WorksheetParts.First();
                    Worksheet sheet = worksheet.Worksheet;

                    var cells = sheet.Descendants<Cell>();
                    var rows = sheet.Descendants<Row>();

                    var phrases = _context.Phrases
                        .Include(p => p.PhraseType)
                        .ToList();
                    var phraseTypes = _context.PhraseTypes.ToList();

                    var user = _context.ApplicationUsers
                        .Include(p => p.Company)
                        .FirstOrDefault(p => p.FullName == "Сотрудник с бейджем №1");
                    
                    
                    foreach(var row in rows)
                    {
                        try
                        {
                            //var rowCells = row.Elements<Cell>();
                            var phraseTextString = GetCellValue(doc, row.Descendants<Cell>().ElementAt(0));
                            var phraseTypeString = GetCellValue(doc, row.Descendants<Cell>().ElementAt(1));
                            var existPhrase = phrases.FirstOrDefault(p => p.PhraseText == phraseTextString
                                    && p.PhraseType.PhraseTypeText == phraseTypeString);

                            var phraseType = phraseTypes.FirstOrDefault(p => p.PhraseTypeText == GetCellValue(doc, row.Descendants<Cell>().ElementAt(1)));
                            if(phraseType is null)
                                continue;
                            
                            if(existPhrase==null)
                            {   
                                System.Console.WriteLine($"phrase not exist in base");
                                var newPhrase = new Phrase
                                {
                                    PhraseId = Guid.NewGuid(),
                                    PhraseText = GetCellValue(doc, row.Descendants<Cell>().ElementAt(0)),
                                    PhraseTypeId = phraseType.PhraseTypeId,
                                    LanguageId = 2,
                                    WordsSpace = 1,
                                    Accurancy = 1,
                                    IsTemplate = false
                                } ;
                                var phraseCompany = new PhraseCompany
                                {
                                    PhraseCompanyId = Guid.NewGuid(),
                                    PhraseId = newPhrase.PhraseId,
                                    CompanyId = user.CompanyId
                                };  
                                System.Console.WriteLine($"Phrase: {newPhrase.PhraseText} - {newPhrase.PhraseTypeId}");
                                _context.Phrases.Add(newPhrase); 
                                _context.PhraseCompanys.Add(phraseCompany);
                            }
                            else
                            {
                                var phraseCompany = new PhraseCompany
                                {
                                    PhraseCompanyId = Guid.NewGuid(),
                                    PhraseId = existPhrase.PhraseId,
                                    CompanyId = user.CompanyId
                                };  
                                System.Console.WriteLine($"Phrase: {existPhrase.PhraseText} - {existPhrase.PhraseTypeId}");  
                                _context.PhraseCompanys.Add(phraseCompany); 
                                System.Console.WriteLine($"phrase exist in base");
                            }                            
                        }
                        catch(NullReferenceException ex)
                        {
                            System.Console.WriteLine($"exception!!");
                            break;
                        }   
                    }
                    _context.SaveChanges();
                }
            }
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

    }
}


      