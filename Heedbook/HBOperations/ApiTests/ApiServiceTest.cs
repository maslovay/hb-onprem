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
            TestData.email = $"test1@heedbook.com";

            accountProviderMock = new Mock<IAccountProvider>();
            commonProviderMock = new Mock<IAnalyticCommonProvider>();
            configMock = new Mock<IConfiguration>();
            dbOperationMock = new Mock<IDBOperations>();
            filterMock = new Mock<IRequestFilters>(MockBehavior.Loose);
            helpProvider = new Mock<IHelpProvider>();
            mailSenderMock = new Mock<IMailSender>();
            moqILoginService = new Mock<ILoginService>();
            repositoryMock = new Mock<IGenericRepository>();
        }
        protected void InitData()
        {
            InitMockILoginService();
            InitMockIMailSender();
            InitMockIAccountProvider();
            InitMockIHelpProvider();
            InitMockIRequestFiltersProvider();
            InitMockIDBOperations();
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

        public void InitMockILoginService()
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
        }
        public void InitMockIMailSender()
        {
            mailSenderMock.Setup(p => p.SendRegisterEmail(new HBData.Models.ApplicationUser()))
                .Returns(Task.FromResult(0));
            mailSenderMock.Setup(p => p.SendPasswordChangeEmail(new HBData.Models.ApplicationUser(), "password"))
                .Returns(Task.FromResult(0));
        }
        public void InitMockIAccountProvider()
        {
            accountProviderMock.Setup(p => p.GetStatusId(It.IsAny<string>()))
                .Returns((string p) => p == "Active" ? 3 : (p == "Inactive" ? 5 : 0));
            accountProviderMock.Setup(p => p.CompanyExist(It.IsAny<string>()))
                .Returns(Task.FromResult(false));
            accountProviderMock.Setup(p => p.EmailExist(It.IsAny<string>()))
                .Returns(Task.FromResult(false));
            accountProviderMock.Setup(p => p.AddNewCompanysInBase(It.IsAny<UserRegister>()))
                .Returns(TestData.Company1());
            accountProviderMock.Setup(p => p.AddNewUserInBase(It.IsAny<UserRegister>(), It.IsAny<Guid?>()))
                .Returns(Task.FromResult(TestData.User1()));
            accountProviderMock.Setup(p => p.AddUserRoleInBase(TestData.GetUserRegister(), new ApplicationUser()))
                .Returns(Task.FromResult(0));
            accountProviderMock.Setup(p => p.GetTariffsAsync(TestData.Company1().CompanyId))
                .Returns(Task.FromResult(0));
            accountProviderMock.Setup(p => p.CreateCompanyTariffAndTransaction(TestData.Company1()))
                .Returns(Task.FromResult(0));
            accountProviderMock.Setup(p => p.AddWorkerType(TestData.Company1()))
                .Returns(Task.FromResult(0));
            accountProviderMock.Setup(p => p.AddContentAndCampaign(TestData.Company1()))
                .Returns(Task.FromResult(0));
            accountProviderMock.Setup(p => p.GetUserIncludeCompany(It.IsAny<string>()))
                .Returns(TestData.User1());
            accountProviderMock.Setup(p => p.GetUserIncludeCompany(It.IsAny<Guid>(), It.IsAny<AccountAuthorization>()))
                .Returns(TestData.User1());
            accountProviderMock.Setup(p => p.RemoveAccount("email"))
                .Callback(() => {});
        }
        public void InitMockIHelpProvider()
        {
            helpProvider.Setup(p => p.AddComanyPhrases());
            helpProvider.Setup(p => p.CreatePoolAnswersSheet(It.IsAny<List<AnswerInfo>>(), It.IsAny<string>()))
                .Returns(new MemoryStream());
        }
        public void InitMockIRequestFiltersProvider()
        {
            var list = new List<Guid>(){};
            filterMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter( ref list, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));
            filterMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
                .Returns(new DateTime(2019, 10, 30));
            filterMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
                .Returns(new DateTime(2019, 11, 01));
        }
        public void InitMockIDBOperations()
        {
            dbOperationMock.Setup(p => p.LoadIndex(
                    It.IsAny<List<SessionInfo>>(),
                    It.IsAny<List<DialogueInfo>>(), 
                    It.IsAny<DateTime>(), 
                    It.IsAny<DateTime>()))
                .Returns(0.5d);
            dbOperationMock.Setup(p => p.DialoguesCount(
                    It.IsAny<List<DialogueInfo>>(),
                    It.IsAny<Guid>(),
                    It.IsAny<DateTime>()))
                .Returns(3);
            dbOperationMock.Setup(p => p.SessionAverageHours(
                    It.IsAny<List<SessionInfo>>(),                    
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>()))
                .Returns(5d);
            dbOperationMock.Setup(p => p.DialogueAverageDuration(
                    It.IsAny<List<DialogueInfo>>(),                    
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>()))
                .Returns(5d);
            dbOperationMock.Setup(p => p.BestEmployeeLoad(
                    It.IsAny<List<DialogueInfo>>(),
                    It.IsAny<List<SessionInfo>>(), 
                    It.IsAny<DateTime>(), 
                    It.IsAny<DateTime>()))
                .Returns(new Employee());
            dbOperationMock.Setup(p => p.SatisfactionIndex(
                    It.IsAny<List<DialogueInfo>>()))
                .Returns(60d);
            dbOperationMock.Setup(p => p.EmployeeCount(
                    It.IsAny<List<DialogueInfo>>()))
                .Returns(3);
            dbOperationMock.Setup(p => p.DialogueAveragePause(
                    It.IsAny<List<SessionInfo>>(),
                    It.IsAny<List<DialogueInfo>>(), 
                    It.IsAny<DateTime>(), 
                    It.IsAny<DateTime>()))
                .Returns(20d);
            dbOperationMock.Setup(p => p.DialogueAvgPauseListInMinutes(
                    It.IsAny<List<SessionInfo>>(),
                    It.IsAny<List<DialogueInfo>>(), 
                    It.IsAny<DateTime>(), 
                    It.IsAny<DateTime>()))
                .Returns(new List<double>(){});
            dbOperationMock.Setup(p => p.SessionTotalHours(
                    It.IsAny<List<SessionInfo>>(),
                    It.IsAny<DateTime>(), 
                    It.IsAny<DateTime>()))
                .Returns(9d);
        }
        public void Dispose()
        {
        }
    }
}