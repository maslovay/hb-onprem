using System;
using System.IO;
using System.Linq;
using System.Net;
using HBData;
using HBData.Models;
using HBData.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Collections.Generic;
using System.Text;

namespace ApiPerformance.Controllers
{
    [Route("performance/[controller]")]
    [ApiController]
    public class ApiPerformanceController : Controller
    {
        private readonly IGenericRepository _repository;
        private readonly RecordsContext _context;
        private string _password = "tysX8u";
        private string _token = "";
        
        public ApiPerformanceController(RecordsContext context, IGenericRepository repository
        )
        {
            _context = context;
            _repository = repository;
        }

        [HttpGet("ApiWorkingTimeReport")]
        [SwaggerResponse(200, "Report constructed")]
        public Microsoft.AspNetCore.Mvc.FileResult ApiWorkingTimeReport([FromQuery] int userCount)
        {
            CheckAPIWorkTime(userCount);
            var fileName = "ApiWorkingTimeReport.xlsx";
            var dataBytes = System.IO.File.ReadAllBytes(fileName);
            System.IO.File.Delete(fileName);
            var dataStream = new MemoryStream(dataBytes);
            var fileType = "application/xlsx";
            var returnedFileName = "ApiWorkingTimeReport.xlsx";
            return File( dataStream, fileType, fileName);
        }
       
       private async void CheckAPIWorkTime(int userCount = 10)
       {
            var report = new List<CompanyAPITimeReport>();

            var applicationUsers = _context.ApplicationUsers
                .Include(p => p.Company)
                .Where(p => p.Company.StatusId == 3
                    && p.StatusId == 3)
                .GroupBy(p => p.CompanyId)
                .Select(p => p.FirstOrDefault())
                .Take(userCount)
                .ToList();
            System.Console.WriteLine($"{applicationUsers.Count}");
            Guid CompanyId;

            var count = 1;
            var companyReportCount = applicationUsers.Count;
            foreach(var user in applicationUsers)
            {
                System.Console.WriteLine($"{count} - {companyReportCount}");
                CompanyId = (Guid)user.CompanyId;
                try
                {
                    var companyReport = new CompanyAPITimeReport()
                    {
                        CompanyName = user.Company.CompanyName,
                        AccountRegister = AccountRegister(),
                        AccountGenerateToken = AccountGenerateToken(),
                        AccountChangePassword = AccountChangePassword(),
                        AccountUnblock = AccountUnblock(),
                        AnalyticClientProfileGenderAgeStructure = AnalyticClientProfileGenderAgeStructure(),
                        AnalyticContentContentShows = AnalyticContentContentShows(CompanyId),
                        AnalyticContentEfficiency = AnalyticContentEfficiency(CompanyId),
                        AnalyticContentPool = AnalyticContentPool(CompanyId),
                        AnalyticHomeDashboard = AnalyticHomeDashboard(CompanyId),
                        AnalyticHomeRecomendation = AnalyticHomeRecomendation(CompanyId),
                        AnalyticOfficeEfficiency = AnalyticOfficeEfficiency(CompanyId),
                        AnalyticRatingProgress = AnalyticRatingProgress(CompanyId),
                        AnalyticRatingRatingUsers = AnalyticRatingRatingUsers(CompanyId),
                        AnalyticRatingOffices = AnalyticRatingOffices(CompanyId),
                        AnalyticReportActiveEmployee = AnalyticReportActiveEmployee(CompanyId),
                        AnalyticReportUserPartial = AnalyticReportUserPartial(CompanyId),
                        AnalyticReportUserFull = AnalyticReportUserFull(CompanyId),
                        AnalyticServiceQualityComponent = AnalyticServiceQualityComponent(CompanyId),
                        AnalyticServiceQualityDashboard = AnalyticServiceQualityDashboard(CompanyId),
                        AnalyticServiceQualityRating = AnalyticServiceQualityRating(CompanyId),
                        AnalyticServiceQualitySatisfactionStats = AnalyticServiceQualitySatisfactionStats(CompanyId),
                        AnalyticSpeechEmployeeRating = AnalyticSpeechEmployeeRating(CompanyId),
                        AnalyticSpeechEmployeePhraseTable = AnalyticSpeechEmployeePhraseTable(CompanyId),
                        AnalyticSpeechPhraseTypeCount = AnalyticSpeechPhraseTypeCount(CompanyId),
                        AnalyticSpeechWordCloud = AnalyticSpeechWordCloud(CompanyId),
                        AnalyticWeeklyReportUser = AnalyticWeeklyReportUser(CompanyId),
                        CampaignContentReturnCampaignWithContent = CampaignContentReturnCampaignWithContent(CompanyId),
                        CampaignContentCreateCampaignWithContent = CampaignContentCreateCampaignWithContent(CompanyId),
                        CampaignContentGetAllContent = CampaignContentGetAllContent(CompanyId),
                        CampaignContentSaveNewContent = CampaignContentSaveNewContent(CompanyId),
                        CatalogueCountry = CatalogueCountry(CompanyId),
                        CatalogueRole = CatalogueRole(CompanyId),
                        CatalogueWorkerType = CatalogueWorkerType(CompanyId),
                        CatalogueIndustry = CatalogueIndustry(CompanyId),
                        CatalogueLanguage = CatalogueLanguage(CompanyId),
                        CataloguePhraseType = CataloguePhraseType(CompanyId),
                        CatalogueAlertType = CatalogueAlertType(CompanyId),
                        CompanyReportGetReport = CompanyReportGetReport(CompanyId),
                        DemonstrationFlushStats = DemonstrationFlushStats(CompanyId),
                        DemonstrationGetContents = DemonstrationGetContents(CompanyId),
                        DemonstrationPoolAnswer = DemonstrationPoolAnswer(CompanyId),
                        HelpGetIndex = HelpGetIndex(CompanyId),
                        HelpGetDatabaseFilling = HelpGetDatabaseFilling(CompanyId),
                        LoggingSendLogGet = LoggingSendLogGet(CompanyId),
                        LoggingSendLogPost = LoggingSendLogPost(CompanyId),
                        MediaFileFileGet = MediaFileFileGet(CompanyId),
                        MediaFilePost = MediaFilePost(CompanyId),
                        PaymentTariff = PaymentTariff(CompanyId),
                        PaymentCheckoutResponce = PaymentCheckoutResponce(CompanyId),
                        PhrasePhraseScript = PhrasePhraseScript(CompanyId),
                        SessionSessionStatus = SessionSessionStatus(CompanyId),
                        SessionAlertNotSmile = SessionAlertNotSmile(CompanyId),
                        SiteFeedBack = SiteFeedBack(CompanyId),
                        UserGetAllCompanyUsers = UserGetAllCompanyUsers(CompanyId),
                        UserPost = UserPost(CompanyId),
                        UserCompanies = UserCompanies(CompanyId),
                        UserCorporations = UserCorporations(CompanyId),
                        UserPhraseLibLibrary = UserPhraseLibLibrary(CompanyId),
                        UserPhraseLibCreateCompanyPhrase = UserPhraseLibCreateCompanyPhrase(CompanyId),
                        UserCompanyPhraseReturnAttachedToCompanyPhrases = UserCompanyPhraseReturnAttachedToCompanyPhrases(CompanyId),
                        UserCompanyPhraseAttachLibraryPhrasesToCompany = UserCompanyPhraseAttachLibraryPhrasesToCompany(CompanyId),
                        UserDialogue = UserDialogue(CompanyId),
                        UserDialogueInclude = UserDialogueInclude(CompanyId),
                        UserAlert = UserAlert(CompanyId)
                    };
                    report.Add(companyReport);
                }
                catch(Exception ex)
                {
                    System.Console.WriteLine(ex);
                }                
                count++;
                
                // if(count == 101)
                //     break;
                // if(count == 10)
                //     break;
            }

            PrintAPIWorkTime(report);
       }
        
