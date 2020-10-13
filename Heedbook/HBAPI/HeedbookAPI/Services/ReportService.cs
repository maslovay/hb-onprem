using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using HBData.Models;
using HBData;
using HBLib.Utils;
using HBLib.Model;
using UserOperations.Utils;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Renci.SshNet.Common;
using HBData.Repository;
using UserOperations.Services.Interfaces;
using UserOperations.Utils.Interfaces;
using UserOperations.Models.Get.AnalyticSpeechController;
using UserOperations.Models.AnalyticModels;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace UserOperations.Services
{
    public class ReportService
    {
        private readonly IDBOperations _dbOperation;
        private readonly IGenericRepository _repository;

        public ReportService(
            IDBOperations dBOperations,
            IGenericRepository repository)
        {
            _dbOperation = dBOperations;
            _repository = repository;
        }
        public async Task GenerateReport(string fileName, string corporationName, string begTime, string endTime)
        {
            try
            {
                var timeFormat = "ddMMyyyy";

                var _begTime = DateTime.ParseExact(begTime, timeFormat, CultureInfo.InvariantCulture);
                var _endTime = DateTime.ParseExact(endTime, timeFormat, CultureInfo.InvariantCulture);            

                //UbrirCorporation b70306dc-8bb8-4b2d-a22c-004270711caf
                var corporation = _repository.GetAsQueryable<Corporation>()
                    .FirstOrDefault(p => p.Name == corporationName);
                
                

                if(corporation is null)
                {
                    throw new Exception("corporation is null");
                }

                var _corporationId = corporation.Id;

                var companys = _repository.GetAsQueryable<Company>()
                    .Where(p => p.CorporationId == corporation.Id)
                    .ToList();
                    
                var companyIds = companys
                    .Select(p => p.CompanyId)
                    .ToList();
                
                var users = _repository.GetAsQueryable<ApplicationUser>()
                    .Where(p => companyIds.Contains((Guid)p.CompanyId))
                    .ToList();
                
                var devices = _repository.GetAsQueryable<Device>()
                    .Where(p => companyIds.Contains(p.CompanyId))
                    .ToList();
                
                var deviceIds = devices
                    .Select(p => p.DeviceId)
                    .ToList();

                var dialogues = _repository.GetAsQueryable<Dialogue>()
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.Device)
                    .Include(p => p.DialogueWord)
                    .Include(p => p.DialogueClientSatisfaction)
                    .Include(p => p.DialogueFrame)
                    .Include(p => p.DialogueVisual)
                    .Include(p => p.DialogueAudio)
                    .Include(p => p.DialoguePhrase)
                    .Where(p => p.BegTime >= _begTime
                        && p.EndTime <= _endTime
                        // && deviceIds.Contains(p.DeviceId)
                        && deviceIds.Contains(p.DeviceId)
                        && p.StatusId == 3
                        && p.InStatistic == true)
                    .OrderBy(p => p.BegTime)
                    .ToList();            

                using (ExcellDocument document = new ExcellDocument(fileName))
                {
                    var workbookPart = document.AddWorkbookPart();
                    var worksheet = document.AddWorksheetPart(ref workbookPart);
                    var sheetData = document.AddSheet(ref workbookPart, worksheet, "Report");
                    var shareStringTablePart = workbookPart.AddNewPart<SharedStringTablePart>();
                    shareStringTablePart.SharedStringTable = new SharedStringTable();
                    // document.AddRow(sheetData, "f1", "f2", "f3");
                    // document.AddCell(sheetData, "HaHa", 4, 1);

                    List<CompanyReportModel> reports = new List<CompanyReportModel>();

                    reports.Add(GenerateReportForDialogues(dialogues, companyIds, users, devices, companys, _begTime, _endTime));

                    var groupingReports = dialogues.GroupBy(p => p.Device.CompanyId)
                        .Select(p => GenerateReportForDialogues(
                            p.ToList(), 
                            new List<Guid>{p.Key}, 
                            users, 
                            devices, 
                            companys,
                            _begTime,
                            _endTime))
                        .ToList();
                    reports.AddRange(groupingReports);

                    var userCrossSellingReports = UserCrossSellingRating(dialogues)
                        .OrderByDescending(p => p.CrossSellingRatio)
                        .ToList();

                    document.AddCell(ref sheetData, $"Период", 1, 1);
                    document.AddCell(ref sheetData, $"{_begTime} - {_endTime}", 1, 2);
                    document.AddCell(ref sheetData, $"Показатель", 2, 1);
                    document.AddCell(ref sheetData, $"Рейтинг офисов", 3, 1);
                    document.AddCell(ref sheetData, $"Общее число диалогов (в день на 1 рабочее место)", 4, 1);
                    document.AddCell(ref sheetData, $"Средняя продолжительность диалога", 5, 1);
                    document.AddCell(ref sheetData, $"Загруженность клиентской работой", 6, 1);
                    document.AddCell(ref sheetData, $"Удовлетворенность", 7, 1);
                    document.AddCell(ref sheetData, $"Улыбки и удивление клиентов", 8, 1);
                    document.AddCell(ref sheetData, $"Уровень внимания клиентов", 9, 1);
                    document.AddCell(ref sheetData, $"Положительные интонации в речи сотрудника", 10, 1);
                    document.AddCell(ref sheetData, $"Фразы лояльности в речи сотрудника", 11, 1);
                    document.AddCell(ref sheetData, $"Эмотивность речи сотрудника", 12, 1);
                    document.AddCell(ref sheetData, $"Рейтинг", 13, 1);
                    document.AddCell(ref sheetData, $"Лучший сотрудник по удовлетворенности", 14, 1);
                    document.AddCell(ref sheetData, $"Отстающий сотрудник по удовлетворенности", 15, 1);
                    document.AddCell(ref sheetData, $"Речевая аналитика", 17, 1);
                    document.AddCell(ref sheetData, $"Обязательные фразы", 18, 1);
                    document.AddCell(ref sheetData, $"Фразы лояльности (Желательные фразы)", 19, 1);
                    document.AddCell(ref sheetData, $"Фразы кросс-продаж", 20, 1);
                    document.AddCell(ref sheetData, $"Слова-паразиты", 21, 1);
                    document.AddCell(ref sheetData, $"Запрещенные слова", 22, 1);
                    document.AddCell(ref sheetData, $"Облако слов (топ-10) с выделением цветом \nпо категориям;", 23, 1);
                    document.AddCell(ref sheetData, $"Рейтинг", 25, 1);
                    document.AddCell(ref sheetData, $"Лучший сотрудник по кросс-продажам", 26, 1);
                    document.AddCell(ref sheetData, $"Отстающий сотрудник по кросс-продажам", 27, 1);
                    document.AddCell(ref sheetData, $"Загруженность", 29, 1);
                    document.AddCell(ref sheetData, $"Диалогов в день на офис", 30, 1);
                    document.AddCell(ref sheetData, $"Диалогов в день на сотрудника", 31, 1);
                    document.AddCell(ref sheetData, $"Средняя продолжительность диалога", 32, 1);
                    document.AddCell(ref sheetData, $"Популярное время", 33, 1);
                    document.AddCell(ref sheetData, $"Популярный день", 34, 1);
                    document.AddCell(ref sheetData, $"(в процентной доле диалогов со словами этой категории\nк общему количеству диалогов за период)", 36, 1);
                    document.AddCell(ref sheetData, $"Диалоги с кросс-продажами", 37, 1);
                    document.AddCell(ref sheetData, $"По сотрудникам", 39, 1);

                    for(int i = 0; i < reports.Count; i++)
                    {
                        document.AddCell(ref sheetData, $"{reports[i].CompanyName}", 2, 2 + i);
                        document.AddCell(ref sheetData, 
                            $"Удовлетворенность: {reports[i].Succesfull}\n"
                            + $"Число диалогов: {reports[i].NumberOfCompanyDialogues}", 3, 2 + i);
                        document.AddCell(ref sheetData, 
                            $"{reports[i].NumberOfCompanyDialogues}({reports[i].NumberOfDialoguesForOneDevice})", 4, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].AvgDialogueDuration.ToString("f")} мин", 5, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].Workload.ToString("f")} %", 6, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].Succesfull.ToString("f")} %", 7, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].SmilesAndCustomerSurprices.ToString("f")} %", 8, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].CustomerAttention.ToString("f")} %", 9, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].PositiveIntonationInEmployeeSpeech.ToString("f")} %", 10, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].LoyaltyPhrasesInEmployeeSpeech.ToString("f")} %", 11, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].EmotivnessOfEmployeeSpeech.ToString("f")} %", 12, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].BestEmployee}", 14, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].WorstEmployee}", 15, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].RequiredPhrases.ToString("f")} %", 18, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].LoyaltyPhrases.ToString("f")} %", 19, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].CrossSellingPhrases.ToString("f")} %", 20, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].ParasiteWords.ToString("f")} %", 21, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].ForbiddenWords.ToString("f")} %", 22, 2 + i);
                        document.AddCell(ref sheetData, reports[i].ColoredWordTexts, shareStringTablePart, 23, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].BestCrossSellingEmployee}", 26, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].WorstCrossSellingEmployee}", 27, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].DialoguesPerDayPerOffice}", 30, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].DialoguesPerDayPerEmployee}", 31, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].AvgDialogueDuration.ToString("f")} мин", 32, 2 + i);
                        document.AddCell(ref sheetData, $"{reports[i].CrossSellingPhrases.ToString("f")} %", 37, 2 + i);
                    }

                    for(int i = 0; i < userCrossSellingReports.Count; i++)
                    {
                        document.AddCell(ref sheetData, $"{userCrossSellingReports[i].UserName}", 40 + i, 1);
                        document.AddCell(ref sheetData, $"Всего {userCrossSellingReports[i].NumberOfDialogues} диалога(ов), "
                            + $"из которых {userCrossSellingReports[i].CrossSellingRatio.ToString("f")} % с кросс-продажей", 40 + i, 2);
                    }

                    document.SaveDocument(ref workbookPart);
                    document.CloseDocument();
                }
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
                throw;
            }
        }
        private List<EmployeeCrossSelling> UserCrossSellingRating(List<Dialogue> dialogues)
        {
            var phraseTypes = _repository.GetAsQueryable<PhraseType>()
                .ToList();
            var userRating = dialogues.Where(p => p.ApplicationUserId != null)
                .GroupBy(p => p.ApplicationUserId)
                .Select(p => new EmployeeCrossSelling
                    {
                        UserName = p.FirstOrDefault().ApplicationUser.FullName,
                        NumberOfDialogues = p.Count(),
                        CrossSellingRatio = phraseTypeDialogueRatio(p.ToList(), phraseTypes, "Cross")
                    })
                .ToList();
            return userRating;
        }
        private CompanyReportModel GenerateReportForDialogues(
            List<Dialogue> dialogues, 
            List<Guid> companyIds, 
            List<ApplicationUser> users,
            List<Device> devices,
            List<Company> companys,
            DateTime begTime,
            DateTime endTime)
        {
            var dialogueIds = dialogues.Select(q => q.DialogueId).ToList();
            var phraseTypes = _repository.GetAsQueryable<PhraseType>()
                .ToList();
            var report = new CompanyReportModel
            {
                CompanyName = companyIds.Count > 1 
                    ? companys.Select(c => c.CompanyName).ToList().Aggregate((c1, c2) => c1 + " " + c2)
                    : companys.FirstOrDefault(q => q.CompanyId == companyIds.FirstOrDefault()).CompanyName,
                NumberOfCompanyDialogues = dialogues.Count(),
                NumberOfDialoguesForOneDevice = (int)dialogues.GroupBy(q => q.Device.CompanyId)
                    .Select(p => 
                        p.ToList()
                        .GroupBy(q => q.BegTime.Date)
                        .Select(q => 
                            q.ToList()
                            .GroupBy(z => z.DeviceId)
                            .Select(z => z.Count())
                            .Average())
                        .Average())
                    .Average(),
                AvgDialogueDuration = dialogues.Average(q => q.EndTime.Subtract(q.BegTime).Minutes),
                Workload = CalculateWorkload(dialogues, companyIds, begTime, endTime),
                Succesfull = (double)dialogues.Average(q => q.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal),
                SmilesAndCustomerSurprices = (double)dialogues
                    .Select(q => q.DialogueFrame.Average(x => x.HappinessShare))
                    .Average(q => q)*100,
                CustomerAttention = (double)dialogues
                    .Select(q => q.DialogueVisual.Average(x => x.AttentionShare))
                    .Average(q => q),
                PositiveIntonationInEmployeeSpeech = (double)dialogues
                    .Select(q => q.DialogueAudio.Average(x => x.PositiveTone))
                    .Average(q => q)*100,
                LoyaltyPhrasesInEmployeeSpeech = (double)dialogues.GroupBy(q => q.DeviceId)
                    .Select(q => 
                        {
                            var countLoyaltyDialogues = q.Where(x => 
                                    x.DialoguePhrase.Any(z => 
                                            z.PhraseTypeId == phraseTypes.FirstOrDefault(c => c.PhraseTypeText == "Loyalty").PhraseTypeId))
                                .Count();
                            return (double)countLoyaltyDialogues/q.Count();
                        })
                    .Average(q => q)*100,
                EmotivnessOfEmployeeSpeech = 0,
                BestEmployee = dialogues.Where(q => q.ApplicationUserId != null)
                    .GroupBy(q => q.ApplicationUserId)
                    .Select(q => new EmployeeSatisfaction
                        {
                            UserName = q.FirstOrDefault().ApplicationUser.FullName,
                            Satisfaction = (double)q.Average(x => x.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal)
                        })
                    .Aggregate((i1,i2) => i1.Satisfaction > i2.Satisfaction ? i1 : i2).UserName,
                WorstEmployee = dialogues.Where(q => q.ApplicationUserId != null)
                    .GroupBy(q => q.ApplicationUserId)
                    .Select(q => new EmployeeSatisfaction
                        {
                            UserName = q.FirstOrDefault().ApplicationUser.FullName,
                            Satisfaction = (double)q.Average(x => x.DialogueClientSatisfaction.FirstOrDefault().MeetingExpectationsTotal)
                        })
                    .Aggregate((i1,i2) => i1.Satisfaction > i2.Satisfaction ? i2 : i1).UserName,
                RequiredPhrases = phraseTypeDialogueRatio(dialogues, phraseTypes, "Necessary"),
                LoyaltyPhrases = phraseTypeDialogueRatio(dialogues, phraseTypes, "Loyalty"),
                CrossSellingPhrases = phraseTypeDialogueRatio(dialogues, phraseTypes, "Cross"),
                ParasiteWords = phraseTypeDialogueRatio(dialogues, phraseTypes, "Fillers"),
                ForbiddenWords = phraseTypeDialogueRatio(dialogues, phraseTypes, "Alert"),
                ColoredWordTexts = companyIds.Count > 1 
                    ? GetPhraseCloud(dialogueIds) 
                    : GetParasiteAndFillerWords(dialogueIds, phraseTypes),
                BestCrossSellingEmployee = BestPhraseTypeEmployee(dialogues, phraseTypes, "Cross"),
                WorstCrossSellingEmployee = WorstPhraseTypeEmployee(dialogues, phraseTypes, "Cross"),
                DialoguesPerDayPerOffice = companyIds.Count > 1 ? dialogues.Count()/companys.Count : dialogues.Count(),
                DialoguesPerDayPerEmployee = (int)DialoguesPerDayPerEmployee(dialogues, companyIds, begTime, endTime),
            };
            return report;
        }
        private double DialoguesPerDayPerEmployee(List<Dialogue> dialogues, List<Guid> companyIds, DateTime begTime, DateTime endTime)
        {
            var totalDays = (int)endTime.Subtract(begTime).TotalDays;
            var result = dialogues.GroupBy(p => p.Device.CompanyId)
                .Select(p => 
                    p.ToList()
                    .GroupBy(q => q.BegTime.Date)
                    .Select(q => 
                        q.ToList()
                        .GroupBy(z => z.ApplicationUserId)
                        .Select(z => z.Count())
                        .Average())
                    .Average())
                .Average();
            return result;
        }
        private double CalculateWorkload(List<Dialogue> dialogues, List<Guid> companyIds, DateTime begTime, DateTime endTime)
        {
            int active = 3;
            var workingTimes = _repository.GetAsQueryable<WorkingTime>()
                .Where(x => !companyIds.Any() || companyIds.Contains(x.CompanyId))
                .ToArray();
            var dialogueInfos = dialogues
                .Where(p => companyIds.Contains(p.Device.CompanyId))
                .Select(p => new DialogueInfo
                    {
                        BegTime = p.BegTime,
                        EndTime = p.EndTime,
                        IsInWorkingTime = _dbOperation.CheckIfDialogueInWorkingTime(p, workingTimes.Where(x => x.CompanyId == p.Device.CompanyId).ToArray())
                    })
                .ToList();
            var devicesFiltered = _repository.GetAsQueryable<Device>()
                .Where(x => companyIds.Contains(x.CompanyId)
                    && x.StatusId == active)
                .ToList();
            var timeTableForDevices = _dbOperation.WorkingTimeDoubleList(workingTimes, begTime, endTime, companyIds, devicesFiltered, "customer");
            var workload = _dbOperation.WorklLoadByTimeIndex(timeTableForDevices, dialogueInfos, begTime, endTime);
            return (double)workload;
        }
        private List<ColoredText> GetParasiteAndFillerWords(List<Guid> dialogueIds, List<PhraseType> phraseTypes)
        {
            var parasiteWords = "Паразиты: " + phraseTypeDialogueWords(dialogueIds, phraseTypes, "Fillers");
            var forbiddenWords = "Запрещенные слова: " + phraseTypeDialogueWords(dialogueIds, phraseTypes, "Alert");
            var result = new List<ColoredText>
            {
                new ColoredText
                {
                    Text = parasiteWords,
                    Colour = phraseTypes.FirstOrDefault(p => p.PhraseTypeText == "Fillers").Colour
                },
                new ColoredText
                {
                    Text = forbiddenWords,
                    Colour = phraseTypes.FirstOrDefault(p => p.PhraseTypeText == "Alert").Colour
                }
            };
            return result;
        }
        private List<ColoredText> GetPhraseCloud(List<Guid> dialogueIds)
        {
            var phrases = DialoguePhrasesInfoAsQueryable(dialogueIds);
            var result = phrases
                .ToList()
                .GroupBy(p => p.PhraseText)
                .Select(p => new ColoredText{
                    Text = p.First().PhraseText,
                    Weight = 2 * p.Count(),
                    Colour = p.First().PhraseColor})
                .OrderByDescending(p => p.Weight)
                .Take(10)
                .ToList();
            return result;
        }
        private IQueryable<DialoguePhrasesInfo> DialoguePhrasesInfoAsQueryable(List<Guid> dialogueIds)
        {
            var phrases = _repository.GetAsQueryable<DialoguePhrase>()
                .Include(p => p.Phrase)
                .Where(p => p.DialogueId.HasValue
                    && dialogueIds.Contains(p.DialogueId.Value))
                .Select(p => new DialoguePhrasesInfo
                    {
                        PhraseText = p.Phrase.PhraseText,
                        PhraseColor = p.Phrase.PhraseType.Colour
                    })
                .Distinct()
                .AsQueryable();
            return phrases;
        }
        private double phraseTypeDialogueRatio(List<Dialogue> dialogues, List<PhraseType> phraseTypes, string PhraseTypeText)
        {            
            var countRequiredPhrasesDialogues = dialogues.Where(x => 
                    x.DialoguePhrase.Any(z => 
                            z.PhraseTypeId == phraseTypes.FirstOrDefault(c => c.PhraseTypeText == PhraseTypeText).PhraseTypeId))
                .Count();
            return (double)countRequiredPhrasesDialogues/dialogues.Count()*100;                        
        }
        
        private string BestPhraseTypeEmployee(List<Dialogue> dialogues, List<PhraseType> phraseTypes, string PhraseTypeText)
        {          
            var phraseTypeId = phraseTypes.FirstOrDefault(c => c.PhraseTypeText == PhraseTypeText).PhraseTypeId;  
            var bestEmployee = dialogues.Where(q => q.ApplicationUserId != null)
                .GroupBy(q => q.ApplicationUserId)
                .Select(q => new EmployeeCrossSelling
                    {
                        UserName = q.FirstOrDefault().ApplicationUser.FullName,
                        CrossSellingRatio = (double)q.Where(x => x.DialoguePhrase.Any(z => z.PhraseTypeId == phraseTypeId)).Count()/q.Count()*100
                    })
                .Aggregate((it1, it2) => it1.CrossSellingRatio > it2.CrossSellingRatio ? it1 : it2);
            return $"{bestEmployee.UserName}({bestEmployee.CrossSellingRatio.ToString("f")})";                        
        }
        private string WorstPhraseTypeEmployee(List<Dialogue> dialogues, List<PhraseType> phraseTypes, string PhraseTypeText)
        {          
            var phraseTypeId = phraseTypes.FirstOrDefault(c => c.PhraseTypeText == PhraseTypeText).PhraseTypeId;  
            var bestEmployee = dialogues.Where(q => q.ApplicationUserId != null)
                .GroupBy(q => q.ApplicationUserId)
                .Select(q => new EmployeeCrossSelling
                    {
                        UserName = q.FirstOrDefault().ApplicationUser.FullName,
                        CrossSellingRatio = (double)q.Where(x => x.DialoguePhrase.Any(z => z.PhraseTypeId == phraseTypeId)).Count()/q.Count()*100
                    })
                .Aggregate((it1, it2) => it1.CrossSellingRatio < it2.CrossSellingRatio ? it1 : it2);
            return $"{bestEmployee.UserName}({bestEmployee.CrossSellingRatio.ToString("f")})";                        
        }
        private string phraseTypeDialogueWords(List<Guid> dialogueIds, List<PhraseType> phraseTypes, string PhraseTypeText)
        {
            var phraseTypeId = phraseTypes.FirstOrDefault(c => c.PhraseTypeText == PhraseTypeText).PhraseTypeId;
            var result = _repository.GetAsQueryable<DialoguePhrase>()
                .Include(p => p.Phrase)
                .Where(p => dialogueIds.Contains((Guid)p.DialogueId)
                    && p.PhraseTypeId == phraseTypeId
                    && p.Phrase.PhraseText != null)
                .Select(p => p.Phrase.PhraseText)
                .Distinct()
                .ToList()
                .Aggregate((ph1, ph2) => ph1 + "; " + ph2);
            return result;            
        }
    }
    public class EmployeeSatisfaction
    {
        public string UserName { get; set; }
        public double Satisfaction { get; set; }
    }
    public class EmployeeCrossSelling
    {
        public string UserName { get; set; }
        public int NumberOfDialogues { get; set; }
        public double CrossSellingRatio { get; set; }
    }
    public class CompanyReportModel
    {
        public string CompanyName { get; set; }
        public double? LoadIndexOfCompany { get; set; }
        public int NumberOfCompanyDialogues { get; set; }
        public int NumberOfDialoguesForOneDevice { get; set; }
        public double AvgDialogueDuration { get; set; }
        public double Workload { get; set; }
        public double Succesfull { get; set; }
        public double SmilesAndCustomerSurprices { get; set; }
        public double CustomerAttention { get; set; }
        public double PositiveIntonationInEmployeeSpeech { get; set; }
        public double LoyaltyPhrasesInEmployeeSpeech { get; set; }
        public double EmotivnessOfEmployeeSpeech { get; set; }
        public string BestEmployee { get; set; }
        public string WorstEmployee { get; set; }
        public double CrossSellingDialogues { get; set; }
        public double RequiredPhrases { get; set; }
        public double LoyaltyPhrases { get; set; }
        public double CrossSellingPhrases { get; set; }
        public double ParasiteWords { get; set; }
        public double ForbiddenWords { get; set; }
        public List<ColoredText> ColoredWordTexts { get; set; }
        public string BestCrossSellingEmployee { get; set; }
        public string WorstCrossSellingEmployee { get; set; }
        public int DialoguesPerDayPerOffice { get; set; }
        public int DialoguesPerDayPerEmployee { get; set; }
        public string PopularTime { get; set; }
        public string PopularDay { get; set; }
    }
}