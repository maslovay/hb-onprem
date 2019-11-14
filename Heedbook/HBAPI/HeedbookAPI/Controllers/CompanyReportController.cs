using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using HBData.Models;
using HBData.Models.AccountViewModels;
using UserOperations.Services;
using UserOperations.AccountModels;
using HBData;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Annotations;
using HBLib.Utils;
using FileResult = Microsoft.AspNetCore.Mvc.FileResult;

namespace UserOperations.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyReportController : Controller
    {
        private readonly RecordsContext _context;
        private readonly ILoginService _loginService;
        private readonly ElasticClient _log;

        public CompanyReportController(
            RecordsContext context,
            ILoginService loginService,
            ElasticClient log
            )
        {
            _context = context;
            _loginService = loginService;
            _log = log;
        }
   

        [AllowAnonymous]
        [HttpGet("GetReport")]
        [SwaggerOperation(Summary = "Report about dialogues", Description = "For not loggined users")]        
        [SwaggerResponse(200, "Report constructed")]
        public FileResult GetReport([FromQuery(Name = "begTime")] string beg,
            [FromQuery(Name = "isTeacherReport")] bool isTeacherReport,
            [FromQuery(Name = "companyId"), SwaggerParameter("list guids, if not passed - takes from token")] List<Guid> companyIds)
        {
            try
            {
                var stringFormat = "yyyyMMddHHmmss";
                var begTime = !String.IsNullOrEmpty(beg) ? DateTime.ParseExact(beg, stringFormat, CultureInfo.InvariantCulture) : DateTime.Now.AddDays(-1);
                System.Console.WriteLine($"{begTime}");

                companyIds = companyIds.Any() ? companyIds : _context.Companys.Select(p=>p.CompanyId).ToList();

                DialogueReport(begTime, companyIds, isTeacherReport);

                var dataBytes = System.IO.File.ReadAllBytes("DialogueReport.xlsx");
                System.IO.File.Delete("DialogueReport.xlsx");

                var dataStream = new MemoryStream(dataBytes);
                var fileType = "application/xlsx";
                var fileName = "DialogueReport.xlsx";
                return File(dataStream, fileType, fileName);
            }
            catch (Exception e)
            {
                throw e;
            }
        }  


        private void DialogueReport(DateTime beginTime, List<Guid> companyIds, bool isTeacherReport = false)
        {      
            var managerRoleId = _context.Roles.Where(p => p.NormalizedName == "MANAGER").FirstOrDefault().Id;
            var users = _context.ApplicationUsers
                .Include(p => p.UserRoles)
                .Where(p => companyIds.Contains((Guid) p.CompanyId))
                .ToList();

            var dialogues = _context.Dialogues
                .Include(p => p.ApplicationUser)
                .Include(p => p.ApplicationUser.Company)
                .Include(p => p.ApplicationUser.WorkerType)
                .Include(p => p.DialogueAudio)
                .Include(p => p.Language)
                .Include(p => p.DialogueClientSatisfaction)
                .Include(p => p.DialogueClientProfile)
                .Include(p => p.DialogueHint)
                .Include(p => p.DialogueInterval)
                .Include(p => p.DialogueSpeech)
                .Include(p => p.DialogueVisual)
                .Include(p => p.DialoguePhrase).ThenInclude(p => p.Phrase).ThenInclude(p => p.PhraseType)
                .Include(p => p.DialogueWord)
                .Where(p => (!isTeacherReport) || (p.DialogueClientSatisfaction.Average(q => q.MeetingExpectationsByTeacher) != 0))
                .Where(p => p.BegTime > beginTime
                    && p.StatusId == 3
                    && companyIds.Contains((Guid)p.ApplicationUser.CompanyId))
                .Select(p => new DialogueReportModel
                    {
                        FullName = p.ApplicationUser.FullName,
                        Email = p.ApplicationUser.Email,
                        ManagerName = GetManager(users, p.ApplicationUser, managerRoleId),
                        CompanyName = p.ApplicationUser.Company.CompanyName,
                        CompanyId = (Guid)p.ApplicationUser.CompanyId,
                        DialogueId = p.DialogueId,
                        EmployeeEmployeeId = p.ApplicationUserId,
                        EmployeeWorkerTypeName = p.ApplicationUser.WorkerType.WorkerTypeName,
                        BeginTime = p.BegTime.AddHours(3),
                        EndTime = p.EndTime.AddHours(3),
                        InStatistic = p.InStatistic,
                        SatisfactionMeetingExpectationsTotal = p.DialogueClientSatisfaction.Average(s => s.MeetingExpectationsTotal).ToString(),
                        SatisfactionBegMoodTotal = p.DialogueClientSatisfaction.Average(s => s.BegMoodTotal).ToString(),
                        SatisfactionEndMoodTotal = p.DialogueClientSatisfaction.Average(s => s.EndMoodTotal).ToString(),
                        SatisfactionMeetingExpectationsByClient = p.DialogueClientSatisfaction.Average(s => s.MeetingExpectationsByClient).ToString(),
                        SatisfactionMeetingExpectationsByEmpoyee = p.DialogueClientSatisfaction.Average(s => s.MeetingExpectationsByEmpoyee).ToString(),
                        BegMoodByTeacher = p.DialogueClientSatisfaction.Average(s => s.BegMoodByTeacher).ToString(),
                        EndMoodByTeacher = p.DialogueClientSatisfaction.Average(s => s.EndMoodByTeacher).ToString(),
                        TotalMoodByTeacher = p.DialogueClientSatisfaction.Average(s => s.MeetingExpectationsByTeacher).ToString(),
                        SatisfactionDiff = p.DialogueClientSatisfaction.Average(s => s.EndMoodTotal - s.BegMoodTotal).ToString(),
                        Language = "Russian",
                        Hints = JsonConvert.SerializeObject(p.DialogueHint.Select(s => s.HintText)),
                        ClientAge = Math.Round((double) p.DialogueClientProfile.Average(s => s.Age)).ToString(),
                        ClientGender = p.DialogueClientProfile.FirstOrDefault().Gender,
                        VisualsAttention = Math.Round((double) p.DialogueVisual.Average(s => s.AttentionShare)).ToString(),
                        VisualsContempt = p.DialogueVisual.Average(s => s.ContemptShare).ToString(),
                        VisualsDisgust = p.DialogueVisual.Average(s => s.DisgustShare).ToString(),
                        VisualsFear = p.DialogueVisual.Average(s => s.FearShare).ToString(),
                        VisualsHappiness = p.DialogueVisual.Average(s => s.HappinessShare).ToString(),
                        VisualsNeutral = p.DialogueVisual.Average(s => s.NeutralShare).ToString(),
                        VisualsSurprise = p.DialogueVisual.Average(s => s.SurpriseShare).ToString(),
                        VisualsSadness = p.DialogueVisual.Average(s => s.SadnessShare).ToString(),
                        VisualsAnger = p.DialogueVisual.Average(s => s.AngerShare).ToString(),
                        AudioNegativeTone = p.DialogueAudio.Average(s => s.NegativeTone).ToString(),
                        AudioNeutralTone = p.DialogueAudio.Average(s => s.NeutralityTone).ToString(),
                        AudioPositiveTone = p.DialogueAudio.Average(s => s.PositiveTone).ToString(),
                        SpeechesPositiveShare = p.DialogueSpeech.Average(s => s.PositiveShare).ToString(),
                        SpeechesSilenceShare = p.DialogueSpeech.Average(s => s.SilenceShare).ToString(),
                        SpeechesSpeechSpeed = p.DialogueSpeech.Average(s => s.SpeechSpeed).ToString(),
                        PhrasesPhraseText = JsonConvert.SerializeObject(p.DialoguePhrase.Select(s => new 
                                {
                                    phraseText = s.Phrase.PhraseText,
                                    IsClient = s.Phrase.IsClient
                                })),
                        WordsWord = p.DialogueWord.FirstOrDefault() != null ? p.DialogueWord.FirstOrDefault().Words : "[]", 
                        Video = $"{p.DialogueId}.mkv",
                        Avatar = p.DialogueClientProfile.FirstOrDefault(s => s.Avatar != null).Avatar,
                        Cross = ListToText(p.DialoguePhrase.Where(q => q.PhraseType.PhraseTypeText == "Cross")
                            .Select(q => q.Phrase.PhraseText).ToList()),
                        Necessary = ListToText(p.DialoguePhrase.Where(q => q.PhraseType.PhraseTypeText == "Necessary")
                            .Select(q => q.Phrase.PhraseText).ToList()),
                        Loyalty = ListToText(p.DialoguePhrase.Where(q => q.PhraseType.PhraseTypeText == "Loyalty")
                            .Select(q => q.Phrase.PhraseText).ToList()),
                        Alert = ListToText(p.DialoguePhrase.Where(q => q.PhraseType.PhraseTypeText == "Alert")
                            .Select(q => q.Phrase.PhraseText).ToList()),
                        Fillers = ListToText(p.DialoguePhrase.Where(q => q.PhraseType.PhraseTypeText == "Fillers")
                            .Select(q => q.Phrase.PhraseText).ToList())
                    })
                .ToList(); 
        

            dialogues.ForEach(p => 
                p.WordsWord = (p.WordsWord == "[]") ? 
                    "" : 
                    string.Join(
                        " ",
                        JsonConvert.DeserializeObject<List<WordsClass>>(p.WordsWord).Select(q => q.Word)));      
            
            PrintDialogueReport(dialogues);
        }

        private string GetManager(List<ApplicationUser> users, ApplicationUser applicationUser, Guid managerRoleId)
        {
            return users.Where(p => p.CompanyId == applicationUser.CompanyId)
                .Where(p => p.UserRoles.First().RoleId == managerRoleId).First().FullName;

        }

        private void PrintDialogueReport(List<DialogueReportModel> DialogueReports)
        {
            using (SpreadsheetDocument document = SpreadsheetDocument.Create("DialogueReport.xlsx", SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());               

                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "HeedbookStatisticForLast10Days" };
                
                sheets.Append(sheet);                

                SheetData sheetData = worksheetPart.Worksheet.AppendChild(new SheetData());
                Row row1 = new Row();

                row1.Append(         
                    ConstructCell("CompanyName", CellValues.String),           
                    ConstructCell("DialogueId", CellValues.String),
                    ConstructCell("EmployeeId", CellValues.String),
                    ConstructCell("Email", CellValues.String), 
                    ConstructCell("Manager Name", CellValues.String),                                       
                    ConstructCell("BeginTime", CellValues.String),
                    ConstructCell("EndTime", CellValues.String),
                    ConstructCell("InStatistic", CellValues.String),
                    ConstructCell("Satisfaction MeetingExpectationsTotal", CellValues.String),

                    ConstructCell("Client Age", CellValues.String),
                    ConstructCell("Client Gender", CellValues.String),

                    ConstructCell("Satisfaction BegMoodTotal", CellValues.String),
                    ConstructCell("Satisfaction EndMoodTotal", CellValues.String),
                    ConstructCell("Satisfaction MeetingExpectationsByClient", CellValues.String),
                    ConstructCell("Satisfaction MeetingExpectationsByEmpoyee", CellValues.String),

                    ConstructCell("Satisfaction BegMoodByTeacher", CellValues.String),
                    ConstructCell("Satisfaction EndMoodByTeacher", CellValues.String),
                    ConstructCell("Satisfaction TotalMoodByTeacher", CellValues.String),

                    ConstructCell("Satisfaction Diff", CellValues.String),

                    ConstructCell("Language", CellValues.String),
                    ConstructCell("Hints", CellValues.String),

                    ConstructCell("Visuals Attention", CellValues.String),
                    ConstructCell("Visuals Contempt", CellValues.String),
                    ConstructCell("Visuals Disgust", CellValues.String),
                    ConstructCell("Visuals Fear", CellValues.String),
                    ConstructCell("Visuals Happiness", CellValues.String),
                    ConstructCell("Visuals Neutral", CellValues.String),
                    ConstructCell("Visuals Surprise", CellValues.String),
                    ConstructCell("Visuals Sadness", CellValues.String),
                    ConstructCell("Visuals Anger", CellValues.String),
                    ConstructCell("Audio NegativeTone", CellValues.String),
                    ConstructCell("Audio NeutralTone", CellValues.String),
                    ConstructCell("Audio PositiveTone", CellValues.String),
                    ConstructCell("Speeches PositiveShare", CellValues.String),
                    ConstructCell("Speeches SilenceShare", CellValues.String),
                    ConstructCell("Speeches SpeechSpeed", CellValues.String),
                  //  ConstructCell("Phrases PhraseText", CellValues.String),  
                    ConstructCell("Words Word", CellValues.String),
                    ConstructCell("Necessary", CellValues.String),
                    ConstructCell("Loyalty", CellValues.String),  
                    ConstructCell("Alert", CellValues.String),
                    ConstructCell("Cross", CellValues.String),
                    ConstructCell("Fillers", CellValues.String),
                    
                    ConstructCell("Video", CellValues.String),
                    ConstructCell("Avatar", CellValues.String)
                );
                sheetData.AppendChild(row1);

                Row tempRow;
                foreach(var dr in DialogueReports)
                {
                    tempRow = new Row();
                    tempRow.Append(
                        ConstructCell(dr.CompanyName.ToString(), CellValues.String),
                        ConstructCell(dr.DialogueId.ToString(), CellValues.String),
                        ConstructCell(dr.FullName.ToString(), CellValues.String),
                        ConstructCell(dr.Email.ToString(), CellValues.String),
                        ConstructCell(dr.ManagerName.ToString(), CellValues.String),                           
                        ConstructCell(dr.BeginTime.ToString(), CellValues.String),
                        ConstructCell(dr.EndTime.ToString(), CellValues.String),
                        ConstructCell(dr.InStatistic.ToString(), CellValues.String),
                        ConstructCell(dr.SatisfactionMeetingExpectationsTotal, CellValues.String),

                        ConstructCell(dr.ClientAge, CellValues.String),
                        ConstructCell(dr.ClientGender, CellValues.String),

                        ConstructCell(dr.SatisfactionBegMoodTotal, CellValues.String),
                        ConstructCell(dr.SatisfactionEndMoodTotal, CellValues.String),
                        ConstructCell(dr.SatisfactionMeetingExpectationsByClient, CellValues.String),
                        ConstructCell(dr.SatisfactionMeetingExpectationsByEmpoyee, CellValues.String),

                        ConstructCell(dr.BegMoodByTeacher, CellValues.String),
                        ConstructCell(dr.EndMoodByTeacher, CellValues.String),
                        ConstructCell(dr.TotalMoodByTeacher, CellValues.String),

                        ConstructCell(dr.SatisfactionDiff, CellValues.String),

                        ConstructCell(dr.Language, CellValues.String),
                        ConstructCell(dr.Hints, CellValues.String),

                        ConstructCell(dr.VisualsAttention, CellValues.String),
                        ConstructCell(dr.VisualsContempt, CellValues.String),
                        ConstructCell(dr.VisualsDisgust, CellValues.String),
                        ConstructCell(dr.VisualsFear, CellValues.String),
                        ConstructCell(dr.VisualsHappiness, CellValues.String),
                        ConstructCell(dr.VisualsNeutral, CellValues.String),
                        ConstructCell(dr.VisualsSurprise, CellValues.String),
                        ConstructCell(dr.VisualsSadness, CellValues.String),
                        ConstructCell(dr.VisualsAnger, CellValues.String),
                        ConstructCell(dr.AudioNegativeTone, CellValues.String),
                        ConstructCell(dr.AudioNeutralTone, CellValues.String),
                        ConstructCell(dr.AudioPositiveTone, CellValues.String),
                        ConstructCell(dr.SpeechesPositiveShare, CellValues.String),
                        ConstructCell(dr.SpeechesSilenceShare, CellValues.String),
                        ConstructCell(dr.SpeechesSpeechSpeed, CellValues.String),
                        // ConstructCell(dr.PhrasesPhraseText, CellValues.String),
                        ConstructCell(dr.WordsWord, CellValues.String),
                        ConstructCell(dr.Necessary, CellValues.String),
                        ConstructCell(dr.Loyalty, CellValues.String),
                        ConstructCell(dr.Alert, CellValues.String),
                        ConstructCell(dr.Cross, CellValues.String),
                        ConstructCell(dr.Fillers, CellValues.String),
                        
                        ConstructCell(dr.Video, CellValues.String),
                        ConstructCell(dr.Avatar, CellValues.String)
                    );
                    sheetData.AppendChild(tempRow);
                }
                workbookPart.Workbook.Save();                
            }
        }
        private Cell ConstructCell(string value, CellValues dataType)
        {
            return new Cell()
            {
                CellValue = new CellValue(value),
                DataType = new EnumValue<CellValues>(dataType)
            };
        }

        private string ListToText(List<string> ls)
        {
            var text = "";
            foreach (var l in  ls)
            {
                text += $"{l}, ";
            }
            return text;
        }        
    }  
    public class DialogueReportModel
    {
        public string FullName {get;set;}
        public string Email {get;set;}
        public string ManagerName {get;set;}
        public string CompanyName {get; set;}
        public Guid CompanyId {get; set;}
        public Guid DialogueId { get; set; }
        public Guid EmployeeEmployeeId { get; set; }
        public string EmployeeWorkerTypeName { get; set; }                   
        public DateTime BeginTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool InStatistic { get; set; }
        public string SatisfactionMeetingExpectationsTotal { get; set; }
        public string SatisfactionBegMoodTotal { get; set; }
        public string SatisfactionEndMoodTotal { get; set; }
        public string SatisfactionMeetingExpectationsByClient { get; set; }
        public string SatisfactionMeetingExpectationsByEmpoyee { get; set; }


        public string BegMoodByTeacher {get;set;}
        public string EndMoodByTeacher {get;set;}
        public string TotalMoodByTeacher {get;set;}


        public string SatisfactionDiff{get;set;}
        public string Language { get; set; }
        public string Hints { get; set; }
        public string ClientAge { get; set; }
        public string ClientGender { get; set; }
        public string VisualsAttention { get; set; }
        public string VisualsContempt { get; set; }
        public string VisualsDisgust { get; set; }
        public string VisualsFear { get; set; }
        public string VisualsHappiness { get; set; }
        public string VisualsNeutral { get; set; }
        public string VisualsSurprise { get; set; }
        public string VisualsSadness { get; set; }
        public string VisualsAnger { get; set; }
        public string AudioNegativeTone { get; set; }
        public string AudioNeutralTone { get; set; }
        public string AudioPositiveTone { get; set; }
        public string SpeechesPositiveShare { get; set; }
        public string SpeechesSilenceShare { get; set; }
        public string SpeechesSpeechSpeed { get; set; }
        public string PhrasesPhraseText { get; set; }
        public string PrasesIsClient { get; set; }
        public string WordsBegTime { get; set; }
        public string WordsEndTime { get; set; }
        public string WordsWord { get; set; }
        public string WordsIsClient { get; set; }
        public string Video { get; set; }
        public string Avatar { get; set; }
        public string Necessary {get;set;}
        public string Loyalty {get;set;}
        public string Alert {get;set;}
        public string Cross {get;set;}
        public string Fillers {get;set;}
    }   
}


public class WordsClass
{
    public string Word {get;set;}
    public DateTime BegTime {get;set;}
    public DateTime EndTime {get;set;}
    public string PhraseId {get;set;}
    public string PhraseTypeId {get;set;}
    public int Position {get;set;}
}


