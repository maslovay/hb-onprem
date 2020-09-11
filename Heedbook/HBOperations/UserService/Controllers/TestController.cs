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
using DocumentFormat.OpenXml;
using System.Text;

namespace UserService.Controllers
{
    [Route("user/[controller]")]
  //  [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class TestController : Controller
    {
        private readonly IGenericRepository _repository;
        private readonly RecordsContext _context;
        private readonly INotificationHandler _handler;
        private readonly SftpClient _sftpClient;
        private readonly SftpSettings _sftpSettings;
        private readonly FFMpegWrapper _wrapper;
        private readonly CheckTokenService _service;
        private readonly DescriptorCalculations _calc;

        public TestController(RecordsContext context, IGenericRepository repository,
            SftpSettings sftpSettings,
            FFMpegWrapper wrapper,
            INotificationHandler handler, SftpClient sftpClient, CheckTokenService service,
            DescriptorCalculations calc)
        {
            _context = context;
            _repository = repository;
            _handler = handler;
            _sftpClient = sftpClient;
            _sftpSettings = sftpSettings;
            _wrapper = wrapper;
            _service = service;
            _calc = calc;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> CreateAvatar(string fileName)
        {
          //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
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

        [HttpGet("[action]")]
       public async Task<IActionResult> AddCompanyDictionary(string fileName)
       {
          //  if (!_service.CheckIsUserAdmin()) return BadRequest("Requires admin role");
            AddCpomanyPhrases();
            return Ok();
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
        [HttpPost("[action]")]
        public async Task SendAudioAnalyzeModel(Guid dialogueId)
        {
            if(dialogueId == null) return;
            var model = new AudioAnalyzeRun
            {
                Path = $"dialogueaudios/{dialogueId}.wav"
            };
            _handler.EventRaised(model);
            System.Console.WriteLine($"model sended");
        }
        [HttpPost("[action]")]
        public void CountWords(Guid fileAudioDialogueId)
        {
            var fileAudioDialogues = _context.FileAudioDialogues.FirstOrDefault(p => p.FileAudioDialogueId == fileAudioDialogueId);
            System.Console.WriteLine($"fileAudioDialogues is null: {fileAudioDialogues is null}");
            var wordRecognized = JsonConvert.DeserializeObject<List<WordRecognized>>(fileAudioDialogues.STTResult);
            var text = "";
            var wordCount = 0;
            foreach(var word in wordRecognized)
            {
                text += $" {word.Word}";
                wordCount++;
            }
            System.Console.WriteLine($"text:\n{text}");
            System.Console.WriteLine($"wordCount:\n{wordCount}");
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> SaveDBTableIn()
        {
            //AlertTypes            
            //AspNetRole
            //CatalogueHints
            //CompanyIndustrys
            //Countrys
            //DeviceTypes
            //Languages
            //PhraseTypes
            //Statuss
            var AlertTypeList = _repository.GetAsQueryable<AlertType>()
                .ToList();
            var AlertTypeListJson = JsonConvert.SerializeObject(AlertTypeList);
            using(StreamWriter SW = new StreamWriter("AlertTypes.txt"))
            {
                SW.Write(AlertTypeListJson);
            }

            var AspNetRoleList = _repository.GetAsQueryable<ApplicationRole>()
                .ToList();
            var AspNetRoleJson = JsonConvert.SerializeObject(AspNetRoleList);
            using(StreamWriter SW = new StreamWriter("ApplicationRoles.txt"))
            {
                SW.Write(AspNetRoleJson);
            }

            var CatalogueHintsList = _repository.GetAsQueryable<CatalogueHint>()
                .ToList();
            var CatalogueHintsListJson = JsonConvert.SerializeObject(CatalogueHintsList);
            using(StreamWriter SW = new StreamWriter("CatalogueHints.txt"))
            {
                SW.Write(CatalogueHintsListJson);
            }

            var CompanyIndustrysList = _repository.GetAsQueryable<CompanyIndustry>()
                .ToList();
            var CompanyIndustrysListJson = JsonConvert.SerializeObject(CompanyIndustrysList);
            using(StreamWriter SW = new StreamWriter("CompanyIndustrys.txt"))
            {
                SW.Write(CompanyIndustrysListJson);
            }

            var CountrysList = _repository.GetAsQueryable<Country>()
                .ToList();
            var CountrysListJson = JsonConvert.SerializeObject(CountrysList);
            using(StreamWriter SW = new StreamWriter("Countrys.txt"))
            {
                SW.Write(CountrysListJson);
            }

            var DeviceTypesList = _repository.GetAsQueryable<DeviceType>()
                .ToList();
            var DeviceTypesListJson = JsonConvert.SerializeObject(DeviceTypesList);
            using(StreamWriter SW = new StreamWriter("DeviceTypes.txt"))
            {
                SW.Write(DeviceTypesListJson);
            }

            var LanguagesList = _repository.GetAsQueryable<Language>()
                .ToList();
            var LanguagesListJson = JsonConvert.SerializeObject(LanguagesList);
            using(StreamWriter SW = new StreamWriter("LanguagesLists.txt"))
            {
                SW.Write(LanguagesListJson);
            }

            var PhraseTypesList = _repository.GetAsQueryable<PhraseType>()
                .ToList();
            var PhraseTypesListJson = JsonConvert.SerializeObject(PhraseTypesList);
            using(StreamWriter SW = new StreamWriter("PhraseTypesLists.txt"))
            {
                SW.Write(PhraseTypesListJson);
            }

            var StatusList = _repository.GetAsQueryable<Status>()
                .ToList();
            var StatusListJson = JsonConvert.SerializeObject(StatusList);
            using(StreamWriter SW = new StreamWriter("StatusLists.txt"))
            {
                SW.Write(StatusListJson);
            }

            return Ok();
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> DeleteUBRIRUserAndCompanies(string fileName)
        {
            var companys = _repository.GetAsQueryable<Company>()
                .Where(p => p.CorporationId == Guid.Parse("b70306dc-8bb8-4b2d-a22c-004270711caf"))
                .ToList();
            var companyIds = companys.Select(p => p.CompanyId)
                .ToList();
            var users = _repository.GetAsQueryable<ApplicationUser>()
                .Where(p => companyIds.Contains((Guid)p.CompanyId))
                .ToList();
            var companyPhrases = _repository.GetAsQueryable<PhraseCompany>()
                .Where(p => companyIds.Contains((Guid)p.CompanyId))
                .ToList();
            _repository.Delete<Company>(companys);
            _repository.Delete<ApplicationUser>(users);
            _repository.Delete<PhraseCompany>(companyPhrases);
            _repository.Save();
            return Ok();
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> DeleteUBRIRPhrases(string fileName)
        {
            var ubrirCorporation = _repository.GetAsQueryable<Corporation>()
                        .FirstOrDefault(p => p.Name == "UBRIR");
            var companys = _repository.GetAsQueryable<Company>()
                .Where(p => p.CorporationId == ubrirCorporation.Id)
                .ToList();
            var companyIds = companys.Select(p => p.CompanyId)
                .ToList();
            var companyPhrases = _repository.GetAsQueryable<PhraseCompany>()
                .Where(p => companyIds.Contains((Guid)p.CompanyId))
                .ToList();
            System.Console.WriteLine($"companyPhrasesCount: {companyPhrases.Count}");
            var salesStages = _repository.GetAsQueryable<SalesStagePhrase>()
                .Where(p => companyIds.Contains((Guid)p.CompanyId)||p.CorporationId == ubrirCorporation.Id)
                .ToList();
            System.Console.WriteLine($"salesStagesCount: {salesStages.Count}");
            _repository.Delete<PhraseCompany>(companyPhrases);
            _repository.Delete<SalesStagePhrase>(salesStages);
            _repository.Save();
            return Ok();
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> UBRIRAddOfficesAndDictionary(string fileName)
        {
            var result = "";
            // result += AddCompanysAndUsers($"offices.xlsx");
            // // result += $"\n";
            // result += AddCompanyPhrases($"Library.xlsx");
            result += AddCompanyPhrases($"LibrarySalesStages3.xlsx");
            System.Console.WriteLine($"done");
            return Ok(result);
        }
        private string AddCompanysAndUsers(string filePath)
        {
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

                    
                    var users = _repository.GetAsQueryable<ApplicationUser>()
                        .ToList();

                    var userRoles = _repository.GetAsQueryable<ApplicationRole>()
                        .ToList();

                    var akBarsCorporation = _repository.GetAsQueryable<Corporation>()
                        .FirstOrDefault(p => p.Name == "UBRIR");

                    if(akBarsCorporation is null)
                        return "UBRIR Corporation not exist";

                    var akBarsCompanys = _repository.GetAsQueryable<Company>()
                        .Where(p => p.CorporationId == akBarsCorporation.Id)
                        .ToList();  
                    
                    var countryId = _repository.GetAsQueryable<Country>()
                        .FirstOrDefault(p => p.CountryName == "Russia").CountryId;

                    var _industry = _repository.GetAsQueryable<CompanyIndustry>()
                        .FirstOrDefault(p => p.CompanyIndustryName == "Bank");
                    string companyName = "";
                    foreach(var row in rows)
                    {
                        try
                        {
                            var tmpCompanyName = GetCellValue(doc, row.Descendants<Cell>().ElementAt(0));
                            if(tmpCompanyName != null && tmpCompanyName != "")
                                companyName = tmpCompanyName;

                            var userName = GetCellValue(doc, row.Descendants<Cell>().ElementAt(1));
                            var userEmail = GetCellValue(doc, row.Descendants<Cell>().ElementAt(2));
                            var userRoleName = GetCellValue(doc, row.Descendants<Cell>().ElementAt(3));
                            var userPassword = GetCellValue(doc, row.Descendants<Cell>().ElementAt(4));

                            userRoleName = userRoleName == $"Руководитель офиса продаж" ? "Manager" : "Employee";
                            var userRole = userRoles
                                .FirstOrDefault(p => p.Name == userRoleName);
                            if(userRole is null)
                                return $"userRole is null";

                            var existCompany = _repository.GetAsQueryable<Company>()
                                .FirstOrDefault(p => p.CompanyName == companyName);

                            if(existCompany is null)
                            {
                                var company = new Company
                                {
                                    CompanyId = Guid.NewGuid(),
                                    CompanyName = companyName,
                                    IsExtended = true,
                                    CompanyIndustryId = _industry.CompanyIndustryId,
                                    CreationDate = DateTime.Now,
                                    LanguageId = 2,
                                    CountryId = countryId,
                                    StatusId = 3,
                                    CorporationId = akBarsCorporation.Id
                                };
                                System.Console.WriteLine($"companyName: {company.CompanyName}");
                                _repository.Create<Company>(company);

                                var userExist = users.Any(p => p.UserName == userName);
                                if(userExist)
                                    continue;
                                if(userEmail is null)
                                    continue;
                                var _applicationUser = new ApplicationUser
                                {
                                    Id = Guid.NewGuid(),
                                    UserName = userName,
                                    NormalizedUserName = userName.ToUpper(),
                                    FullName = userName,
                                    CreationDate = DateTime.Now,
                                    CompanyId = company.CompanyId,
                                    EmailConfirmed = false,
                                    StatusId = 3,
                                    Email = userEmail,
                                    NormalizedEmail = userEmail.ToUpper(),
                                    //PasswordHash = _loginService.GeneratePasswordHash(userPassword),
                                    PhoneNumberConfirmed = false,
                                    TwoFactorEnabled = false,
                                    LockoutEnabled = false,
                                    UserRoles = new List<ApplicationUserRole>
                                    {
                                        new ApplicationUserRole
                                        {
                                            RoleId = userRole.Id,
                                        }
                                    }
                                };
                                
                                System.Console.WriteLine($"username: {_applicationUser.UserName} {userRoleName}");                                
                                _repository.Create<ApplicationUser>(_applicationUser);
                            }
                            else
                            {
                                var userExist = users.Any(p => p.UserName == userName);
                                if(userExist)
                                    continue;
                                if(userEmail is null)
                                    continue;
                                var _applicationUser = new ApplicationUser
                                {
                                    Id = Guid.NewGuid(),
                                    UserName = userName,
                                    NormalizedUserName = userName.ToUpper(),
                                    FullName = userName,
                                    CreationDate = DateTime.Now,
                                    CompanyId = existCompany.CompanyId,
                                    EmailConfirmed = false,
                                    StatusId = 3,
                                    Email = userEmail,
                                    NormalizedEmail = userEmail.ToUpper(),
                                    //PasswordHash = _loginService.GeneratePasswordHash(userPassword),
                                    PhoneNumberConfirmed = false,
                                    TwoFactorEnabled = false,
                                    LockoutEnabled = false,
                                    UserRoles = new List<ApplicationUserRole>
                                    {
                                        new ApplicationUserRole
                                        {
                                            RoleId = userRole.Id,
                                        }
                                    }
                                };
                                System.Console.WriteLine($"username: {_applicationUser.UserName} {userRoleName}");
                                _repository.Create<ApplicationUser>(_applicationUser);
                            }
                            _repository.Save();
                        }
                        catch(NullReferenceException ex)
                        {
                            System.Console.WriteLine($"exception:\n{ex}");
                            break;
                        }  
                        catch(Exception e)
                        {
                            System.Console.WriteLine(e);
                        } 
                    }
                    _repository.Save();
                    return "Ok";
                }
            }
        }
        private string AddCompanyPhrases(string filePath)
        {
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

                    var phrases = _repository.GetAsQueryable<Phrase>()
                        .Include(p => p.PhraseType)
                        .ToList();
                    var phraseTypes = _repository.GetAsQueryable<PhraseType>()
                        .ToList();

                    var corporation = _repository.GetAsQueryable<Corporation>()
                        .FirstOrDefault(p => p.Name == "UBRIR");
                    System.Console.WriteLine($"corporation exist: {corporation != null}");
                    if(corporation is null)
                        return "corporation is null";

                    var companys = _repository.GetAsQueryable<Company>()
                        .Where(p => p.CorporationId == corporation.Id)
                        .ToList();
                    
                    if(companys is null || companys.Count == 0)
                        return "companys is null";
                    
                    foreach(var row in rows)
                    {
                        try
                        {
                            string phraseTextString;
                            string phraseTypeString;
                            try
                            {
                                phraseTextString = GetCellValue(doc, row.Descendants<Cell>().ElementAt(0));
                                phraseTypeString = GetCellValue(doc, row.Descendants<Cell>().ElementAt(1));
                            }
                            catch(Exception e)
                            {
                                break;
                            }
                            if(phraseTextString == null && phraseTypeString == null)
                                break;


                            var salesStageSequenceNumber = 0;
                            try
                            {
                                if(GetCellValue(doc, row.Descendants<Cell>().ElementAt(2)) != null)
                                {
                                    salesStageSequenceNumber = Int32.Parse(GetCellValue(doc, row.Descendants<Cell>().ElementAt(2)));
                                }
                            }
                            catch(Exception e)
                            {}

                            var salesStages = _repository.GetAsQueryable<SalesStage>().ToList();

                            var existPhrase = phrases.FirstOrDefault(p => p.PhraseText == phraseTextString
                                    && p.PhraseType.PhraseTypeText == phraseTypeString);
                            
                            var phraseType = phraseTypes.FirstOrDefault(p => p.PhraseTypeText == GetCellValue(doc, row.Descendants<Cell>().ElementAt(1)));
                            if(phraseType is null)
                                continue;
                            
                            if(existPhrase==null)
                            {   
                                System.Console.WriteLine($"phrase not exist in base");
                                var phraseText = GetCellValue(doc, row.Descendants<Cell>().ElementAt(0));
                                var newPhrase = new Phrase
                                {
                                    PhraseId = Guid.NewGuid(),
                                    PhraseText = phraseText,
                                    PhraseTypeId = phraseType.PhraseTypeId,
                                    LanguageId = 2,
                                    WordsSpace = 1,
                                    Accurancy = 1,
                                    IsTemplate = false
                                };
                                _repository.Create<Phrase>(newPhrase); 
                                System.Console.WriteLine($"Phrase: {newPhrase.PhraseText} {newPhrase.PhraseTypeId}");
                                foreach(var company in companys)
                                {
                                    var phraseCompany = new PhraseCompany
                                    {
                                        PhraseCompanyId = Guid.NewGuid(),
                                        PhraseId = newPhrase.PhraseId,
                                        CompanyId = company.CompanyId
                                    };
                                    _repository.Create<PhraseCompany>(phraseCompany);
                                    System.Console.WriteLine($"phraseCompany: {phraseCompany.PhraseId} {phraseCompany.CompanyId}");

                                    if(salesStageSequenceNumber >= 1 && salesStageSequenceNumber <= 7)
                                    {
                                        var salesStage = new SalesStagePhrase
                                        {
                                            SalesStagePhraseId = Guid.NewGuid(),
                                            CompanyId = company.CompanyId,
                                            CorporationId = company.CorporationId,
                                            PhraseId = newPhrase.PhraseId,
                                            SalesStageId = salesStages.FirstOrDefault(p => p.SequenceNumber == salesStageSequenceNumber).SalesStageId
                                        };
                                        _repository.Create<SalesStagePhrase>(salesStage);
                                        System.Console.WriteLine($"created: salesStage for phrase");
                                    }                                    
                                }
                            }
                            else
                            {
                                foreach(var company in companys)
                                {
                                    var phraseCompany = new PhraseCompany
                                    {
                                        PhraseCompanyId = Guid.NewGuid(),
                                        PhraseId = existPhrase.PhraseId,
                                        CompanyId = company.CompanyId
                                    };  
                                    _repository.Create<PhraseCompany>(phraseCompany);
                                    System.Console.WriteLine($"Phrase: {existPhrase.PhraseText} - {existPhrase.PhraseTypeId}");
                                    if(salesStageSequenceNumber >= 1 && salesStageSequenceNumber <= 7)
                                    {
                                        var salesStage = new SalesStagePhrase
                                        {
                                            SalesStagePhraseId = Guid.NewGuid(),
                                            CompanyId = company.CompanyId,
                                            CorporationId = company.CorporationId,
                                            PhraseId = existPhrase.PhraseId,
                                            SalesStageId = salesStages.FirstOrDefault(p => p.SequenceNumber == salesStageSequenceNumber).SalesStageId
                                        };
                                        _repository.Create<SalesStagePhrase>(salesStage);
                                        System.Console.WriteLine($"created: salesStage for phrase");
                                    } 
                                }
                                System.Console.WriteLine($"phrase exist in base");
                            } 
                            // _repository.Save();
                        }
                        catch(NullReferenceException ex)
                        {
                            System.Console.WriteLine($"exception:\n{ex}");
                            break;
                        }   
                    }
                    _repository.Save();
                    return "all phrases added in DB";
                }
            }
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> RequestDialogueMkvToMp4()
        {
            var listOfFilesonFtp = (await _sftpClient.ListDirectoryFiles($"dialoguevideos", ".mkv")).ToList();
            var listOfFileGuidOnFtp = listOfFilesonFtp
                .Select(p => Guid.Parse(p.Split(".")[0]))
                .ToList();
            System.Console.WriteLine($"{listOfFilesonFtp.Count}");
            System.Console.WriteLine($"{listOfFileGuidOnFtp.Count}");
            System.Console.WriteLine($"{listOfFileGuidOnFtp.First()}");

            var listOfFilesOnDB = _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.StatusId == 3
                    && p.BegTime >= new DateTime(2020, 07, 01))
                .Select(p => p.DialogueId)
                .ToList();
            System.Console.WriteLine($"number of dialogues: {listOfFilesOnDB.Count}");

            var newList = listOfFileGuidOnFtp.Intersect(listOfFilesOnDB)
                .ToList();
            System.Console.WriteLine($"newList: {newList.Count}");

            var first = newList.First();
            // System.Console.WriteLine(first);
            // await SendMessageCreateGif(first.ToString());
            foreach(var d in newList)
            {
                System.Console.WriteLine(d.ToString());
                Thread.Sleep(500);
                await SendMessageCreateGif(d.ToString());
            }
            System.Console.WriteLine($"done");
            return Ok("dialogues sended!");
        }
        private async Task SendMessageCreateGif(string dialogueId)
        {
            var url = $"https://heedbookapi.northeurope.cloudapp.azure.com/user/VideoToSound/ConvertDialogueMkvToMp4?dialogueId={dialogueId}";
                
            var request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";                

            var responce = request.GetResponseAsync();
            System.Console.WriteLine($"responce: {responce}");
        }
        [HttpPost("[action]")]
        public async Task DeleteSalesStages()
        {
            try
            {
                var corporation = _repository.GetAsQueryable<Corporation>()
                .FirstOrDefault(p => p.Name == "UBRIR");
                var companys = _repository.GetAsQueryable<Company>()
                    .Where(p => p.CorporationId == corporation.Id)
                    .Select(p => (Guid?)p.CompanyId)
                    .ToList();

                var salesStages = _repository.GetAsQueryable<SalesStagePhrase>()
                    .Where(p => companys.Contains(p.CompanyId))
                    .ToList();
                System.Console.WriteLine($"salesStagesCount: {salesStages.Count}");
                _repository.Delete<SalesStagePhrase>(salesStages);
                _repository.Save();
                System.Console.WriteLine($"done");
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
            }
        }
        [HttpPost("[action]")]
        public async Task CreateReportFromExcell()
        {
            var fileName = $"wordsForReport.xlsx";

            var corporation = _repository.GetAsQueryable<Corporation>()
                .FirstOrDefault(p => p.Name == "UBRIR");
            System.Console.WriteLine($"corporation exist: {corporation != null}");
            if(corporation is null)
            {
                System.Console.WriteLine($"corporation is null");
                return;
            }

            var companys = _repository.GetAsQueryable<Company>()
                // .Where(p => p.CorporationId == corporation.Id)
                .Where(p => p.CompanyId == Guid.Parse("42701f43-2a03-4e73-8e8b-215089fb6bab"))//Ботанич
                // .Where(p => p.CompanyId == Guid.Parse("72e4aed1-709e-43e0-ad68-04e5305648e5"))//Чкаловск
                .Select(p => p.CompanyId)
                .ToList();
            System.Console.WriteLine($"companys: {companys.Count}");
            var devices = _repository.GetAsQueryable<Device>()
                .Where(p => companys.Contains(p.CompanyId))
                .Select(p => p.DeviceId)
                .ToList();
            System.Console.WriteLine($"devices: {devices.Count}");
            var dialogues = _repository.GetAsQueryable<Dialogue>()
                .Include(p => p.DialogueWord)
                .Where(p => p.BegTime >= new DateTime(2020, 08, 11)
                    && p.EndTime <= new DateTime(2020, 08, 17)
                    && devices.Contains(p.DeviceId)
                    && p.StatusId == 3)
                .OrderBy(p => p.BegTime)
                .ToList();

            
            var listOfSalesStages = new List<ReportWords>();
            using(FileStream FS = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using(SpreadsheetDocument doc = SpreadsheetDocument.Open(fileName, false))
                {
                    try
                    {
                        // System.Console.WriteLine();
                        WorkbookPart workbook = doc.WorkbookPart;
                        SharedStringTablePart sstpart = workbook.GetPartsOfType<SharedStringTablePart>().First();
                        SharedStringTable sst = sstpart.SharedStringTable;

                        // WorksheetPart worksheet = workbook.WorksheetParts.First();
                        // Worksheet sheet = worksheet.Worksheet;

                        string relationshipId = workbook.Workbook.Descendants<Sheet>().FirstOrDefault(s => s.Name.Equals("справочник"))?.Id;

                        Worksheet sheet = ((WorksheetPart)workbook.GetPartById(relationshipId)).Worksheet;

                        var cells = sheet.Descendants<Cell>();
                        var rows = sheet.Descendants<Row>();
                        var listOfFirstCollumn = new List<string>();
                        var listOfSecondCollumn = new List<string>();
                        string SalesStageName = null;  
                        var rowsCount = rows.Count();
                        var counter = 0;
                        foreach(var row in rows)
                        {     
                            // System.Console.WriteLine(row.Count());
                            string firstWord = null; 
                            string secondWord = null;    
                            try
                            {
                                var cell0 = row.Descendants<Cell>().ElementAt(0);
                                firstWord = GetCellValue(doc, cell0); 
                                System.Console.WriteLine($"f: {firstWord}");           
                            }  
                            catch(Exception e)
                            {
                                System.Console.WriteLine(e);
                            }  

                            try
                            {
                                var cell1 = row.Descendants<Cell>().ElementAt(1);
                                secondWord = GetCellValue(doc, cell1);   
                                System.Console.WriteLine($"s: {secondWord}");     
                            }  
                            catch(Exception e)
                            {
                                System.Console.WriteLine(e);
                            }                                            
                            
                            if(firstWord == null && secondWord == null)
                            {
                                continue;
                            }
                                
                            if(secondWord == "name" || counter == rowsCount - 1)
                            {                                
                                if(listOfFirstCollumn.Count > 0)
                                {
                                    listOfSalesStages.Add(
                                        new ReportWords()
                                        {
                                            Name = SalesStageName,
                                            FirstStageWords = listOfFirstCollumn,
                                            SecondStageWords = listOfSecondCollumn,
                                            SecondWordStat = listOfSecondCollumn.Select(p => new WordStat
                                            {
                                                Name = p,
                                                UsedCount = 0
                                            })
                                            .ToList()
                                        }
                                    );
                                }
                                // System.Console.WriteLine($"SalesStageName: {firstWord}");
                                SalesStageName = firstWord;
                                listOfFirstCollumn = new List<string>(){SalesStageName};
                                listOfSecondCollumn = new List<string>();
                            }
                            if(firstWord != "@" && secondWord != "name")
                                listOfFirstCollumn.Add(firstWord.ToLower());
                            if(firstWord == "@" && secondWord != null)
                                listOfSecondCollumn.Add(secondWord.ToLower());
                            counter++;
                        }
                        // System.Console.WriteLine($"firstcollumn");
                        // foreach(var w in listOfFirstCollumn)
                        //     System.Console.WriteLine(w);
                        // System.Console.WriteLine($"\nsecondcollumn");
                        // foreach(var w in listOfSecondCollumn)
                        //     System.Console.WriteLine(w);
                        // System.Console.WriteLine($"done");
                    }
                    catch(Exception e)
                    {
                        System.Console.WriteLine($"{e}");
                    }
                }
            }
            // System.Console.WriteLine($"{JsonConvert.SerializeObject(listOfSalesStages)}");
            
            //================================================

            foreach(var salesStage in listOfSalesStages)
            {
                var countOfDialogues = dialogues.Count();
                
                var firstStageDialogues = dialogues.Where(p => DialogueInFirstSalesStage(p, salesStage))
                    .ToList();
                var countOfFirstStageDialogues = firstStageDialogues.Count();
                var secondStageDialogues = firstStageDialogues.Where(p => DialogueInSecondSalesStage(p, salesStage))
                    .ToList();
                var secondOfFirstStageDialogues = secondStageDialogues.Count();
                System.Console.WriteLine($"{salesStage.Name} - AOD: {dialogues.Count} - FSOD: {countOfFirstStageDialogues} - SSOD: {secondOfFirstStageDialogues}");
                foreach(var w in salesStage.SecondWordStat)
                {
                    System.Console.WriteLine($"{w.Name} - {w.UsedCount}");
                }
            }
            System.Console.WriteLine($"done");
        }
        private bool DialogueInFirstSalesStage(Dialogue p, ReportWords salesStage)
        {
            if(p.DialogueWord.Count ==0 || p.DialogueWord == null)
                return false;
            if(p.DialogueWord.First().Words == null)
                return false;
            
            var words = p.DialogueWord.First().Words.ToLower();
            var newWords = JsonConvert.DeserializeObject<List<RecognitionWords>>(words);
            words = "";
            foreach(var w in newWords)
                words += $"{w.Word} ";

            foreach(var w in salesStage.FirstStageWords)
            {
                if(words.Contains(w))
                    return true;
            }
            return false;
        }
        private bool DialogueInSecondSalesStage(Dialogue d, ReportWords salesStage)
        {
            if(d.DialogueWord.First().Words == null)
                return false;
            if(salesStage.SecondStageWords.Count == 0)
                return false;
            var words = d.DialogueWord.First().Words.ToLower();
            var newWords = JsonConvert.DeserializeObject<List<RecognitionWords>>(words);
            words = "";
            foreach(var w in newWords)
                words += $"{w.Word} ";

            foreach(var w in salesStage.SecondStageWords)
            {
                if(words.Contains(w))
                {
                    salesStage.SecondWordStat.FirstOrDefault(p => p.Name == w).UsedCount += 1;
                    return true;
                }                    
            }
            return false;
        }
        [HttpPost("[action]")]
        public async Task UpploadUbrirLibrary()
        {
            SaveUbrirLibrary();
        }
        private void SaveUbrirLibrary()
        {
            using (SpreadsheetDocument document = SpreadsheetDocument.Create("UBRIRLIBRARY.xlsx", SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());               

                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "UbrirLibrary" };
                
                sheets.Append(sheet);                

                SheetData sheetData = worksheetPart.Worksheet.AppendChild(new SheetData());
                Row row1 = new Row();

                row1.Append(         
                    ConstructCell("Word", CellValues.String),
                    ConstructCell("WordType", CellValues.String)
                );
                sheetData.AppendChild(row1);

                var corporation = _repository.GetAsQueryable<Corporation>()
                    .FirstOrDefault(p => p.Name == "UBRIR");

                var companys = _repository.GetAsQueryable<Company>()
                    .Where(p => p.CorporationId == corporation.Id)
                    .Select(p => p.CompanyId)
                    .ToList();

                var words = _repository.GetAsQueryable<PhraseCompany>()
                    .Where(p => companys.Contains((Guid)p.CompanyId))
                    .Select(p => new LibraryItem
                        {
                            Phrase = p.Phrase.PhraseText,
                            PhraseType = p.Phrase.PhraseType.PhraseTypeText
                        })
                    .Distinct()
                    .ToList();
                Row tempRow;
                foreach(var w in words)
                {
                    tempRow = new Row();
                    tempRow.Append(
                        ConstructCell(w.Phrase.ToString(), CellValues.String),
                        ConstructCell(w.PhraseType.ToString(), CellValues.String)
                    );
                    sheetData.AppendChild(tempRow);
                }
                workbookPart.Workbook.Save();                
            }
        }
        [HttpPost("[action]")]
        public async Task SaveUbrirPhraseTable()
        {
            using (SpreadsheetDocument document = SpreadsheetDocument.Create("UBRIRPhraseTable.xlsx", SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());               

                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "UbrirLibrary" };
                
                sheets.Append(sheet);                

                SheetData sheetData = worksheetPart.Worksheet.AppendChild(new SheetData());
                Row row1 = new Row();

                row1.Append(         
                    ConstructCell("Текст Фразы", CellValues.String),
                    ConstructCell("Тип Фразы", CellValues.String),
                    ConstructCell("Доля диалогов содержащих фразу", CellValues.String),
                    ConstructCell("Лидер", CellValues.String)
                );
                sheetData.AppendChild(row1);

                var phraseTable = GetPhraseTable();
                
                Row tempRow;
                foreach(var ph in phraseTable.Where(p => p.PopularName != null))
                {
                    System.Console.WriteLine($"{JsonConvert.SerializeObject(ph)}");
                    tempRow = new Row();
                    tempRow.Append(
                        ConstructCell(ph.Phrase.ToString(), CellValues.String),
                        ConstructCell(ph.PhraseType.ToString(), CellValues.String),
                        ConstructCell(ph.Percent.ToString(), CellValues.String),
                        ConstructCell(ph.PopularName.ToString(), CellValues.String)
                    );
                    sheetData.AppendChild(tempRow);
                }
                workbookPart.Workbook.Save();                
            }
            System.Console.WriteLine($"done");
        }
        private List<PraseTableresult> GetPhraseTable()
        {
            var corporation = _repository.GetAsQueryable<Corporation>()
                    .FirstOrDefault(p => p.Name == "UBRIR");

            var companyIds = _repository.GetAsQueryable<Company>()
                .Where(p => p.CorporationId == corporation.Id)
                .Select(p => p.CompanyId)
                .ToList();
            var phraseIds = _repository.GetAsQueryable<PhraseCompany>()
                .Where(p => companyIds.Contains((Guid)p.CompanyId))
                .Select(p => p.PhraseId)
                .Distinct()
                .ToList();
            System.Console.WriteLine($"phraseIdsCOunt: {phraseIds.Count}");

            var dialogueIds = _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= new DateTime(2020, 08, 11)
                    && p.EndTime <= new DateTime(2020, 08, 17)
                    && p.StatusId == 3
                    && p.InStatistic == true)
                .Where(p => (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId)))
                    // && (!applicationUserIds.Any() || ( p.ApplicationUserId != null && applicationUserIds.Contains(p.ApplicationUserId)))
                    // && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId)))
                .Select(p => p.DialogueId).ToList();           

            var dialoguesTotal = dialogueIds.Count();
            
            // GET ALL PHRASES INFORMATION
            var phrasesInfo = _repository.GetAsQueryable<DialoguePhrase>()
                .Where(p => p.DialogueId.HasValue
                    && dialogueIds.Contains(p.DialogueId.Value)
                    && phraseIds.Contains((Guid)p.PhraseId)
                    // && (!phraseTypeIds.Any() || phraseTypeIds.Contains((Guid)p.Phrase.PhraseTypeId))
                    //&& (companysPhrases.Contains(p.PhraseId))
                    )
                .Select(p => new PhrasesInfo
                {
                    IsClient = p.IsClient,
                    FullName = p.Dialogue.ApplicationUser.FullName,
                    ApplicationUserId = p.Dialogue.ApplicationUserId,
                    DialogueId = p.DialogueId,
                    PhraseId = p.PhraseId,
                    PhraseText = p.Phrase.PhraseText,
                    PhraseTypeText = p.Phrase.PhraseType.PhraseTypeText
                })
                .AsQueryable();

            var result = phrasesInfo
                .GroupBy(p => p.PhraseText.ToLower())
                .Select(p => new PraseTableresult(){
                    Phrase = p.Key,
                    PhraseId = (Guid)p.First().PhraseId,
                    PopularName = p.GroupBy(q => q.FullName)
                        .OrderByDescending(q => q.Count())
                        .Select(g => g.Key)
                        .First(),
                    PhraseType = p.First().PhraseTypeText,
                    Percent = dialogueIds.Any() ? Math.Round(100 * Convert.ToDouble(p.Select(q => q.DialogueId).Distinct().Count()) / Convert.ToDouble(dialoguesTotal), 1) : 0,
                    Freq = Convert.ToDouble(p.Select(q => q.DialogueId).Distinct().Count()) != 0 ?
                        Math.Round(Convert.ToDouble(p.GroupBy(q => q.ApplicationUserId).Max(q => q.Count())) / Convert.ToDouble(p.Select(q => q.DialogueId).Distinct().Count()), 2) :
                        0
                    })
                .ToList();
            return result;
        }
        [HttpPost("[action]")]
        public async Task DialogueAndVideosStatistics()
        {
            // Длительность всех диалогов за неделю.
            // Количество диалогов со статусом 8 и их суммарная длительность.
            // Количество видео файлов, которые не являются частью диалога.
            // Суммарная длительность файлов, которые не являются частью диалога.

            var dialogues = _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime >= DateTime.Now.AddDays(-7)
                    && p.EndTime <= DateTime.Now)
                .ToList();

