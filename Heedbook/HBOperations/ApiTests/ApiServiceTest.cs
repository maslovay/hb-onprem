using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBData.Models;
using HBData.Models.AccountViewModels;
using HBData.Repository;
using Microsoft.Extensions.Configuration;
using Moq;
using UserOperations.AccountModels;
using UserOperations.Models.AnalyticModels;
using UserOperations.Providers;
using UserOperations.Providers.Interfaces;
using UserOperations.Services;
using UserOperations.Utils;

namespace ApiTests
{
    public abstract class ApiServiceTest : IDisposable
    {
        protected Mock<IAccountProvider> accountProviderMock;
        protected Mock<IAnalyticCommonProvider> commonProviderMock;
        protected Mock<IConfiguration> configMock;
        protected Mock<IDBOperations> dbOperationMock;
        protected Mock<IRequestFilters> filterMock;
        protected Mock<IHelpProvider> helpProvider;
        protected Mock<IMailSender> mailSenderMock;
        protected Mock<ILoginService> moqILoginService;
        protected Mock<IGenericRepository> repositoryMock;

        protected void BaseInit()
        {
            TestData.beg = "20191001";
            TestData.end = "20191002";
            TestData.begDate = (new DateTime(2019, 10, 03)).Date;
            TestData.endDate = (new DateTime(2019, 10, 05)).Date;
            TestData.prevDate = (new DateTime(2019, 10, 01)).Date;
            TestData.token = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhbm5zYW1vbHVrX3Rlc3RAZ21haWwuY29tIiwianRpIjoiZmY3Yjc4NGQtMTEzMi00ZmY0LThlN2ItODU4YTBhMDVhMzE3IiwiYXBwbGljYXRpb25Vc2VySWQiOiJhNmI2NjgzNS1hNDEyLTRjMjAtODBiNy0yZGNhN2VhZTRjZDYiLCJhcHBsaWNhdGlvblVzZXJOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlJZCI6ImJkNGM0MmIwLWRlNmEtNDkxNS1hYzY5LWE1ZjAzNjAxOWM5ZCIsImNvcnBvcmF0aW9uSWQiOiIiLCJsYW5ndWFnZUNvZGUiOiIxIiwicm9sZSI6Ik1hbmFnZXIiLCJmdWxsTmFtZSI6ImFubnNhbW9sdWtfdGVzdEBnbWFpbC5jb20iLCJhdmF0YXIiOiIiLCJleHAiOjE1NzQ5MzQxNDUsImlzcyI6Imh0dHBzOi8vaGVlZGJvb2suY29tIiwiYXVkIjoiaHR0cHM6Ly9oZWVkYm9vay5jb20ifQ.rRRAcst-r0mD4jkn80L8yKLf9xGPhGxVaNy0tRgKUXM";
            TestData.tokenclaims = TestData.GetClaims();
            TestData.companyIds = TestData.GetCompanyIds();
            TestData.email = $"test@heedbook.com";

            accountProviderMock = new Mock<IAccountProvider>();
            commonProviderMock = new Mock<IAnalyticCommonProvider>();
            configMock = new Mock<IConfiguration>();
            dbOperationMock = new Mock<IDBOperations>();
            filterMock = new Mock<IRequestFilters>(MockBehavior.Loose);
            helpProvider = new Mock<IHelpProvider>();
            mailSenderMock = new Mock<IMailSender>();
            moqILoginService = new Mock<ILoginService>(MockBehavior.Loose);
            repositoryMock = new Mock<IGenericRepository>();
        }
        protected virtual void InitData()
        {
          
        }

        protected virtual void InitServices()
        {

        }
        public void Setup()
        {
            BaseInit();
            InitData();
            InitServices();
        }