       private void PrintAPIWorkTime(List<CompanyAPITimeReport> companyReport)
       {
           var path = "ApiWorkingTimeReport.xlsx";
            using (SpreadsheetDocument document = SpreadsheetDocument.Create( path, SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());               

                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "ApiWorkingTimeReport" };
                
                sheets.Append(sheet);                

                SheetData sheetData = worksheetPart.Worksheet.AppendChild(new SheetData());
                Row row1 = new Row();

                row1.Append(
                    ConstructCell("CompanyName", CellValues.String),
                    ConstructCell("AccountRegister",CellValues.String),
                    ConstructCell("AccountGenerateToken",CellValues.String),
                    ConstructCell("AccountChangePassword",CellValues.String),
                    ConstructCell("AccountUnblock",CellValues.String),

                    ConstructCell("AnalyticClientProfileGenderAgeStructure",CellValues.String),

                    ConstructCell("AnalyticContentContentShows",CellValues.String),
                    ConstructCell("AnalyticContentEfficiency",CellValues.String),
                    ConstructCell("AnalyticContentPool",CellValues.String),

                    ConstructCell("AnalyticHomeDashboard",CellValues.String),
                    ConstructCell("AnalyticHomeRecomendation",CellValues.String),

                    ConstructCell("AnalyticOfficeEfficiency",CellValues.String),

                    ConstructCell("AnalyticRatingProgress",CellValues.String),
                    ConstructCell("AnalyticRatingRatingUsers",CellValues.String),
                    ConstructCell("AnalyticRatingOffices",CellValues.String),

                    ConstructCell("AnalyticReportActiveEmployee",CellValues.String),
                    ConstructCell("AnalyticReportUserPartial",CellValues.String),
                    ConstructCell("AnalyticReportUserFull",CellValues.String),

                    ConstructCell("AnalyticServiceQualityComponent",CellValues.String),
                    ConstructCell("AnalyticServiceQualityDashboard",CellValues.String),
                    ConstructCell("AnalyticServiceQualityRating",CellValues.String),
                    ConstructCell("AnalyticServiceQualitySatisfactionStats",CellValues.String),

                    ConstructCell("AnalyticSpeechEmployeeRating",CellValues.String),
                    ConstructCell("AnalyticSpeechEmployeePhraseTable",CellValues.String),
                    ConstructCell("AnalyticSpeechPhraseTypeCount",CellValues.String),
                    ConstructCell("AnalyticSpeechWordCloud",CellValues.String),

                    ConstructCell("AnalyticWeeklyReportUser",CellValues.String),

                    ConstructCell("CampaignContentReturnCampaignWithContent",CellValues.String),
                    ConstructCell("CampaignContentCreateCampaignWithContent",CellValues.String),
                    ConstructCell("CampaignContentGetAllContent",CellValues.String),
                    ConstructCell("CampaignContentSaveNewContent",CellValues.String),

                    ConstructCell("CatalogueCountry",CellValues.String),
                    ConstructCell("CatalogueRole",CellValues.String),
                    ConstructCell("CatalogueWorkerType",CellValues.String),
                    ConstructCell("CatalogueIndustry",CellValues.String),
                    ConstructCell("CatalogueLanguage",CellValues.String),
                    ConstructCell("CataloguePhraseType",CellValues.String),
                    ConstructCell("CatalogueAlertType",CellValues.String),

                    ConstructCell("CompanyReportGetReport",CellValues.String),

                    ConstructCell("DemonstrationFlushStats",CellValues.String),
                    ConstructCell("DemonstrationGetContents",CellValues.String),
                    ConstructCell("DemonstrationPoolAnswer",CellValues.String),

                    ConstructCell("HelpGetIndex",CellValues.String),
                    ConstructCell("HelpGetDatabaseFilling",CellValues.String),
                    
                    ConstructCell("LoggingSendLogGet",CellValues.String),
                    ConstructCell("LoggingSendLogPost",CellValues.String),

                    ConstructCell("MediaFileFileGet",CellValues.String),
                    ConstructCell("MediaFilePost",CellValues.String),

                    ConstructCell("PaymentTariff",CellValues.String),
                    ConstructCell("PaymentCheckoutResponce",CellValues.String),

                    ConstructCell("PhrasePhraseScript",CellValues.String),

                    ConstructCell("SessionSessionStatus",CellValues.String),
                    ConstructCell("SessionAlertNotSmile",CellValues.String),

                    ConstructCell("SiteFeedBack",CellValues.String),

                    ConstructCell("UserGetAllCompanyUsers",CellValues.String),
                    ConstructCell("UserPost",CellValues.String),
                    ConstructCell("UserCompanies",CellValues.String),
                    ConstructCell("UserCorporations",CellValues.String),
                    ConstructCell("UserPhraseLibLibrary",CellValues.String),
                    ConstructCell("UserPhraseLibCreateCompanyPhrase",CellValues.String),
                    ConstructCell("UserCompanyPhraseReturnAttachedToCompanyPhrases",CellValues.String),
                    ConstructCell("UserCompanyPhraseAttachLibraryPhrasesToCompany",CellValues.String),
                    ConstructCell("UserDialogue",CellValues.String),
                    ConstructCell("UserDialogueInclude",CellValues.String),
                    ConstructCell("UserAlert",CellValues.String)
                );
                sheetData.AppendChild(row1);
                Row tempRow;
                
                foreach(var c in companyReport)
                {                    
                    tempRow = new Row();
                    tempRow.Append(
                        ConstructCell(c.CompanyName, CellValues.String),
                        ConstructCell(c.AccountRegister, CellValues.Number),
                        ConstructCell(c.AccountGenerateToken, CellValues.Number),
                        ConstructCell(c.AccountChangePassword, CellValues.Number),
                        ConstructCell(c.AccountUnblock, CellValues.Number),
                        ConstructCell(c.AnalyticClientProfileGenderAgeStructure, CellValues.Number),
                        ConstructCell(c.AnalyticContentContentShows, CellValues.Number),
                        ConstructCell(c.AnalyticContentEfficiency, CellValues.Number),
                        ConstructCell(c.AnalyticContentPool, CellValues.Number),
                        ConstructCell(c.AnalyticHomeDashboard, CellValues.Number),
                        ConstructCell(c.AnalyticHomeRecomendation, CellValues.Number),
                        ConstructCell(c.AnalyticOfficeEfficiency, CellValues.Number),
                        ConstructCell(c.AnalyticRatingProgress, CellValues.Number),
                        ConstructCell(c.AnalyticRatingRatingUsers, CellValues.Number),
                        ConstructCell(c.AnalyticRatingOffices, CellValues.Number),
                        ConstructCell(c.AnalyticReportActiveEmployee, CellValues.Number),
                        ConstructCell(c.AnalyticReportUserPartial, CellValues.Number),
                        ConstructCell(c.AnalyticReportUserFull, CellValues.Number),
                        ConstructCell(c.AnalyticServiceQualityComponent, CellValues.Number),
                        ConstructCell(c.AnalyticServiceQualityDashboard, CellValues.Number),
                        ConstructCell(c.AnalyticServiceQualityRating, CellValues.Number),
                        ConstructCell(c.AnalyticServiceQualitySatisfactionStats, CellValues.Number),
                        ConstructCell(c.AnalyticSpeechEmployeeRating, CellValues.Number),
                        ConstructCell(c.AnalyticSpeechEmployeePhraseTable, CellValues.Number),
                        ConstructCell(c.AnalyticSpeechPhraseTypeCount, CellValues.Number),
                        ConstructCell(c.AnalyticSpeechWordCloud, CellValues.Number),
                        ConstructCell(c.AnalyticWeeklyReportUser, CellValues.Number),
                        ConstructCell(c.CampaignContentReturnCampaignWithContent, CellValues.Number),
                        ConstructCell(c.CampaignContentCreateCampaignWithContent, CellValues.Number),
                        ConstructCell(c.CampaignContentGetAllContent, CellValues.Number),
                        ConstructCell(c.CampaignContentSaveNewContent, CellValues.Number),
                        ConstructCell(c.CatalogueCountry, CellValues.Number),
                        ConstructCell(c.CatalogueRole, CellValues.Number),
                        ConstructCell(c.CatalogueWorkerType, CellValues.Number),
                        ConstructCell(c.CatalogueIndustry, CellValues.Number),
                        ConstructCell(c.CatalogueLanguage, CellValues.Number),
                        ConstructCell(c.CataloguePhraseType, CellValues.Number),
                        ConstructCell(c.CatalogueAlertType, CellValues.Number),
                        ConstructCell(c.CompanyReportGetReport, CellValues.Number),
                        ConstructCell(c.DemonstrationFlushStats, CellValues.Number),
                        ConstructCell(c.DemonstrationGetContents, CellValues.Number),
                        ConstructCell(c.DemonstrationPoolAnswer, CellValues.Number),
                        ConstructCell(c.HelpGetIndex, CellValues.Number),
                        ConstructCell(c.HelpGetDatabaseFilling, CellValues.Number),
                        ConstructCell(c.LoggingSendLogGet, CellValues.Number),
                        ConstructCell(c.LoggingSendLogPost, CellValues.Number),
                        ConstructCell(c.MediaFileFileGet, CellValues.Number),
                        ConstructCell(c.MediaFilePost, CellValues.Number),
                        ConstructCell(c.PaymentTariff, CellValues.Number),
                        ConstructCell(c.PaymentCheckoutResponce, CellValues.Number),
                        ConstructCell(c.PhrasePhraseScript, CellValues.Number),
                        ConstructCell(c.SessionSessionStatus, CellValues.Number),
                        ConstructCell(c.SessionAlertNotSmile, CellValues.Number),
                        ConstructCell(c.SiteFeedBack, CellValues.Number),
                        ConstructCell(c.UserGetAllCompanyUsers, CellValues.Number),
                        ConstructCell(c.UserPost, CellValues.Number),
                        ConstructCell(c.UserCompanies, CellValues.Number),
                        ConstructCell(c.UserCorporations, CellValues.Number),
                        ConstructCell(c.UserPhraseLibLibrary, CellValues.Number),
                        ConstructCell(c.UserPhraseLibCreateCompanyPhrase, CellValues.Number),
                        ConstructCell(c.UserCompanyPhraseReturnAttachedToCompanyPhrases, CellValues.Number),
                        ConstructCell(c.UserCompanyPhraseAttachLibraryPhrasesToCompany, CellValues.Number),
                        ConstructCell(c.UserDialogue, CellValues.Number),
                        ConstructCell(c.UserDialogueInclude, CellValues.Number),
                        ConstructCell(c.UserAlert, CellValues.Number)
                    );
                    sheetData.AppendChild(tempRow);
                }
                workbookPart.Workbook.Save();
                System.Console.WriteLine($"файл сохранен!!!");
            }
       }
        