            var dialoguesWith3Status = dialogues.Where(p => p.StatusId == 3)
                .ToList();

            var videos = _repository.GetAsQueryable<FileVideo>()
                .Where(p => p.BegTime >= DateTime.Now.AddDays(-7)
                    && p.EndTime <= DateTime.Now)
                .ToList();

            var allDialoguesDuration = dialogues
                .Select(p => p.EndTime.Subtract(p.BegTime).TotalHours)
                .Sum();
            var numberOf8DialoguesStatus = dialogues.Where(p => p.StatusId == 8);
            var counter = 0;
            foreach(var d in numberOf8DialoguesStatus)
            {
                System.Console.WriteLine($"{d.BegTime} {d.EndTime} {d.Comment}");
                var url = $"https://heedbookapi.northeurope.cloudapp.azure.com/user/DialogueRecalculate?dialogueId={d.DialogueId}";
                
                var request1 = WebRequest.Create(url);
                request1.Credentials = CredentialCache.DefaultCredentials;
                request1.Method = "GET";
                request1.ContentType = "application/json-patch+json";
                
                var responce = request1.GetResponseAsync();
                System.Console.WriteLine($"{responce}");          

                counter++;
                Thread.Sleep(300);
                if(counter >=10)
                {
                    counter = 0;
                    Thread.Sleep(5000);
                }
            }
                