        public Mock<ILoginService> MockILoginService(Mock<ILoginService> moqILoginService)
        {
            moqILoginService.Setup(p => p.CheckUserLogin(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
            moqILoginService.Setup(p => p.SaveErrorLoginHistory(It.IsAny<Guid>(), It.IsAny<string>()))
                .Returns(true);
            var dict = TestData.GetClaims();
            moqILoginService.Setup(p => p.GetDataFromToken(It.IsAny<string>(), out dict, null))
                .Returns(true);
            moqILoginService.Setup(p => p.GeneratePasswordHash(It.IsAny<string>()))
                .Returns("Hash");
            moqILoginService.Setup(p => p.SavePasswordHistory(It.IsAny<Guid>(), It.IsAny<string>()))
                .Returns(true);
            moqILoginService.Setup(p => p.GeneratePass(6))
                .Returns("123456");
            moqILoginService.Setup(p => p.CreateTokenForUser(It.IsAny<ApplicationUser>(), It.IsAny<bool>()))
                .Returns("Token");
            return moqILoginService;
        }
        public Mock<IMailSender> MockIMailSender(Mock<IMailSender> moqIMailSender)
        {
            moqIMailSender.Setup(p => p.SendRegisterEmail(new HBData.Models.ApplicationUser()))
                .Returns(Task.FromResult(0));
            moqIMailSender.Setup(p => p.SendPasswordChangeEmail(new HBData.Models.ApplicationUser(), "password"))
                .Returns(Task.FromResult(0));
            return moqIMailSender;
        }
        public Mock<IAccountProvider> MockIAccountProvider(Mock<IAccountProvider> moqIAccountProvider)
        {            
            moqIAccountProvider.Setup(p => p.GetStatusId(It.IsAny<string>()))
                .Returns((string p) => p == "Active" ? 3 : (p == "Inactive" ? 5 : 0));
            moqIAccountProvider.Setup(p => p.CompanyExist(It.IsAny<string>()))
                .Returns(Task.FromResult(false));
            moqIAccountProvider.Setup(p => p.EmailExist(It.IsAny<string>()))
                .Returns(Task.FromResult(false));
            moqIAccountProvider.Setup(p => p.AddNewCompanysInBase(new UserRegister(), Guid.NewGuid()))
                .Returns(Task.FromResult(new Company()));
            moqIAccountProvider.Setup(p => p.AddNewUserInBase(new UserRegister(), Guid.NewGuid()))
                .Returns(Task.FromResult(new ApplicationUser()));
            moqIAccountProvider.Setup(p => p.AddUserRoleInBase(new UserRegister(), new ApplicationUser()))
                .Returns(Task.FromResult(0));
            moqIAccountProvider.Setup(p => p.GetTariffs(Guid.NewGuid()))
                .Returns(0);
            moqIAccountProvider.Setup(p => p.CreateCompanyTariffAndtransaction(new Company()))
                .Returns(Task.FromResult(0));
            moqIAccountProvider.Setup(p => p.AddWorkerType(new Company()))
                .Returns(Task.FromResult(0));
            moqIAccountProvider.Setup(p => p.AddContentAndCampaign(new Company()))
                .Returns(Task.FromResult(0));
            moqIAccountProvider.Setup(p => p.SaveChangesAsync())
                .Callback(() => {});
            moqIAccountProvider.Setup(p => p.SaveChanges())
                .Callback(() => {});
            var user = new ApplicationUser(){UserName = "TestUser", StatusId = 3, PasswordHash = ""};
            moqIAccountProvider.Setup(p => p.GetUserIncludeCompany(It.IsAny<string>()))
                .Returns(user);
            moqIAccountProvider.Setup(p => p.GetUserIncludeCompany(It.IsAny<Guid>(), It.IsAny<AccountAuthorization>()))
                .Returns(user);
            moqIAccountProvider.Setup(p => p.RemoveAccount("email"))
                .Callback(() => {});          
            return moqIAccountProvider;
        }
        public Mock<IHelpProvider> MockIHelpProvider(Mock<IHelpProvider> moqIHelpProvider)
        {
            moqIHelpProvider.Setup(p => p.AddComanyPhrases());
            moqIHelpProvider.Setup(p => p.CreatePoolAnswersSheet(It.IsAny<List<AnswerInfo>>(), It.IsAny<string>()))
                .Returns(new MemoryStream());
            return moqIHelpProvider;
        }
        public Mock<IRequestFilters> MockIRequestFiltersProvider(Mock<IRequestFilters> moqIRequestFiltersProvider)
        {
            var list = new List<Guid>(){};
            moqIRequestFiltersProvider.Setup(p => p.CheckRolesAndChangeCompaniesInFilter( ref list, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));
            moqIRequestFiltersProvider.Setup(p => p.GetBegDate(It.IsAny<string>()))
                .Returns(new DateTime(2019, 10, 30));
            moqIRequestFiltersProvider.Setup(p => p.GetEndDate(It.IsAny<string>()))
                .Returns(new DateTime(2019, 11, 01));
            return moqIRequestFiltersProvider;
        }
        public Mock<IAnalyticOfficeProvider> MockIAnalyticOfficeProvider(Mock<IAnalyticOfficeProvider> moqIAnalyticOfficeProvider)
        {
            var sessionsInfo = new List<SessionInfo>
            {
                new SessionInfo
                {
                    ApplicationUserId = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0"),
                    BegTime = new DateTime(2019,10,04, 12, 19,00),
                    EndTime = new DateTime(2019,10,04,12,20,25),
                    CompanyId = Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182"),
                    FullName = "tuisv@heedbook.com",
                    IndustryId = Guid.Parse("99960395-2cc3-46e8-bcef-c844f1048999")
                },
                  new SessionInfo
                {
                    ApplicationUserId = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0"),
                    BegTime = new DateTime(2019,10,04, 18, 19,00),
                    EndTime = new DateTime(2019,10,04,18,25,30),
                    CompanyId = Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182"),
                    FullName = "tuisv@heedbook.com",
                    IndustryId = Guid.Parse("99960395-2cc3-46e8-bcef-c844f1048999")
                }
            };
            moqIAnalyticOfficeProvider.Setup(p => p.GetSessionsInfo(
                It.IsAny<DateTime>(), 
                It.IsAny<DateTime>(), 
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>()))
                .Returns(sessionsInfo);
            var dialogues = new List<DialogueInfo>()
            {
                new DialogueInfo(){BegTime = new DateTime(2019, 10, 29, 18, 30, 00), EndTime = new DateTime(2019, 10, 29, 19, 00, 00)},
                new DialogueInfo(){BegTime = new DateTime(2019, 10, 30, 18, 30, 00), EndTime = new DateTime(2019, 10, 30, 19, 00, 00)},
                new DialogueInfo(){BegTime = new DateTime(2019, 10, 30, 19, 10, 00), EndTime = new DateTime(2019, 10, 30, 19, 40, 00)},
                new DialogueInfo(){BegTime = new DateTime(2019, 10, 30, 19, 50, 00), EndTime = new DateTime(2019, 10, 30, 20, 20, 00)},
                new DialogueInfo(){BegTime = new DateTime(2019, 10, 30, 19, 10, 00), EndTime = new DateTime(2019, 10, 30, 19, 40, 00)},
                new DialogueInfo(){BegTime = new DateTime(2019, 10, 30, 20, 30, 00), EndTime = new DateTime(2019, 10, 30, 21, 00, 00)},
                new DialogueInfo(){BegTime = new DateTime(2019, 10, 30, 21, 10, 00), EndTime = new DateTime(2019, 10, 30, 21, 40, 00)},
                new DialogueInfo(){BegTime = new DateTime(2019, 10, 31, 18, 30, 00), EndTime = new DateTime(2019, 10, 31, 19, 00, 00)},
                new DialogueInfo(){BegTime = new DateTime(2019, 11, 01, 18, 30, 00), EndTime = new DateTime(2019, 11, 01, 19, 00, 00)}
            };
            moqIAnalyticOfficeProvider.Setup(p => p.GetDialoguesInfo(
                It.IsAny<DateTime>(), 
                It.IsAny<DateTime>(), 
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>()))
                .Returns(dialogues);
            return moqIAnalyticOfficeProvider;
        }
        public Mock<IDBOperations> MockIDBOperations(Mock<IDBOperations> moqIDBOperationsProvider)
        {
            moqIDBOperationsProvider.Setup(p => p.LoadIndex(
                    It.IsAny<List<SessionInfo>>(),
                    It.IsAny<List<DialogueInfo>>(), 
                    It.IsAny<DateTime>(), 
                    It.IsAny<DateTime>()))
                .Returns(0.5d);
            moqIDBOperationsProvider.Setup(p => p.DialoguesCount(
                    It.IsAny<List<DialogueInfo>>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>()))
                .Returns(3);
            moqIDBOperationsProvider.Setup(p => p.SessionAverageHours(
                    It.IsAny<List<SessionInfo>>(),                    
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>()))
                .Returns(5d);
            moqIDBOperationsProvider.Setup(p => p.DialogueAverageDuration(
                    It.IsAny<List<DialogueInfo>>(),                    
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>()))
                .Returns(5d);
            moqIDBOperationsProvider.Setup(p => p.BestEmployeeLoad(
                    It.IsAny<List<DialogueInfo>>(),
                    It.IsAny<List<SessionInfo>>(), 
                    It.IsAny<DateTime>(), 
                    It.IsAny<DateTime>()))
                .Returns(new Employee());
            moqIDBOperationsProvider.Setup(p => p.SatisfactionIndex(
                    It.IsAny<List<DialogueInfo>>()))
                .Returns(60d);
            moqIDBOperationsProvider.Setup(p => p.EmployeeCount(
                    It.IsAny<List<DialogueInfo>>()))
                .Returns(3);
            moqIDBOperationsProvider.Setup(p => p.DialogueAveragePause(
                    It.IsAny<List<SessionInfo>>(),
                    It.IsAny<List<DialogueInfo>>(), 
                    It.IsAny<DateTime>(), 
                    It.IsAny<DateTime>()))
                .Returns(20d);
            moqIDBOperationsProvider.Setup(p => p.DialogueAvgPauseListInMinutes(
                    It.IsAny<List<SessionInfo>>(),
                    It.IsAny<List<DialogueInfo>>(), 
                    It.IsAny<DateTime>(), 
                    It.IsAny<DateTime>()))
                .Returns(new List<double>(){});
            moqIDBOperationsProvider.Setup(p => p.SessionTotalHours(
                    It.IsAny<List<SessionInfo>>(),
                    It.IsAny<DateTime>(), 
                    It.IsAny<DateTime>()))
                .Returns(9d);
            
            return moqIDBOperationsProvider;
        }
        public void Dispose()
        {
        }
    }
}