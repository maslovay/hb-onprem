using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ApiPerformance.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ApiPerformance.Services
{
    public class ApiPerformanceService
    {
        private readonly IGenericRepository _repository;
        private readonly RecordsContext _context;
        private readonly URLSettings _urlSettings;
        private readonly TestCompanySettings _companySettings;
        private string _token = "";
        private Guid _userId;
        private Guid _deviceId;
        private Guid CompanyId;
        private Guid _dialogueId;
        private Guid _phraseId;
        private Guid _corporationId;
        private Guid _industryId;
        private Guid _contentId;
        private Guid _campaignId;
        private Guid _campaignContentId;
        private readonly SftpClient _sftpClient;
        public ApiPerformanceService(RecordsContext context, 
            IGenericRepository repository,
            URLSettings urlSettings,
            TestCompanySettings companySettings,
            SftpClient sftpClient)
        {
            _context = context;
            _repository = repository;
            _urlSettings = urlSettings;
            _companySettings = companySettings;
            _sftpClient = sftpClient;
        }
        private void PrepareTestData()
        {
            var user = _repository.GetAsQueryable<ApplicationUser>()
                .FirstOrDefault(p => p.Email == _companySettings.UserEmail);
            _userId = user.Id;
            CompanyId = (Guid)user.CompanyId;
            var device = new Device()
            {
                DeviceId = Guid.NewGuid(),
                Code = "MMMMMM",
                CompanyId = CompanyId,
                Name = "TestDeviceName",
                StatusId = 3
            };
            _repository.Create<Device>(device);
            var client = new Client()
            {
                ClientId = Guid.NewGuid(),
                Name = "TestUserName",
                Phone = "89007776655",
                Gender = "male",
                Age = 36,
                StatusId = 3,
                CompanyId = CompanyId
            };
            _repository.Create<Client>(client);
            var dialogue = new Dialogue()
            {
                DialogueId = Guid.NewGuid(),
                BegTime = DateTime.Now.AddDays(-1),
                EndTime = DateTime.Now.AddDays(-1).AddMinutes(15),
                ApplicationUserId = _userId,
                DeviceId = device.DeviceId,
                CreationTime = DateTime.Now,
                StatusId = 3,
                InStatistic = true,
                ClientId = client.ClientId
            };
            _repository.Create<Dialogue>(dialogue);
            var phrase = new Phrase()
            {
                PhraseId = Guid.NewGuid(),
                PhraseText = "TestPhrase",
                LanguageId = 2
            };
            var content = new Content()
            {
                ContentId = Guid.NewGuid(),
                RawHTML = "<div>Тестовая разметка</div>",
                Name = "Test Content",
                Duration = 100,
                CompanyId = CompanyId,
                IsTemplate = false,
                CreationDate = DateTime.Now.AddDays(-2),
                UpdateDate = DateTime.Now.AddDays(2),
                StatusId = 3,
                JSONData = "<div>answerText</div>"
            };
            _repository.Create<Content>(content);
            
            var campaign = new Campaign()
            {
                CampaignId = Guid.NewGuid(),
                Name = "APITestCampaign",
                IsSplash = true,
                GenderId = 1,
                BegAge = 0,
                EndAge = 18,
                BegDate = DateTime.Now.AddDays(-1),
                EndDate = DateTime.Now,
                CreationDate = DateTime.Now.AddDays(-2),
                StatusId = 3,
                CompanyId = CompanyId
            };
            _repository.Create<Campaign>(campaign);
            
            var campaignContent = new CampaignContent()
            {
                CampaignContentId = Guid.NewGuid(),
                SequenceNumber = 1,
                ContentId = content.ContentId,
                CampaignId = campaign.CampaignId,
                StatusId = 3
            };            
            _repository.Create<CampaignContent>(campaignContent);
            _repository.Save();
            _deviceId = device.DeviceId;
            _contentId = content.ContentId;
            _campaignId = campaign.CampaignId;
            _campaignContentId = campaignContent.CampaignContentId;
            _phraseId = phrase.PhraseId;
            _repository.Create<Phrase>(phrase);            
            _repository.Save();
            _dialogueId = dialogue.DialogueId;
        }
        public async Task<string> CheckAPIWorkTime(string fileName, int numberOfAttempts = 1)
        {
            List<ResponceReportModel> report;

            var applicationUser = _context.ApplicationUsers
                .Include(p => p.Company)
                .Where(p => p.Email == _companySettings.UserEmail)
                .FirstOrDefault();

            if(applicationUser != null)
            {
                System.Console.WriteLine($"remove existed Test User {_companySettings.UserEmail}");
                await RemoveCreatedCompanyAndUser();
                throw new ArgumentException($"user: {_companySettings.UserEmail} already exist, remove it first");
            }

            try
            {
                report = new List<ResponceReportModel>();
                
                var accountRegisterResult = AccountRegister();
                PrepareTestData();
                var generateTokenTasks = AccountGenerateToken(numberOfAttempts);
                report.Add(accountRegisterResult);
                report.Add(generateTokenTasks);

                var taskList = new List<Task<ResponceReportModel>>()
                {                    
                    Task.Run(() => AccountChangePassword(numberOfAttempts)),                    
                    Task.Run(() => AnalyticClientProfileGenderAgeStructure(numberOfAttempts)),
                    Task.Run(() => AnalyticContentContentShows(CompanyId, numberOfAttempts)),
                    Task.Run(() => AnalyticContentEfficiency(CompanyId, numberOfAttempts)),
                    Task.Run(() => AnalyticContentPool(CompanyId, numberOfAttempts)),
                    Task.Run(() => AnalyticHomeDashboard(CompanyId, numberOfAttempts)),
                    Task.Run(() => AnalyticOfficeEfficiency(CompanyId, numberOfAttempts)),
                    Task.Run(() => AnalyticRatingProgress(CompanyId, numberOfAttempts)),
                    Task.Run(() => AnalyticRatingRatingUsers(CompanyId, numberOfAttempts)),
                    Task.Run(() => AnalyticRatingOffices(CompanyId, numberOfAttempts)),
                    Task.Run(() => AnalyticReportActiveEmployee(CompanyId, numberOfAttempts)),
                    Task.Run(() => AnalyticReportUserPartial(CompanyId, numberOfAttempts)),
                    Task.Run(() => AnalyticReportUserFull(CompanyId, numberOfAttempts)),
                    Task.Run(() => AnalyticServiceQualityComponent(CompanyId, numberOfAttempts)),
                    Task.Run(() => AnalyticServiceQualityDashboard(CompanyId, numberOfAttempts)),
                    Task.Run(() => AnalyticServiceQualityRating(CompanyId, numberOfAttempts)),
                    Task.Run(() => AnalyticServiceQualitySatisfactionStats(CompanyId, numberOfAttempts)),
                    Task.Run(() => AnalyticSpeechEmployeeRating(CompanyId, numberOfAttempts)),
                    Task.Run(() => AnalyticSpeechEmployeePhraseTable(CompanyId, numberOfAttempts)),
                    Task.Run(() => AnalyticSpeechPhraseTypeCount(CompanyId, numberOfAttempts)),
                    Task.Run(() => AnalyticSpeechWordCloud(CompanyId, numberOfAttempts)),
                    Task.Run(() => AnalyticWeeklyReportUser(CompanyId, numberOfAttempts)),
                    Task.Run(() => CampaignContentReturnCampaignWithContent(CompanyId, numberOfAttempts)),
                    Task.Run(() => CampaignContentCreateCampaignWithContent(CompanyId, numberOfAttempts)),
                    Task.Run(() => CampaignContentGetAllContent(CompanyId, numberOfAttempts)),
                    Task.Run(() => CampaignContentSaveNewContent(CompanyId, numberOfAttempts)),
                    Task.Run(() => CatalogueCountry(CompanyId, numberOfAttempts)),
                    Task.Run(() => CatalogueRole(CompanyId, numberOfAttempts)),
                    Task.Run(() => CatalogueDeviceType(CompanyId, numberOfAttempts)),
                    Task.Run(() => CatalogueIndustry(CompanyId, numberOfAttempts)),
                    Task.Run(() => CatalogueLanguage(CompanyId, numberOfAttempts)),
                    Task.Run(() => CataloguePhraseType(CompanyId, numberOfAttempts)),
                    Task.Run(() => CatalogueAlertType(CompanyId, numberOfAttempts)),
                    Task.Run(() => CompanyReportGetReport(CompanyId, numberOfAttempts)),
                    Task.Run(() => DemonstrationFlushStats(CompanyId, numberOfAttempts)),
                    Task.Run(() => DemonstrationPoolAnswer(CompanyId, numberOfAttempts)),
                    Task.Run(() => LoggingSendLogPost(CompanyId, numberOfAttempts)),
                    Task.Run(() => SessionSessionStatus(CompanyId, numberOfAttempts)),
                    Task.Run(() => SessionAlertNotSmile(CompanyId, numberOfAttempts)),
                    Task.Run(() => SiteFeedBack(CompanyId, numberOfAttempts)),
                    Task.Run(() => UserGetAllCompanyUsers(CompanyId, numberOfAttempts)),
                    Task.Run(() => UserCompanies(CompanyId, numberOfAttempts)),
                    Task.Run(() => UserPhraseLibLibrary(CompanyId, numberOfAttempts)),
                    Task.Run(() => UserPhraseLibCreateCompanyPhrase(CompanyId, numberOfAttempts)),
                    Task.Run(() => UserCompanyPhraseReturnAttachedToCompanyPhrases(CompanyId, numberOfAttempts)),
                    Task.Run(() => UserCompanyPhraseAttachLibraryPhrasesToCompany(CompanyId, numberOfAttempts)),
                    Task.Run(() => UserDialogue(CompanyId, numberOfAttempts)),
                    Task.Run(() => UserDialogueInclude(CompanyId, numberOfAttempts)),
                    Task.Run(() => UserAlert(CompanyId, numberOfAttempts))
                };
                Task.WaitAll(taskList.ToArray());
                foreach(var task in taskList)
                {
                    report.Add(task.Result);
                }
                PrintAPIWorkTime(fileName, report);                
            }
            catch(Exception ex)
            {
                System.Console.WriteLine(ex);
                throw;
            }
            
            await RemoveCreatedCompanyAndUser();
            return "report generated";
        }
        
        private void PrintAPIWorkTime(string fileName, List<ResponceReportModel> Report)
        {
            System.Console.WriteLine($"ReportListCount: {Report.Count}");
            using(ExcellDocument document = new ExcellDocument(fileName))
            {
                var workbookPart = document.AddWorkbookPart();
                var worksheet = document.AddWorksheetPart(ref workbookPart);
                var sheetData = document.AddSheet(ref workbookPart, worksheet, "Report");
                var shareStringTablePart = workbookPart.AddNewPart<SharedStringTablePart>();
                shareStringTablePart.SharedStringTable = new SharedStringTable();

                int index = 2;
                document.AddCell(ref sheetData, "RequestName", 1, 1);
                document.AddCell(ref sheetData, "200 Response", 1, 2);
                document.AddCell(ref sheetData, "Other Response", 1, 3);
                foreach(var item in Report)
                {
                    document.AddCell(ref sheetData, item.Name, index, 1);
                    document.AddCell(ref sheetData, $"{item.NumberOf200Responce}", index, 2);
                    document.AddCell(ref sheetData, $"{item.NumberOfOtherResponce}", index, 3);
                    index++;
                }

                document.SaveDocument(ref workbookPart);
                document.CloseDocument();                
            }
        }
        
    #region Methods        
        private ResponceReportModel AccountRegister()
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;

            var industry = _repository.GetAsQueryable<CompanyIndustry>()
                .FirstOrDefault(p => p.CompanyIndustryName == "APITESTINDUSTRY");
            if(industry == null)
            {
                industry = new CompanyIndustry()
                    {
                        CompanyIndustryId = Guid.NewGuid(),
                        CompanyIndustryName = "APITESTINDUSTRY",
                        SatisfactionIndex = 0,
                        LoadIndex = 0.5,
                        CrossSalesIndex = 0.04
                    };
                _industryId = industry.CompanyIndustryId;
                _repository.Create<CompanyIndustry>(industry);  
                _repository.Save();
            }
            
            var corporation = new Corporation()
            {
                Id = Guid.NewGuid(),
                Name = "APITestCorporation"
            };            
            _corporationId = corporation.Id;
            _repository.Create<Corporation>(corporation);
            _repository.Save();           
            

            var json = JsonConvert.SerializeObject(new UserRegister{                
                FullName = "Ivanov Ivan Ivanovich",
                Email = _companySettings.UserEmail,
                Password = _companySettings.Password,
                CompanyName = _companySettings.CompanyName,
                LanguageId = 2,
                CountryId = Guid.Parse("be6a6509-7c9e-4d63-b787-5725bbbb2f26"),
                CompanyIndustryId = industry.CompanyIndustryId,
                CorporationId = _corporationId,
                IsExtended = true,
                TimeZone = "-3",
                Role = "Supervisor",
                MondayBeg = new DateTime(2, 2, 2, 10, 0, 0),
                MondayEnd = new DateTime(2, 2, 2, 19, 0, 0),
                TuesdayBeg = new DateTime(2, 2, 2, 10, 0, 0), 
                TuesdayEnd = new DateTime(2, 2, 2, 19, 0, 0), 
                WednesdayBeg = new DateTime(2, 2, 2, 10, 0, 0),
                WednesdayEnd = new DateTime(2, 2, 2, 19, 0, 0),
                ThursdayBeg = new DateTime(2, 2, 2, 10, 0, 0),
                ThursdayEnd = new DateTime(2, 2, 2, 19, 0, 0),
                FridayBeg = new DateTime(2, 2, 2, 10, 0, 0),
                FridayEnd = new DateTime(2, 2, 2, 19, 0, 0),
                SaturdayBeg = new DateTime(2, 2, 2, 10, 0, 0),
                SaturdayEnd = new DateTime(2, 2, 2, 19, 0, 0),
                SundayBeg = new DateTime(2, 2, 2, 10, 0, 0),
                SundayEnd = new DateTime(2, 2, 2, 19, 0, 0)
            });
            var data = Encoding.ASCII.GetBytes(json);
            var url = $"{_urlSettings.Host}api/Account/Register";
            var request = WebRequest.Create(url);

            request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "POST";
            request.ContentType = "application/json-patch+json";   
            request.ContentLength = data.Length;    

            using(var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            try
            {
                var responce = (HttpWebResponse)request.GetResponse();
                if(responce.StatusCode == HttpStatusCode.OK)
                    numberOf200Responce++;
                else
                    numberOfOtherResponce++;
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
                numberOfOtherResponce++;
            }      

            return new ResponceReportModel
            {
                Name = "AccountRegister",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }

        private ResponceReportModel AccountGenerateToken(int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;            
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {    
                var json = JsonConvert.SerializeObject(new AccountAuthorization(){                
                    UserName=_companySettings.UserEmail,                
                    Password=_companySettings.Password
                });
                var data = Encoding.ASCII.GetBytes(json);
                var url = $"{_urlSettings.Host}api/Account/GenerateToken";
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
                    var responce = (HttpWebResponse)request.GetResponse();
                    var dataStream = responce.GetResponseStream();
                    var reader = new StreamReader(dataStream);
                    _token = reader.ReadToEnd();
                    System.Console.WriteLine($"token: {_token}");
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++; 
                }
                catch(Exception ex)
                {
                    System.Console.WriteLine(ex);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AccountGenerateToken",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel AccountChangePassword(int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {    
                var json = JsonConvert.SerializeObject(new {                
                    userName=_companySettings.UserEmail,                
                    password=_companySettings.Password,
                    remember=true             
                });
                var data = Encoding.ASCII.GetBytes(json);
                var url = $"{_urlSettings.Host}api/Account/ChangePassword";
                var request = WebRequest.Create(url);
                
                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "POST";
                request.ContentType = "application/json-patch+json";   
                request.ContentLength = data.Length;    

                using(var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AccountChangePassword",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel AnalyticClientProfileGenderAgeStructure(int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/AnalyticClientProfile/GenderAgeStructure";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                System.Console.WriteLine($"AnalyticClientProfileGenderAgeStructuretoken: {_token}");
                request.Headers.Add("Authorization", $"Bearer {_token}");
            
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine($"catch");
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticClientProfileGenderAgeStructure",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        
        private ResponceReportModel AnalyticContentContentShows(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var companyId = CompanyId;
                var dialogue =  _context.Dialogues
                    .Include(p => p.ApplicationUser)
                    .FirstOrDefault(p => p.BegTime.Date > DateTime.Now.Date.AddDays(-4)
                        && p.ApplicationUser.CompanyId == companyId);
                var url = $"{_urlSettings.Host}api/AnalyticContent/ContentShows?dialogueId={_dialogueId}";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticContentContentShows",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }

        private ResponceReportModel AnalyticContentEfficiency(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/AnalyticContent/Efficiency";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
            
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticContentEfficiency",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        
        private ResponceReportModel AnalyticContentPool(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {        
                var url = $"{_urlSettings.Host}api/AnalyticContent/Poll";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
            
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticContentPool",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel AnalyticHomeDashboard(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {                
                var url = $"{_urlSettings.Host}api/AnalyticHome/NewDashboard";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
            
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticHomeDashboard",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }

        private ResponceReportModel AnalyticOfficeEfficiency(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/AnalyticOffice/Efficiency";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
            
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticOfficeEfficiency",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel AnalyticRatingProgress(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/AnalyticRating/Progress";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
            
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticRatingProgress",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }

        private ResponceReportModel AnalyticRatingRatingUsers(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/AnalyticRating/RatingUsers";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
            
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticRatingRatingUsers",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel AnalyticRatingOffices(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/AnalyticRating/RatingOffices";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
            
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticRatingOffices",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel AnalyticReportActiveEmployee(Guid CompanyId, int numberOfAttempts)
        {   
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/AnalyticReport/ActiveEmployee";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
            
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticReportActiveEmployee",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel AnalyticReportUserPartial(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {               
                var url = $"{_urlSettings.Host}api/AnalyticReport/UserPartial";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticReportUserPartial",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel AnalyticReportUserFull(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {               
                var url = $"{_urlSettings.Host}api/AnalyticReport/UserFull";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticReportUserFull",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel AnalyticServiceQualityComponent(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {             
                var url = $"{_urlSettings.Host}api/AnalyticServiceQuality/Components";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticServiceQualityComponent",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel AnalyticServiceQualityDashboard(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {               
                var url = $"{_urlSettings.Host}api/AnalyticServiceQuality/Dashboard";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticServiceQualityDashboard",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel AnalyticServiceQualityRating(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {             
                var url = $"{_urlSettings.Host}api/AnalyticServiceQuality/Rating";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticServiceQualityRating",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel AnalyticServiceQualitySatisfactionStats(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {             
                var url = $"{_urlSettings.Host}api/AnalyticServiceQuality/SatisfactionStats";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticServiceQualitySatisfactionStats",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel AnalyticSpeechEmployeeRating(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {         
                var url = $"{_urlSettings.Host}api/AnalyticSpeech/EmployeeRating";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticSpeechEmployeeRating",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel AnalyticSpeechEmployeePhraseTable(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {   
                var url = $"{_urlSettings.Host}api/AnalyticSpeech/PhraseTable";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticSpeechEmployeePhraseTable",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel AnalyticSpeechPhraseTypeCount(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/AnalyticSpeech/PhraseTypeCount";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticSpeechPhraseTypeCount",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel AnalyticSpeechWordCloud(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/AnalyticSpeech/WordCloud";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticSpeechWordCloud",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }        
        private ResponceReportModel AnalyticWeeklyReportUser(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var applicationUserId = _userId;
                var url = $"{_urlSettings.Host}api/AnalyticWeeklyReport/User?applicationUserId={_userId}";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "AnalyticWeeklyReportUser",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel CampaignContentReturnCampaignWithContent(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {              
                var url = $"{_urlSettings.Host}api/CampaignContent/Campaign";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "CampaignContentReturnCampaignWithContent",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel CampaignContentCreateCampaignWithContent(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;

            var contentId = Guid.NewGuid();
            var content = new Content()
            {
                ContentId = contentId,
                RawHTML = "<body>APITestContent</body>",
                Name = "APITestContent",
                Duration = 30,
                CompanyId = CompanyId,
                StatusId = 3
            };
            _repository.Create<Content>(content);
            _repository.Save();

            for(int i = 0; i < numberOfAttempts; i++)
            {            
                var url = $"{_urlSettings.Host}api/CampaignContent/Campaign";
                var request = WebRequest.Create(url);
                var campaignId = Guid.NewGuid();
                var campaignContentId = Guid.NewGuid();
                var cmp = new Campaign()
                {
                    CampaignId = campaignId,
                    Name = "Тестовая компания Иванова Иван Ивановича",
                    IsSplash = true,
                    GenderId = 0,
                    BegAge = 20,
                    BegDate = DateTime.Now.AddDays(-1),
                    EndDate = DateTime.Now.AddDays(1),
                    CreationDate = DateTime.Now.AddDays(-2),
                    CompanyId = CompanyId,
                    StatusId = 3
                };
                var campaignContents = new List<CampaignContent>()
                {
                    new CampaignContent()
                        {
                            CampaignContentId = campaignContentId,
                            SequenceNumber = 1,
                            ContentId = contentId,
                            CampaignId = campaignId,
                            StatusId = 3
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
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "CampaignContentCreateCampaignWithContent",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel CampaignContentGetAllContent(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {             
                var url = $"{_urlSettings.Host}api/CampaignContent/Content?inActive=false&screenshot=false&isTemplate=false";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "CampaignContentGetAllContent",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel CampaignContentSaveNewContent(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            var url = $"{_urlSettings.Host}api/CampaignContent/Content";
            var testFileName = $"TestImage.jpg";
            var FS = new FileStream(testFileName, FileMode.Open, FileAccess.Read);
            var fileData = new  byte[FS.Length];
            FS.Read(fileData, 0, fileData.Length);
            FS.Close();


            for(int i = 0; i < numberOfAttempts; i++)
            {                   
                var request = WebRequest.Create(url);
                
                var model = new Content()
                {
                    ContentId = Guid.NewGuid(),
                    RawHTML = "<div>Тестовая разметка</div>",
                    Name = "Test Content",
                    Duration = 100,
                    CompanyId = CompanyId,
                    IsTemplate = false,
                    CreationDate = DateTime.Now.AddDays(-2),
                    UpdateDate = DateTime.Now.AddDays(2),
                    StatusId = 3
                };
                
                string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
                string contentType = "multipart/form-data; boundary=" + formDataBoundary;

                var dictionary = new Dictionary<string, object>();
                dictionary.Add("data", JsonConvert.SerializeObject(model));
                dictionary.Add("file", new FileParameter(fileData, testFileName, "image/jpeg"));

                byte[] formData = GetMultipartFormData(dictionary, formDataBoundary);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "POST";
                // request.ContentType = "application/json-patch+json";   
                request.ContentType = contentType;
                request.ContentLength = formData.Length;       
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                using(var stream = request.GetRequestStream())
                {
                    stream.Write(formData, 0, formData.Length);
                }
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }            
            return new ResponceReportModel
            {
                Name = "CampaignContentSaveNewContent",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        
        private ResponceReportModel CatalogueCountry(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/Catalogue/Country";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json"; 
                request.Headers.Add("Authorization", $"Bearer {_token}");  
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "CatalogueCountry",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel CatalogueRole(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {  
                var url = $"{_urlSettings.Host}api/Catalogue/Role";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json"; 
                request.Headers.Add("Authorization", $"Bearer {_token}");  
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "CatalogueRole",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel CatalogueDeviceType(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/Catalogue/DeviceType";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "CatalogueDeviceType",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel CatalogueIndustry(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/Catalogue/Industry";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json"; 
                request.Headers.Add("Authorization", $"Bearer {_token}");  
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "CatalogueIndustry",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel CatalogueLanguage(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/Catalogue/Language";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json"; 
                request.Headers.Add("Authorization", $"Bearer {_token}");  
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "CatalogueLanguage",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel CataloguePhraseType(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/Catalogue/PhraseType";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json"; 
                request.Headers.Add("Authorization", $"Bearer {_token}");  
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "CataloguePhraseType",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel CatalogueAlertType(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/Catalogue/AlertType";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json"; 
                request.Headers.Add("Authorization", $"Bearer {_token}");  
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "CatalogueAlertType",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel CompanyReportGetReport(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/CompanyReport/GetReport";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json"; 
                request.Headers.Add("Authorization", $"Bearer {_token}");  
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "CompanyReportGetReport",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel DemonstrationFlushStats(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;            

            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/DemonstrationV2/FlushStats";
                var request = WebRequest.Create(url);
                
                var model = new List<SlideShowSession>()
                {
                    new SlideShowSession()
                    {
                        CampaignContentId = _campaignContentId,
                        ApplicationUserId = _userId,
                        BegTime = DateTime.Now.AddDays(-1),
                        EndTime = DateTime.Now.AddDays(1),
                        ContentType = "url",
                        Url = "https://www.heedbook.com/",
                        DeviceId = _deviceId,
                        DialogueId = _dialogueId                       
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
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "DemonstrationFlushStats",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel DemonstrationPoolAnswer(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/DemonstrationV2/PollAnswer";
                var request = WebRequest.Create(url);
                
                var model = new CampaignContentAnswerModel()
                {
                    CampaignContentId = _campaignContentId,
                    Answer = "Тестовый ответ",
                    Time = DateTime.Now,
                    DeviceId = _deviceId,
                    ApplicationUserId = _userId
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
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "DemonstrationPoolAnswer",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        
        private ResponceReportModel LoggingSendLogPost(Guid CompanyId, int numberOfAttempts)
        {           
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {           
                var url = $"{_urlSettings.Host}api/Logging/SendLog";
                var request = WebRequest.Create(url);      
                var model = new JObject();
                var json = JsonConvert.SerializeObject(model);
                var data = Encoding.ASCII.GetBytes(json);
                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "POST";
                request.ContentType = "application/json-patch+json";   
                request.Headers.Add("Authorization", $"Bearer {_token}");
                using(var requestStream = request.GetRequestStream())
                {
                    requestStream.Write(data, 0, data.Length);
                }
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "LoggingSendLogPost",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel MediaFileFileGet(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            var testFileName = $"TestImage.jpg";
            
            var task = Task.Run(() => _sftpClient.UploadAsync(testFileName, "mediacontent/", testFileName));
            Task.WaitAll(new Task[]{task});

            for(int i = 0; i < numberOfAttempts; i++)
            {
                var containerName = "mediacontent";
                var fileName = "testFileName";
                var url = $"{_urlSettings.Host}api/MediaFile/File?containerName={containerName}&fileName={fileName}";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";   
                    
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "MediaFileFileGet",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel MediaFilePost(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/MediaFile/File";
                var request = WebRequest.Create(url);            
                
                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "POST";
                request.ContentType = "application/json-patch+json";                   
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "MediaFilePost",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel PaymentTariff(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/Payment/Tariff";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";                
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "PaymentTariff",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel PaymentCheckoutResponce(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/Payment/CheckoutResponse";
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
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "PaymentCheckoutResponce",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel SessionSessionStatus(Guid CompanyId, int numberOfAttempts)
        {   
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/Session/SessionStatus";
                var request = WebRequest.Create(url);
                
                var model = new SessionParams()
                {
                    ApplicationUserId = _userId,
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
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "SessionSessionStatus",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel SessionAlertNotSmile(Guid CompanyId, int numberOfAttempts)
        {    
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/Session/AlertNotSmile";
                var request = WebRequest.Create(url);
                
                var model = new AlertModel()
                {
                    ApplicationUserId = _userId,
                    DeviceId = _deviceId
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
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "SessionAlertNotSmile",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel SiteFeedBack(Guid CompanyId, int numberOfAttempts)
        {    
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/Site/Feedback";
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
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "SiteFeedBack",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel UserGetAllCompanyUsers(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/User/User";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";                
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "UserGetAllCompanyUsers",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel UserPost(Guid CompanyId, int numberOfAttempts)
        {           
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/User/User";
                var request = WebRequest.Create(url);            
                var employeeId = _repository.GetAsQueryable<ApplicationRole>().FirstOrDefault(p => p.Name == "Employee").Id;
                var postUser = new PostUser()
                {
                    FullName = $"newApiTESTUser{i}",
                    Email = $"newApiTESTUser{i}@heedbook.com",
                    EmployeeId = $"{i}",
                    RoleId = employeeId,
                    Password = $"Test_User12345",
                    CompanyId = CompanyId
                };

                string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
                string contentType = "multipart/form-data; boundary=" + formDataBoundary;

                var dictionary = new Dictionary<string, object>();
                dictionary.Add("data", JsonConvert.SerializeObject(postUser));

                byte[] formData = GetMultipartFormData(dictionary, formDataBoundary);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "POST";
                request.ContentType = "application/json-patch+json";                   
                request.Headers.Add("Authorization", $"Bearer {_token}");  
                request.ContentLength = formData.Length;          
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(formData, 0, formData.Length);
                }
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "UserPost",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private static byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary)
        {
            Encoding encoding = Encoding.UTF8;
            Stream formDataStream = new System.IO.MemoryStream();
            bool needsCLRF = false;

            foreach (var param in postParameters)
            {
                // Thanks to feedback from commenters, add a CRLF to allow multiple parameters to be added.
                // Skip it on the first parameter, add it to subsequent parameters.
                if (needsCLRF)
                    formDataStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));

                needsCLRF = true;

                if (param.Value is FileParameter)
                {
                    FileParameter fileToUpload = (FileParameter)param.Value;

                    // Add just the first part of this param, since we will write the file data directly to the Stream
                    string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n",
                        boundary,
                        param.Key,
                        fileToUpload.FileName ?? param.Key,
                        fileToUpload.ContentType ?? "application/octet-stream");

                    formDataStream.Write(encoding.GetBytes(header), 0, encoding.GetByteCount(header));

                    // Write the file data directly to the Stream, rather than serializing it to a string.
                    formDataStream.Write(fileToUpload.File, 0, fileToUpload.File.Length);
                }
                else
                {
                    string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
                        boundary,
                        param.Key,
                        param.Value);
                    formDataStream.Write(encoding.GetBytes(postData), 0, encoding.GetByteCount(postData));
                }
            }

            // Add the end of the request.  Start with a newline
            string footer = "\r\n--" + boundary + "--\r\n";
            formDataStream.Write(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer));

            // Dump the Stream into a byte[]
            formDataStream.Position = 0;
            byte[] formData = new byte[formDataStream.Length];
            formDataStream.Read(formData, 0, formData.Length);
            formDataStream.Close();

            return formData;
        }
        private ResponceReportModel UserCompanies(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/User/Companies";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";                
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "UserCompanies",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }        
        private ResponceReportModel UserPhraseLibLibrary(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/User/PhraseLib";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";                
                request.Headers.Add("Authorization", $"Bearer {_token}");            
                
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "UserPhraseLibLibrary",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel UserPhraseLibCreateCompanyPhrase(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/User/PhraseLib";
                var request = WebRequest.Create(url);
                var phraseType = _repository.GetAsQueryable<PhraseType>().FirstOrDefault();
                var salesStage = _repository.GetAsQueryable<SalesStage>().FirstOrDefault();
                var model = new PhrasePost()
                {
                    PhraseText = "Артемий Лебедев",
                    PhraseTypeId = phraseType.PhraseTypeId,
                    LanguageId = 2,
                    SalesStageId = salesStage.SalesStageId,
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
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "UserPhraseLibCreateCompanyPhrase",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel UserCompanyPhraseReturnAttachedToCompanyPhrases(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/User/CompanyPhrase";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";                
                request.Headers.Add("Authorization", $"Bearer {_token}");            
            
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "UserCompanyPhraseReturnAttachedToCompanyPhrases",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel UserCompanyPhraseAttachLibraryPhrasesToCompany(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/User/CompanyPhrase";
                var request = WebRequest.Create(url);                
                // var listOfPhrases = new List<Guid>()
                // {
                //     _phraseId
                // };
                // var json = JsonConvert.SerializeObject(listOfPhrases);
                // System.Console.WriteLine($"listPhrases: {json}");
                // var data = Encoding.ASCII.GetBytes(json);
                // request.ContentLength = data.Length;
                // using(var stream = request.GetRequestStream())
                // {
                //     stream.Write(data, 0, data.Length);
                // }
                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";
                request.Headers.Add("Authorization", $"Bearer {_token}");
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "UserCompanyPhraseAttachLibraryPhrasesToCompany",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel UserDialogue(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/User/Dialogue";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";                
                request.Headers.Add("Authorization", $"Bearer {_token}");            
            
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "UserDialogue",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel UserDialogueInclude(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/User/DialogueInclude?dialogueId={_dialogueId}";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";                
                request.Headers.Add("Authorization", $"Bearer {_token}");            
            
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "UserDialogueInclude",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private ResponceReportModel UserAlert(Guid CompanyId, int numberOfAttempts)
        {
            int numberOf200Responce = 0;
            int numberOfOtherResponce = 0;
            var start = DateTime.Now;
            for(int i = 0; i < numberOfAttempts; i++)
            {
                var url = $"{_urlSettings.Host}api/User/Alert";
                var request = WebRequest.Create(url);

                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "GET";
                request.ContentType = "application/json-patch+json";                
                request.Headers.Add("Authorization", $"Bearer {_token}");            
            
                try
                {
                    var responce = (HttpWebResponse)request.GetResponse();
                    if(responce.StatusCode == HttpStatusCode.OK)
                        numberOf200Responce++;
                    else
                        numberOfOtherResponce++;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e);
                    numberOfOtherResponce++;
                }
            }
            
            return new ResponceReportModel
            {
                Name = "UserAlert",
                Duration = DateTime.Now.Subtract(start).TotalMilliseconds/numberOfAttempts,
                NumberOf200Responce = numberOf200Responce,
                NumberOfOtherResponce = numberOfOtherResponce,
            };
        }
        private async Task RemoveCreatedCompanyAndUser()
        {
            try
            {
                var corporation = _repository.GetAsQueryable<Corporation>().FirstOrDefault(p => p.Name == "APITestCorporation");
                var coprorartionId = corporation.Id;
                var user = _repository.GetAsQueryable<ApplicationUser>().FirstOrDefault(p => p.Email == _companySettings.UserEmail);
                var companys = _repository.GetAsQueryable<Company>().Where(x => x.CorporationId == corporation.Id);
                var companyIds = companys.Select(p => p.CompanyId).ToList();
                var users = _repository.GetWithInclude<ApplicationUser>(x => companyIds.Contains((Guid)x.CompanyId), o => o.UserRoles).ToList();
                var tariffs = _repository.GetAsQueryable<Tariff>().Where(x => companyIds.Contains((Guid)x.CompanyId)).ToList();
                var tarifIds = tariffs.Select(p => p.TariffId).ToList();

                var taskTransactions = _repository.GetAsQueryable<HBData.Models.Transaction>().Where(x => tarifIds.Contains((Guid)x.TariffId)).ToListAsync();
                taskTransactions.Wait();
                var transactions = taskTransactions.Result;
                var userRoles = users.SelectMany(x => x.UserRoles).ToList();
                var contents = _repository.GetAsQueryable<Content>().Where(x => companyIds.Contains((Guid)x.CompanyId)).ToList();
                var campaigns = _repository.GetWithInclude<Campaign>(x => companyIds.Contains((Guid)x.CompanyId), p => p.CampaignContents).ToList();
                var campaignContents = campaigns.SelectMany(x => x.CampaignContents).ToList();
                var campaignContentIds = campaignContents.Select(p => p.CampaignContentId).ToList();
                var phrases = _repository.GetAsQueryable<PhraseCompany>().Where(x => companyIds.Contains((Guid)x.CompanyId)).ToList();
                var workingTimes = _repository.GetAsQueryable<WorkingTime>().Where(x => companyIds.Contains((Guid)x.CompanyId)).ToList();
                var salesStagesPhrases = _repository.GetAsQueryable<SalesStagePhrase>().Where(x => x.CorporationId == coprorartionId).ToList();
                var devices = _repository.GetAsQueryable<Device>().Where(p => companyIds.Contains((Guid)p.CompanyId)).ToList();
                var deviceIds = devices.Select(q => q.DeviceId).ToList();
                var clients = _repository.GetAsQueryable<Client>().Where(p => companyIds.Contains((Guid)p.CompanyId)).ToList();
                var dialogues = _repository.GetAsQueryable<Dialogue>().Where(p => deviceIds.Contains(p.DeviceId)).ToList();            
                var slideShowSessions = _repository.GetAsQueryable<SlideShowSession>().Where(p => campaignContentIds.Contains((Guid)p.CampaignContentId)).ToList();
                var alerts = _repository.GetAsQueryable<Alert>().Where(p => deviceIds.Contains(p.DeviceId)).ToList();

                if (salesStagesPhrases.Count() != 0)
                    _repository.Delete<SalesStagePhrase>(salesStagesPhrases);
                _repository.Save();


                if (alerts != null && alerts.Count() != 0)
                    _repository.Delete<Alert>(alerts);
                if (slideShowSessions != null && slideShowSessions.Count() != 0)
                    _repository.Delete<SlideShowSession>(slideShowSessions);
                if (devices != null && devices.Count() != 0)
                    _repository.Delete<Device>(devices);
                if (clients != null && clients.Count() != 0)
                    _repository.Delete<Client>(clients);
                if (phrases != null && phrases.Count() != 0)
                    _repository.Delete<PhraseCompany>(phrases);
                if (campaignContents.Count() != 0)
                    _repository.Delete<CampaignContent>(campaignContents); 
                if (campaigns.Count() != 0)
                    _repository.Delete<Campaign>(campaigns);
                if (contents.Count() != 0)
                    _repository.Delete<Content>(contents);
                
                
                _repository.Delete<ApplicationUserRole>(userRoles);
                _repository.Delete<HBData.Models.Transaction>(transactions);
                _repository.Delete<ApplicationUser>(users);
                _repository.Delete<Tariff>(tariffs);
                _repository.Delete<WorkingTime>(workingTimes);

                _repository.Delete<Company>(companys);
                _repository.Delete<Corporation>(corporation);
            
                _repository.Save();

                var task = Task.Run(() => _sftpClient.DeleteFileIfExistsAsync("mediacontent/TestImage.jpg"));
                Task.WaitAll(new Task[]{task});
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
            }
        }
        #endregion   
    }
}