            var numberOf8DialoguesStatusDuration = numberOf8DialoguesStatus
                .Select(p => p.EndTime.Subtract(p.BegTime).TotalHours)
                .Sum();

            var notUsedVideos = new List<FileVideo>();
            foreach(var v in videos)
            {
                var intersectDialogues = dialogues.Where(p =>
                        ((p.BegTime <= v.BegTime
                            && p.EndTime > v.BegTime
                            && p.EndTime < v.EndTime) 
                        || (p.BegTime < v.EndTime
                            && p.BegTime > v.BegTime
                            && p.EndTime >= v.EndTime)
                        || (p.BegTime >= v.BegTime
                            && p.EndTime <= v.EndTime)
                        || (p.BegTime < v.BegTime
                            && p.EndTime > v.EndTime)))
                    .ToList();
                if(intersectDialogues == null || intersectDialogues.Count == 0)
                    notUsedVideos.Add(v);
            }
            notUsedVideos = notUsedVideos.Distinct().ToList();
            var notUsedVideosCount = notUsedVideos.Count();
            
            var allNotUsedVideosDuration = notUsedVideos
                .Select(p => p.EndTime.Subtract(p.BegTime).TotalHours)
                .Sum();
            // System.Console.WriteLine($"numberOfAllDialoguesCount: {dialogues.Count}");
            // System.Console.WriteLine($"dialoguesWith3StatusCount: {dialogues.Where(p => p.StatusId == 3).Count()}");
            System.Console.WriteLine($"allDialoguesDuration: {allDialoguesDuration} h");
            System.Console.WriteLine($"numberOf8DialoguesStatusCount: {numberOf8DialoguesStatus.Count()}");
            System.Console.WriteLine($"numberOf8DialoguesStatusDuration: {numberOf8DialoguesStatusDuration} h");
            System.Console.WriteLine($"videosCount: {videos.Count}");
            System.Console.WriteLine($"notUsedVideosCount: {notUsedVideosCount}");
            System.Console.WriteLine($"allNotUsedVideosDuration: {allNotUsedVideosDuration} h");
        }
        [HttpPost("[action]")]
        public async Task SavePhraseSalesStageCount()
        {
            using (SpreadsheetDocument document = SpreadsheetDocument.Create("UBRIRSalesStagePhraseCount.xlsx", SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());               

                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "UBRIRSalesStagePhraseCount" };
                
                sheets.Append(sheet);                

                SheetData sheetData = worksheetPart.Worksheet.AppendChild(new SheetData());
                Row row1 = new Row();

                row1.Append(         
                    ConstructCell("ApplicationUser", CellValues.String),
                    ConstructCell("Persentage", CellValues.String),
                    ConstructCell("SalesStage", CellValues.String)
                );
                sheetData.AppendChild(row1);

                var phraseSalesStageCount = PhraseSalesStageCount();
                
                Row tempRow;
                foreach(var us in phraseSalesStageCount)
                {
                    tempRow = new Row();
                    tempRow.Append(
                        ConstructCell(us.FullName.ToString(), CellValues.String)
                    );
                    sheetData.AppendChild(tempRow);
                    foreach(var s in us.SalesStagePhrases.OrderBy(p=> p.SequenceNumber))
                    {
                        tempRow = new Row();
                        tempRow.Append(
                            ConstructCell("", CellValues.String),
                            ConstructCell(s.PercentageOfExecution.ToString(), CellValues.String),
                            ConstructCell(s.SequenceNumber.ToString(), CellValues.String),
                            ConstructCell(s.SalesStageName.ToString(), CellValues.String)
                        );
                        sheetData.AppendChild(tempRow);
                    }
                    
                }
                workbookPart.Workbook.Save();                
            }
            System.Console.WriteLine($"done");
        }
        private List<UserSalesStagePhrase> PhraseSalesStageCount()
        {
            var corporation = _repository.GetAsQueryable<Corporation>()
                    .FirstOrDefault(p => p.Name == "UBRIR");

            var companyIds = _repository.GetAsQueryable<Company>()
                .Where(p => p.CorporationId == corporation.Id)
                .Select(p => p.CompanyId)
                .ToList();

            var dialogues = _repository.GetAsQueryable<Dialogue>()
                .Include(p => p.ApplicationUser)
                .Where(p => p.BegTime >= new DateTime(2020, 08, 11)
                    && p.EndTime <= new DateTime(2020, 08, 17)
                    && p.StatusId == 3
                    && p.InStatistic == true)
                .Where(p => (!companyIds.Any() || companyIds.Contains(p.Device.CompanyId))
                    && p.ApplicationUserId != null)
                    // && (!deviceIds.Any() || deviceIds.Contains(p.DeviceId)))
                .ToList();
            var dialogueIds = dialogues.Select(p => p.DialogueId)
                .ToList();

            var phrases = _repository.GetAsQueryable<DialoguePhrase>()
                    .Where(p => p.PhraseId != null
                    // && (!phraseIds.Any() || phraseIds.Contains((Guid)p.PhraseId))
                    && (!dialogueIds.Any() || dialogueIds.Contains((Guid)p.DialogueId)))
                    .ToList();
            
            var result = dialogues.GroupBy(p => p.ApplicationUserId)
                .Select(p => new UserSalesStagePhrase
                    {
                        ApplicationUserId = (Guid)p.Key,
                        FullName = p.FirstOrDefault().ApplicationUser.FullName,
                        SalesStagePhrases = GetSalesStagePhrases(p, phrases, corporation.Id)
                    })
                .ToList();
            return result;
        }
        private List<SalesStagePhraseModel> GetSalesStagePhrases(IGrouping<Guid?, Dialogue> dialogueGroup, List<DialoguePhrase> dialoguePhrases, Guid corporationId)
        {
            var dialogueIds = dialogueGroup.Select(p => p.DialogueId)
                .ToList();
            var phrases = dialoguePhrases.Where(p => dialogueIds.Contains((Guid)p.DialogueId))
                .ToList();
            var phrasesSalesStages = _repository.GetAsQueryable<SalesStagePhrase>()
                .Where(x =>
                // (!salesStageIds.Any() || salesStageIds.Contains((Guid)x.SalesStageId))
                (x.CorporationId == corporationId))
                .GroupBy(x => x.SalesStageId)
                .Select(k => new SalesStagePhraseModel
                {
                    Count = CountPhrasesAmount(phrases, k),
                    SalesStageId = k.Key,
                    SalesStageName = k.FirstOrDefault().SalesStage.Name,
                    SequenceNumber = k.FirstOrDefault().SalesStage.SequenceNumber
                }).ToList();

            var dialogueCount = 0;
            foreach (var item in phrasesSalesStages)
            {
                dialogueCount = dialogueIds.Count();
                item.PercentageOfExecution = dialogueCount == 0? 0: (double)item.Count / dialogueCount;
            }
            return phrasesSalesStages;
        }
        private int CountPhrasesAmount(List<DialoguePhrase> dialoguePhr, IGrouping<Guid, SalesStagePhrase> ssPhr)
        {
            var count = dialoguePhr
                                  .Where(p => ssPhr.Select(s => s.PhraseId)
                                  .Contains((Guid)p.PhraseId)
                                  )
                                  .Select(p => p.DialogueId).Distinct().Count();
            return count;
        }
        private Cell ConstructCell(string value, CellValues dataType)
        {
            return new Cell()
            {
                CellValue = new CellValue(value),
                DataType = new EnumValue<CellValues>(dataType)
            };
        }   
        [HttpPost("[action]")]
        public async Task SendFramesToFaceAnalyze()
        {
            // var corporation = _repository.GetAsQueryable<Corporation>()
            //         .FirstOrDefault(p => p.Name == "UBRIR");

            // var companyIds = _repository.GetAsQueryable<Company>()
            //     .Where(p => p.CorporationId == corporation.Id)
            //     .Select(p => p.CompanyId)
            //     .ToList();

            var frames = _repository.GetAsQueryable<FileFrame>()
                .Include(p => p.FrameAttribute)
                .Where(p => p.Time.Date == new DateTime(2020, 08, 17)
                    && p.FaceLength == 0
                    && p.FrameAttribute.FirstOrDefault().Descriptor == null)
                .OrderBy(p => p.Time)
                .ToList();
            
            System.Console.WriteLine($"framesCount: {frames.Count}");
            
            var counter = 0;
            var counter2 = 0;
            foreach(var f in frames)
            {
                var url = $"https://heedbookapi.northeurope.cloudapp.azure.com/user/Face";
                
                var model = new FaceAnalyzeRun()
                {
                    Path = $"{f.FileContainer}/{f.FileName}"  
                };
                var modelJson = JsonConvert.SerializeObject(model);
                var data = Encoding.ASCII.GetBytes(modelJson);

                var request1 = WebRequest.Create(url);
                request1.Credentials = CredentialCache.DefaultCredentials;
                request1.Method = "POST";
                request1.ContentType = "application/json-patch+json";
                request1.ContentLength = data.Length;
                
                using(var stream = request1.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var responce = request1.GetResponseAsync();
                System.Console.WriteLine($"{counter2}-{responce != null}");          

                counter++;
                counter2++;
                Thread.Sleep(100);
                if(counter >=10)
                {
                    counter = 0;
                    Thread.Sleep(200);
                }
            }
            System.Console.WriteLine($"done");
        } 
        [HttpPost("[action]")]
        public async Task RemoveDialogueIntersection()
        {
            var dialoguesApplicationUserGroup = _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.BegTime.Date >= new DateTime(2020, 09, 05).Date
                    && p.BegTime.Date <= new DateTime(2020, 09, 07).Date
                    && p.StatusId == 3)
                .GroupBy(p => p.ApplicationUserId)
                .ToList();
            System.Console.WriteLine($"dialoguesCount: {dialoguesApplicationUserGroup.Count}");

            foreach(var userDialogues in dialoguesApplicationUserGroup)
            {
                foreach(var d  in userDialogues)
                {
                    try
                    {
                        var intersectDialogues = userDialogues.Where(p =>
                            ((p.BegTime <= d.BegTime
                                && p.EndTime > d.BegTime
                                && p.EndTime < d.EndTime) 
                            || (p.BegTime < d.EndTime
                                && p.BegTime > d.BegTime
                                && p.EndTime >= d.EndTime)
                            || (p.BegTime >= d.BegTime
                                && p.EndTime <= d.EndTime)
                            || (p.BegTime < d.BegTime
                                && p.EndTime > d.EndTime))
                            && p.StatusId == 3)
                        .ToList();
                        var dwmd = intersectDialogues.Aggregate((p1, p2) => 
                                p1.EndTime.Subtract(p1.BegTime) > p2.EndTime.Subtract(p2.BegTime) 
                                ? p1 
                                : p2);
                        var allOtherDialogues = intersectDialogues.Where(p => p.DialogueId != dwmd.DialogueId)
                            .ToList();
                        allOtherDialogues.ForEach(p => p.StatusId = 8);
                        // allOtherDialogues.Select(p =>
                        //     {
                        //         p.StatusId = 8;
                        //         return p;
                        //     });
                        System.Console.WriteLine($"{dwmd.DialogueId} {dwmd.BegTime} {dwmd.EndTime} {dwmd.StatusId}");
                        foreach(var di in allOtherDialogues)
                        {
                            System.Console.WriteLine($"{di.DialogueId} {di.BegTime} {di.EndTime} {di.StatusId}");
                        }
                        System.Console.WriteLine();
                    }
                    catch(Exception e)
                    {
                        
                    }
                }
                System.Console.WriteLine();
            }
            _repository.Save();
            System.Console.WriteLine($"done");
        }
        [HttpPost("[action]")]
        public async Task<string> SendGifsCreateCommand()
        {
            var files = await _sftpClient.GetAllFilesDataRecursively("media");
            var filteredFiles = files.Where(p => p.FullName.Split(".").Last() == "mkv" || p.FullName.Split(".").Last() == "mp4")
                .Select(p => p.FullName.Replace($"/home/nkrokhmal/storage/", ""));
            var path = $"";
            foreach(var f in filteredFiles)
            {
                System.Console.WriteLine($"{f}");
                await SendMessageGif($"{f}");
            }
                
            // if(fileInfo.Extension == ".mkv" || fileInfo.Extension == ".mp4")
            //     await SendMessageCreateGif($"{path}");
            System.Console.WriteLine($"done");
            return await Task.FromResult("Ok");
        }
        private async Task SendMessageGif(string fileName)
        {
            var url = $"https://heedbookapi.northeurope.cloudapp.azure.com/user/VideoToSound/VideoToGif";
                
            var request = WebRequest.Create(url);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";                

            var model = new VideoContentToGifRun
            {
                Path = $"{fileName}"                    
            };
            var json = JsonConvert.SerializeObject(model);
            var data = Encoding.ASCII.GetBytes(json);

            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = request.GetResponseAsync();
        }
        [HttpPost("[action]")]
        public async Task RecognizeDialogues()
        {
            var dialogues = _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.StatusId == 3
                    && p.DialogueWord.Count() == 0
                    && p.BegTime.Date >= new DateTime(2020, 08, 17).Date
                    && p.BegTime.Date < new DateTime(2020, 08, 20).Date)
                .OrderBy(p => p.BegTime)
                .ToList();
            System.Console.WriteLine($"dialoguesCount: {dialogues.Count}");
            
            foreach(var d in dialogues)
            {
                var url = $"https://heedbookapi.northeurope.cloudapp.azure.com/user/AudioAnalyze/audio-analyze";
                var model = new AudioAnalyzeRun()
                {
                    Path = $"dialogueaudios/{d.DialogueId}.wav"
                };
                var modelByteArray = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(model));

                var request = WebRequest.Create(url);
                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "POST";
                request.ContentType = "application/json-patch+json";   

                using(var stream = request.GetRequestStream())
                {
                    stream.Write(modelByteArray, 0, modelByteArray.Length);
                }
                var responce = request.GetResponseAsync();
                System.Console.WriteLine($"{d.BegTime}- {d.DialogueId} - {responce.Result}");
            }
            System.Console.WriteLine($"done");
        }
        [HttpPost("[action]")]
        public async Task DateTimeExample()
        {
            System.Console.WriteLine($"12345");

            var times = new List<DateTime>()
            {
                new DateTime(2020, 08, 25, 13, 50, 00),
                new DateTime(2020, 08, 25, 13, 51, 00),
                new DateTime(2020, 08, 25, 13, 52, 00),
                new DateTime(2020, 08, 25, 13, 53, 00),
                new DateTime(2020, 08, 25, 13, 54, 00),
                new DateTime(2020, 08, 25, 13, 55, 00)
            };
            // times.ForEach(p => p.AddSeconds(15));
            times = times.Select(p => {p = p.AddSeconds(15); return p;}).ToList();



            
            foreach(var t in times)
                System.Console.WriteLine(t.ToString("yyyy MM dd hh mm ss"));
            System.Console.WriteLine($"done");
        }
        [HttpPost("[action]")]
        public async Task UbrirCompanyDialogues()
        {
            var corporation = _repository.GetAsQueryable<Corporation>()
                    .FirstOrDefault(p => p.Name == "UBRIR");
            var companys = _repository.GetAsQueryable<Company>()
                .Where(p => p.CorporationId == corporation.Id)
                .ToList();
            var companyIds = _repository.GetAsQueryable<Company>()
                .Where(p => p.CorporationId == corporation.Id)
                .Select(p => p.CompanyId)
                .ToList();
            var devices = _repository.GetAsQueryable<Device>()
                .Where(p => companyIds.Contains(p.CompanyId))
                .ToList();
            var deviceIds = _repository.GetAsQueryable<Device>()
                .Where(p => companyIds.Contains(p.CompanyId))
                .Select(p => p.DeviceId)
                .ToList();

            var dialogues = _repository.GetAsQueryable<Dialogue>()
                .Where(p => p.StatusId == 3
                    && deviceIds.Contains(p.DeviceId)
                    && p.BegTime >= new DateTime(2020, 09, 04, 04, 00, 00)
                    && p.BegTime <= new DateTime(2020, 09, 04, 07, 00, 00))
                .AsQueryable();
            
            System.Console.WriteLine($"amountOfDialogueCount: {dialogues.Count()}");

            // var report = dialogues.GroupBy(p => p.Device.CompanyId)
            //     .Select(p => new CompanyReport
            //         {
            //             CompanyId = p.Key,
            //             devices = p.GroupBy(q => q.Device.DeviceId)
            //                 .Select(q => new DeviceReport
            //                     {
            //                         DeviceId = q.Key,
            //                         CountOfDialogues = q.Count()
            //                     })
            //                 .ToList()
            //         })
            //     .ToList();

            // System.Console.WriteLine($"report:\n{JsonConvert.SerializeObject(report)}");
            // foreach(var c in report)
            // {
            //     var comp = companys.FirstOrDefault(p => p.CompanyId == c.CompanyId);
            //     System.Console.WriteLine($"{comp.CompanyName}");
            //     foreach(var d in c.devices)
            //     {
            //         var dev = devices.FirstOrDefault(p => p.DeviceId == d.DeviceId);
            //         System.Console.WriteLine($"{dev.Name} {d.CountOfDialogues}");
            //     }
            // }

            foreach(var c in companys)
            {
                System.Console.WriteLine(c.CompanyName);
                var compDevices = devices.Where(p => p.CompanyId == c.CompanyId).ToList();
                foreach(var d in compDevices)
                {
                    var devDialogues = dialogues.Where(p => p.DeviceId == d.DeviceId).ToList();
                    System.Console.WriteLine($"{d.Name} {devDialogues.Count}");
                }
            }
            System.Console.WriteLine($"done");
        }
        [HttpPost("[action]")]
        public async Task CheckFramesFromVideos()
        {
            var frames = _repository.GetAsQueryable<FileFrame>()
                .Where(p => p.Time > new DateTime(2020, 09, 05, 07, 00, 00)
                    && p.Time < new DateTime(2020, 09, 07, 09, 00, 00))
                .ToList();
            var videos = _repository.GetAsQueryable<FileVideo>()
                .Where(p => p.BegTime > new DateTime(2020, 09, 05, 07, 00, 00)
                    && p.BegTime < new DateTime(2020, 09, 07, 09, 00, 00))
                .OrderBy(p => p.BegTime)
                .ToList();
            var counter = 0;
            List<FileVideo> prepareVideo = new List<FileVideo>();
            foreach(var v in videos)
            {
                var videoFrames = frames.Where(p => p.DeviceId == v.DeviceId
                        && p.Time >= v.BegTime
                        && p.Time <= v.EndTime)
                    .ToList();
                
                if(!videoFrames.Any())
                {
                    prepareVideo.Add(v);
                    counter++;                    
                }
            }
            System.Console.WriteLine($"amount of videos: {counter}");

            var counter2 = 0;
            var url = $"https://heedbookapi.northeurope.cloudapp.azure.com/user/FramesFromVideo";
            foreach(var v in prepareVideo)
            {
                var model = new VideoToSoundRun
                {
                    Path = $"videos/{v.FileName}"
                };                

                var request = WebRequest.Create(url);
                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "POST";
                request.ContentType = "application/json-patch+json";                

                var json = JsonConvert.SerializeObject(model);
                var data = Encoding.ASCII.GetBytes(json);

                using(var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
                var responce = request.GetResponseAsync();
                System.Console.WriteLine($"{counter2}-{counter} {responce}");
                counter2++;
            }
            System.Console.WriteLine($"videos without frame: {counter}");
        }
        [HttpPost("[action]")]
        public async Task CompareFrames()
        {
            var frame1 = _repository.GetAsQueryable<FileFrame>()
                .Include(p => p.FrameAttribute)
                .FirstOrDefault(p => p.FileFrameId == Guid.Parse("99e89ac4-710c-415f-b633-f07e769f6646"));
            var frame2 = _repository.GetAsQueryable<FileFrame>()
                .Include(p => p.FrameAttribute)
                .FirstOrDefault(p => p.FileFrameId == Guid.Parse("3fd06ccf-2073-49f4-b012-55dd7372584e"));
            var cos = _calc.Cos(frame1.FrameAttribute.FirstOrDefault().Descriptor, frame2.FrameAttribute.FirstOrDefault().Descriptor);
        
            System.Console.WriteLine($"cos: {cos}");
            System.Console.WriteLine("done");
        }
    }
    public class CompanyReport
    {
        public Guid CompanyId { get; set; }
        public List<DeviceReport> devices { get; set; }
    }
    public class DeviceReport
    {
        public Guid DeviceId { get; set; }
        public int CountOfDialogues { get; set; }
    }
    public class UserSalesStagePhrase
    {
        public Guid ApplicationUserId { get; set; }
        public string FullName { get; set; }
        public List<SalesStagePhraseModel> SalesStagePhrases { get; set; }
    }
     public class SalesStagePhraseModel
    {
        public Guid SalesStageId;
        public int SequenceNumber;
        public string SalesStageName;
        public int Count;
        public double PercentageOfExecution;
    }
    public class PraseTableresult
    {
        public string Phrase { get; set; }
        public Guid PhraseId { get; set; }
        public string PopularName { get; set; }
        public string PhraseType { get; set; }
        public double Percent { get; set; }
        public double Freq { get; set; }
    }
    public class ReportWords
    {
        public string Name { get; set; }
        public List<string> FirstStageWords { get; set; }
        public List<string> SecondStageWords { get; set; }
        public List<WordStat> SecondWordStat { get; set; }
    }
    public class WordStat
    {
        public string Name { get; set; }
        public int UsedCount { get; set; }
    }
    public class RecognitionWords
    {
        public string Word { get; set; }
        public string BegTime { get; set; }
        public string EndTime { get; set; }
        public string PhraseId { get; set; }
        public string PhraseTypeId { get; set; }
        public int Position { get; set; }
    }
    public class LibraryItem
    {
        public string Phrase { get; set; }
        public string PhraseType { get; set; }
    }   
    public class PhrasesInfo
    {
        public Boolean IsClient;
        public string FullName;
        public Guid? ApplicationUserId;
        public Guid? DialogueId;
        public Guid? PhraseId;
        public string PhraseText;
        public string PhraseTypeText;
    } 
}


      