    #region Methods        
        private string AccountRegister()
        {
            var start = DateTime.Now;
            var json = JsonConvert.SerializeObject(new {                
                fullName="Ivanov Ivan Ivanovich",
                email="pinarin@heedbook.com",
                password="ivanov",
                companyName="IvanovCompany",
                languageId=2,
                countryId="be6a6509-7c9e-4d63-b787-5725bbbb2f26",
                companyIndustryId="4ff8cf57-4285-4cd4-92d6-1011964f2110",
                corporationId=""
            });
            var data = Encoding.ASCII.GetBytes(json);
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Account/Register";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";   
            request.ContentLength = data.Length;    

            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var responce = request.GetResponseAsync();
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }

        private string AccountGenerateToken()
        {
            var start = DateTime.Now;
            var json = JsonConvert.SerializeObject(new {                
                userName="pinarin@heedbook.com",                
                password=_password,
                remember=true             
            });
            var data = Encoding.ASCII.GetBytes(json);
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Account/GenerateToken";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";   
            request.ContentLength = data.Length;    
            try
            {
                using(var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }            
                var responce = request.GetResponse();
                var dataStream = responce.GetResponseStream();
                var reader = new StreamReader(dataStream);
                var token = reader.ReadToEnd();        
            }
            catch(Exception ex)
            {
                System.Console.WriteLine(ex);
            }
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string AccountChangePassword()
        {
            var start = DateTime.Now;
            var json = JsonConvert.SerializeObject(new {                
                userName="pinarin@heedbook2.com",                
                password=_password,
                remember=true             
            });
            var data = Encoding.ASCII.GetBytes(json);
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Account/ChangePassword";
            var request = WebRequest.Create(url);
            
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";   
            request.ContentLength = data.Length;    

            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }

        private string AccountUnblock()
        {
            var _token = GetToken();
            var start = DateTime.Now;
            var email="pinarin@heedbook.com";
            var data = Encoding.ASCII.GetBytes(email);
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Account/Unblock";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";   
            request.ContentLength = data.Length;    
            request.Headers.Add("Authorization", $"Bearer {_token}");
            
            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }

        private string GetToken()
        {            
            var json = JsonConvert.SerializeObject(new {                
                userName="pinarin@heedbook.com",                
                password=_password,
                remember=true             
            });
            var data = Encoding.ASCII.GetBytes(json);
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Account/GenerateToken";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";   
            request.ContentLength = data.Length;    

            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }            
            var responce = request.GetResponse();
            var dataStream = responce.GetResponseStream();
            var reader = new StreamReader(dataStream);
            var token = reader.ReadToEnd();
            return token;
        }

        private string AnalyticClientProfileGenderAgeStructure()
        {
            var start = DateTime.Now;    
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticClientProfile/GenderAgeStructure";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        
        private string AnalyticContentContentShows(Guid CompanyId)
        {
            var companyId = Guid.Parse("4fdf3b7d-0707-40d6-8aee-d95499fd7b7d");
            var dialogue =  _context.Dialogues
                .Include(p => p.ApplicationUser)
                .FirstOrDefault(p => p.BegTime.Date > DateTime.Now.Date.AddDays(-4)
                    && p.ApplicationUser.CompanyId == companyId);
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticContent/ContentShows?dialogueId=0055585f-3fad-4d70-92be-353ce4952954";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }

        private string AnalyticContentEfficiency(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticContent/Efficiency";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        
        private string AnalyticContentPool(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticContent/Poll";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string AnalyticHomeDashboard(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticHome/Dashboard";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string AnalyticHomeRecomendation(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticHome/Recomendation";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }

        private string AnalyticOfficeEfficiency(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticOffice/Efficiency";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string AnalyticRatingProgress(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticRating/Progress";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }

        private string AnalyticRatingRatingUsers(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticRating/RatingUsers";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string AnalyticRatingOffices(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticRating/RatingOffices";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string AnalyticReportActiveEmployee(Guid CompanyId)
        {            
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticReport/ActiveEmployee";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string AnalyticReportUserPartial(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticReport/UserPartial";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string AnalyticReportUserFull(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticReport/UserFull";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string AnalyticServiceQualityComponent(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticServiceQuality/Components";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string AnalyticServiceQualityDashboard(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticServiceQuality/Dashboard";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string AnalyticServiceQualityRating(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticServiceQuality/Rating";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string AnalyticServiceQualitySatisfactionStats(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticServiceQuality/SatisfactionStats";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string AnalyticSpeechEmployeeRating(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticSpeech/EmployeeRating";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string AnalyticSpeechEmployeePhraseTable(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticSpeech/PhraseTable";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string AnalyticSpeechPhraseTypeCount(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticSpeech/PhraseTypeCount";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string AnalyticSpeechWordCloud(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticSpeech/WordCloud";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }        
        private string AnalyticWeeklyReportUser(Guid CompanyId)
        {
            var applicationUserId = "0a1c29c0-d531-44bd-8b30-4677e049149b";
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/AnalyticWeeklyReport/User?applicationUserId={applicationUserId}";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string CampaignContentReturnCampaignWithContent(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/CampaignContent/Campaign";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string CampaignContentCreateCampaignWithContent(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/CampaignContent/Campaign";
            var request = WebRequest.Create(url);
            var campaignId = Guid.NewGuid();
            var cmp = new Campaign()
            {
                CampaignId = campaignId,
                Name = "Тестовая компания Иванова Иван Ивановича",
                IsSplash = false,
                GenderId = 0,
                BegAge = 20,
                BegDate = DateTime.Now.AddDays(-1),
                EndDate = DateTime.Now.AddDays(1),
                CreationDate = DateTime.Now.AddDays(-2),
                CompanyId = CompanyId,
                StatusId = 3,
                CampaignContents = new List<CampaignContent>()
                {
                    new CampaignContent()
                    {
                        CampaignContentId = Guid.NewGuid(),
                        SequenceNumber = 1,
                        ContentId = Guid.NewGuid(),
                        CampaignId = campaignId
                    }
                }
            };
            var campaignContents = new List<CampaignContent>()
            {
                new CampaignContent()
                    {
                        CampaignContentId = Guid.NewGuid(),
                        SequenceNumber = 1,
                        ContentId = Guid.NewGuid(),
                        CampaignId = campaignId
                    }
            };
            var model = new CampaignPutPostModel(cmp, campaignContents);
            var json = JsonConvert.SerializeObject(model);
            var data = Encoding.ASCII.GetBytes(json);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";   
            request.ContentLength = data.Length;       
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string CampaignContentGetAllContent(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/CampaignContent/Content";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string CampaignContentSaveNewContent(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/CampaignContent/Campaign";
            var request = WebRequest.Create(url);
            
            var model = new Content()
            {
                ContentId = Guid.NewGuid(),
                RawHTML = "<div>Тестовая разметка</div>",
                Name = "Test Content",
                Duration = 100,
                CompanyId = CompanyId,
                JSONData = "{\"panels\":[]}",
                IsTemplate = false,
                CreationDate = DateTime.Now.AddDays(-2),
                UpdateDate = DateTime.Now.AddDays(2)
            };
            var json = JsonConvert.SerializeObject(model);
            var data = Encoding.ASCII.GetBytes(json);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";   
            request.ContentLength = data.Length;       
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        
        private string CatalogueCountry(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Catalogue/Country";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json"; 
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string CatalogueRole(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Catalogue/Role";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json"; 
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string CatalogueWorkerType(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Catalogue/WorkerType";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string CatalogueIndustry(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Catalogue/Industry";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json"; 
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string CatalogueLanguage(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Catalogue/Language";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json"; 
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string CataloguePhraseType(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Catalogue/PhraseType";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json"; 
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string CatalogueAlertType(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Catalogue/AlertType";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json"; 
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string CompanyReportGetReport(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/CompanyReport/GetReport";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json"; 
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string DemonstrationFlushStats(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Demonstration/FlushStats";
            var request = WebRequest.Create(url);
            
            var model = new List<SlideShowSession>()
            {
                new SlideShowSession()
                {
                    CampaignContentId = Guid.NewGuid(),
                    ApplicationUserId = Guid.Parse("0a1c29c0-d531-44bd-8b30-4677e049149b"),
                    BegTime = DateTime.Now.AddDays(-1),
                    EndTime = DateTime.Now.AddDays(1)
                }
            };
            var json = JsonConvert.SerializeObject(model);
            var data = Encoding.ASCII.GetBytes(json);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";   
            request.ContentLength = data.Length;       
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string DemonstrationGetContents(Guid CompanyId)
        {
            var start = DateTime.Now;    
            var userId = "0a1c29c0-d531-44bd-8b30-4677e049149b";           
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Demonstration/GetContents?userId={userId}";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json"; 
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string DemonstrationPoolAnswer(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Demonstration/PollAnswer";
            var request = WebRequest.Create(url);
            
            var model = new CampaignContentAnswer()
            {
                CampaignContentId = Guid.NewGuid(),
                Answer = "Тестовый ответ",
                Time = DateTime.Now
            };
            var json = JsonConvert.SerializeObject(model);
            var data = Encoding.ASCII.GetBytes(json);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";   
            request.ContentLength = data.Length;       
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string HelpGetIndex(Guid CompanyId)
        {
            var start = DateTime.Now;         
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Help/GetIndex?companyId={CompanyId}";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json"; 
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string HelpGetDatabaseFilling(Guid CompanyId)
        {
            var start = DateTime.Now;      
            var countryName = "Russia";   
            var companyIndustryName = "Car dealers and service";
            var corporationName = "";
            var languageName = "Russian";
            var languageShort = "Русский";
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Help/DatabaseFilling?countryName={countryName}&companyIndustryName={companyIndustryName}&corporationName={corporationName}&languageName={languageName}&languageShortName={languageShort}";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json"; 
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string LoggingSendLogGet(Guid CompanyId)
        {
            var start = DateTime.Now;     
            var message = "TestMessage";
            var severity = "TestSeverity";
            var functionName = "TestFunctionName"; 
            
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Logging/SendLog?message={message}&severity={severity}&functionName={functionName}";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json"; 
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string LoggingSendLogPost(Guid CompanyId)
        {           
            var start = DateTime.Now;   
            var message = "TestMessage";
            var severity = "TestSeverity";
            var functionName = "TestFunctionName";             
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Logging/SendLog?message={message}&severity={severity}&functionName={functionName}";
            var request = WebRequest.Create(url);                        
            
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";   
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string MediaFileFileGet(Guid CompanyId)
        {
            var containerName = "";
            var fileName = "";
            var expiration = "";
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/MediaFile/File?containerName={containerName}&fileName={fileName}&expirationDate={expiration}";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";   
                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string MediaFilePost(Guid CompanyId)
        {           
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/MediaFile/File";
            var request = WebRequest.Create(url);            
            
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";                   
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string PaymentTariff(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Payment/Tariff";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string PaymentCheckoutResponce(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Payment/CheckoutResponse";
            var request = WebRequest.Create(url);
            
            var model = "test=test&test2=test2";
            var json = JsonConvert.SerializeObject(model);
            var data = Encoding.ASCII.GetBytes(json);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";   
            request.ContentLength = data.Length;          
            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string PhrasePhraseScript(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Phrase/PhraseScripts";
            var request = WebRequest.Create(url);
            
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";        
            request.Headers.Add("Authorization", $"Bearer {_token}");
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string SessionSessionStatus(Guid CompanyId)
        {   
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Session/SessionStatus";
            var request = WebRequest.Create(url);
            
            var model = new SessionParams()
            {
                ApplicationUserId = Guid.Parse("0a1c29c0-d531-44bd-8b30-4677e049149b"),
                Action = "close",
                IsDesktop = true
            };

            var json = JsonConvert.SerializeObject(model);
            var data = Encoding.ASCII.GetBytes(json);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";   
            request.ContentLength = data.Length;          
            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string SessionAlertNotSmile(Guid CompanyId)
        {    
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Session/AlertNotSmile";
            var request = WebRequest.Create(url);
            
            var model = "0a1c29c0-d531-44bd-8b30-4677e049149b";

            var json = JsonConvert.SerializeObject(model);
            var data = Encoding.ASCII.GetBytes(json);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";   
            request.ContentLength = data.Length;          
            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string SiteFeedBack(Guid CompanyId)
        {    
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/Session/AlertNotSmile";
            var request = WebRequest.Create(url);
            
            var model = new FeedbackEntity()
            {
                name = "TestUser",
                phone = "1234567890",
                body = "Very impressive, unbelivable product",
                email = "test@test.com"
            };

            var json = JsonConvert.SerializeObject(model);
            var data = Encoding.ASCII.GetBytes(json);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";   
            request.ContentLength = data.Length;          
            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string UserGetAllCompanyUsers(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/User/User";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string UserPost(Guid CompanyId)
        {           
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/User/User";
            var request = WebRequest.Create(url);            
            
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";                   
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string UserCompanies(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/User/Companies";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string UserCorporations(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/User/Corporations";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string UserPhraseLibLibrary(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/User/PhraseLib";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string UserPhraseLibCreateCompanyPhrase(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/User/PhraseLib";
            var request = WebRequest.Create(url);
            
            var model = new PhrasePost()
            {
                PhraseText = "Артемий Лебедев",
                PhraseTypeId = Guid.Parse("94549e12-78a8-11e9-8f9e-2a86e4085a59"),
                LanguageId = 2,
                IsClient = true,
                WordsSpace = 1,
                Accurancy = 1,
                IsTemplate = true
            };
            var json = JsonConvert.SerializeObject(model);
            var data = Encoding.ASCII.GetBytes(json);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";   
            request.ContentLength = data.Length;       
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string UserCompanyPhraseReturnAttachedToCompanyPhrases(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/User/CompanyPhrase";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string UserCompanyPhraseAttachLibraryPhrasesToCompany(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/User/CompanyPhrase";
            var request = WebRequest.Create(url);
            
            var model = new PhrasePost()
            {
                PhraseText = "Артемий Лебедев",
                PhraseTypeId = Guid.Parse("94549e12-78a8-11e9-8f9e-2a86e4085a59"),
                LanguageId = 2,
                IsClient = true,
                WordsSpace = 1,
                Accurancy = 1,
                IsTemplate = true
            };
            var json = JsonConvert.SerializeObject(model);
            var data = Encoding.ASCII.GetBytes(json);
            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";   
            request.ContentLength = data.Length;       
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string UserDialogue(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/User/Dialogue";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string UserDialogueInclude(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/User/DialogueInclude";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        private string UserAlert(Guid CompanyId)
        {
            var start = DateTime.Now;               
            var url = $"https://heedbookslave.northeurope.cloudapp.azure.com/api/User/Alert";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "application/json-patch+json";                
            request.Headers.Add("Authorization", $"Bearer {_token}");            
            
            var responce = request.GetResponseAsync();
            
            return DateTime.Now.Subtract(start).TotalMilliseconds.ToString();
        }
        #endregion
        private Cell ConstructCell(string value, CellValues dataType)
        {
            return new Cell()
            {
                CellValue = new CellValue(value),
                DataType = new EnumValue<CellValues>(dataType)
            };
        }
    }
    public class CompanyAPITimeReport
    {
        public string CompanyName {get; set;}
        public string AccountRegister {get; set;}
        public string AccountGenerateToken {get; set;}
        public string AccountChangePassword {get; set;}
        public string AccountUnblock {get; set;}

        public string AnalyticClientProfileGenderAgeStructure {get; set;}

        public string AnalyticContentContentShows {get; set;}
        public string AnalyticContentEfficiency {get; set;}
        public string AnalyticContentPool {get; set;}

        public string AnalyticHomeDashboard {get; set;}
        public string AnalyticHomeRecomendation {get; set;}

        public string AnalyticOfficeEfficiency {get; set;}

        public string AnalyticRatingProgress {get; set;}
        public string AnalyticRatingRatingUsers {get; set;}
        public string AnalyticRatingOffices {get; set;}

        public string AnalyticReportActiveEmployee {get; set;}
        public string AnalyticReportUserPartial {get; set;}
        public string AnalyticReportUserFull {get; set;}

        public string AnalyticServiceQualityComponent {get; set;}
        public string AnalyticServiceQualityDashboard {get; set;}
        public string AnalyticServiceQualityRating {get; set;}
        public string AnalyticServiceQualitySatisfactionStats {get; set;}

        public string AnalyticSpeechEmployeeRating {get; set;}
        public string AnalyticSpeechEmployeePhraseTable {get; set;}
        public string AnalyticSpeechPhraseTypeCount {get; set;}
        public string AnalyticSpeechWordCloud {get; set;}

        public string AnalyticWeeklyReportUser {get; set;}

        public string CampaignContentReturnCampaignWithContent {get; set;}
        public string CampaignContentCreateCampaignWithContent {get; set;}
        public string CampaignContentGetAllContent {get; set;}
        public string CampaignContentSaveNewContent {get; set;}

        public string CatalogueCountry {get; set;}
        public string CatalogueRole {get; set;}
        public string CatalogueWorkerType {get; set;}
        public string CatalogueIndustry {get; set;}
        public string CatalogueLanguage {get; set;}
        public string CataloguePhraseType {get; set;}
        public string CatalogueAlertType {get; set;}

        public string CompanyReportGetReport {get; set;}

        public string DemonstrationFlushStats {get; set;}
        public string DemonstrationGetContents {get; set;}
        public string DemonstrationPoolAnswer {get; set;}

        public string HelpGetIndex {get; set;}
        public string HelpGetDatabaseFilling {get; set;}        

        public string LoggingSendLogGet {get; set;}
        public string LoggingSendLogPost {get; set;}

        public string MediaFileFileGet {get; set;}
        public string MediaFilePost {get; set;}

        public string PaymentTariff {get; set;}
        public string PaymentCheckoutResponce {get; set;}

        public string PhrasePhraseScript {get; set;}

        public string SessionSessionStatus {get; set;}
        public string SessionAlertNotSmile {get; set;}

        public string SiteFeedBack {get; set;}

        public string UserGetAllCompanyUsers {get; set;}        
        public string UserPost {get; set;}
        public string UserCompanies {get; set;}
        public string UserCorporations {get; set;}
        public string UserPhraseLibLibrary {get; set;}        
        public string UserPhraseLibCreateCompanyPhrase {get; set;}
        public string UserCompanyPhraseReturnAttachedToCompanyPhrases {get; set;}
        public string UserCompanyPhraseAttachLibraryPhrasesToCompany {get; set;}
        public string UserDialogue {get; set;}
        public string UserDialogueInclude {get; set;}
        public string UserAlert {get; set;}

    }
    public class SessionParams
    {
        public Guid ApplicationUserId;
        public string Action;
        public bool? IsDesktop;
    }
    public class FeedbackEntity
    {
        public string name { get; set; }
        public string phone { get; set; }
        public string body { get; set; }
        public string email { get; set; }
    }
    public class PhrasePost
    {
        public string PhraseText;
        public Guid PhraseTypeId;
        public Int32? LanguageId;
        public bool IsClient;
        public Int32? WordsSpace;
        public double? Accurancy;
        public Boolean IsTemplate;
    }
    public class CampaignPutPostModel
    {
        public CampaignPutPostModel(Campaign cmp, List<CampaignContent> campaignContents)
        {
            Campaign = cmp;
            CampaignContents = campaignContents;
        }
        public Campaign Campaign { get; set; }
        public List<CampaignContent> CampaignContents { get; set; }
    }
}


      