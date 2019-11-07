using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using UserOperations.Services;
using UserOperations.Controllers;
using UserOperations.AccountModels;
using HBData;
using System;
using System.Threading.Tasks;
using UserOperations.Providers;
using HBData.Models;
using HBData.Models.AccountViewModels;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Utils;
using UserOperations.Providers.Interfaces;
using UserOperations.Models.AnalyticModels;
using System.IO;

namespace ApiTests
{
    public class AccountControllerTests : ApiServiceTest
    {        
        protected Mock<IAccountProvider> accountProviderMock;
        protected MockInterfaceProviders mockProvider;
        protected Mock<IHelpProvider> helpProvider;

        [SetUp]
        public void Setup()
        {
            mockProvider = new MockInterfaceProviders();
            accountProviderMock = new Mock<IAccountProvider>();
            helpProvider = new Mock<IHelpProvider>();
            base.Setup();
        }
       
        [Test]
        public void RegisterPostTest()
        {
            //Arrange
            base.loginMock = mockProvider.MockILoginService(base.loginMock);            
            
            base.mailSenderMock = mockProvider.MockIMailSender(base.mailSenderMock);

            accountProviderMock = mockProvider.MockIAccountProvider(accountProviderMock);

            helpProvider = mockProvider.MockIHelpProvider(helpProvider);

            var accountController = new AccountController(loginMock.Object, mailSenderMock.Object, accountProviderMock.Object, helpProvider.Object);

            //Act
            var task = accountController.UserRegister(new UserRegister());            
            task.Wait();
            var okResult = task.Result as OkObjectResult;
            var result = okResult.Value.ToString();    

            //Assert
            Assert.IsTrue(result == "Registred");
        }
       
        [Test]
        public void GenerateTokenPostTest()
        {
            //Arrange
            loginMock = mockProvider.MockILoginService(loginMock);            
            
            mailSenderMock = mockProvider.MockIMailSender(mailSenderMock);

            accountProviderMock = mockProvider.MockIAccountProvider(accountProviderMock);

            helpProvider = mockProvider.MockIHelpProvider(helpProvider);

            var accountController = new AccountController(loginMock.Object, mailSenderMock.Object, accountProviderMock.Object, helpProvider.Object);

            //Act
            var okResult = accountController.GenerateToken(new AccountAuthorization()) as OkObjectResult;
            var result = okResult.Value.ToString();
            
            //Assert         
            Assert.IsTrue(result == "Token");
        }
        
        [Test]
        public void ChangePasswordPostTest()
        {
            //Arrange
            loginMock = mockProvider.MockILoginService(loginMock);            
            
            mailSenderMock = mockProvider.MockIMailSender(mailSenderMock);

            accountProviderMock = mockProvider.MockIAccountProvider(accountProviderMock);

            var accountController = new AccountController(loginMock.Object, mailSenderMock.Object, accountProviderMock.Object, helpProvider.Object);

            helpProvider = mockProvider.MockIHelpProvider(helpProvider);

            //Act
            var task = accountController.UserChangePasswordAsync(new AccountAuthorization(){Password = "password"}, $"Bearer Token");
            task.Wait();
            var OkResult = task.Result as OkObjectResult;
            System.Console.WriteLine($"result: {OkResult is null}");
            var result = OkResult.Value.ToString();
            

            //Assert
            Assert.IsTrue(result == "password changed");
        }
        
        [Test]
        public void UserChangePasswordOnDefaultAsyncPostTest()
        {
            //Arrange
            loginMock = mockProvider.MockILoginService(loginMock);            
            
            mailSenderMock = mockProvider.MockIMailSender(mailSenderMock);

            accountProviderMock = mockProvider.MockIAccountProvider(accountProviderMock);

            helpProvider = mockProvider.MockIHelpProvider(helpProvider);

            var accountController = new AccountController(loginMock.Object, mailSenderMock.Object, accountProviderMock.Object, helpProvider.Object);

            //Act
            var task = accountController.UserChangePasswordOnDefaultAsync($"test@heedbook.com");
            task.Wait();
            var OkResult = task.Result as OkObjectResult;
            var result = OkResult.Value.ToString();

            //Assert
            Assert.IsTrue(result == "password changed");
        }

        [Test]
        public void UnblockPostTest()
        {
            //Arrange
            loginMock = mockProvider.MockILoginService(loginMock);            
            
            mailSenderMock = mockProvider.MockIMailSender(mailSenderMock);

            accountProviderMock = mockProvider.MockIAccountProvider(accountProviderMock);

            helpProvider = mockProvider.MockIHelpProvider(helpProvider);

            var accountController = new AccountController(loginMock.Object, mailSenderMock.Object, accountProviderMock.Object, helpProvider.Object);

            //Act
            var task = accountController.Unblock($"test@heedbook.com", $"Bearer Token");
            task.Wait();
            var OkResult = task.Result as OkObjectResult;
            var result = OkResult.Value.ToString();

            //Assert
            Assert.IsTrue(result == "password changed");
        }
        
        [Test]
        public void RemoveDeleteTest()
        {
            //Arrange
            loginMock = mockProvider.MockILoginService(loginMock);            
            
            mailSenderMock = mockProvider.MockIMailSender(mailSenderMock);

            accountProviderMock = mockProvider.MockIAccountProvider(accountProviderMock);

            helpProvider = mockProvider.MockIHelpProvider(helpProvider);

            var accountController = new AccountController(loginMock.Object, mailSenderMock.Object, accountProviderMock.Object, helpProvider.Object);

            //Act
            var task = accountController.AccountDelete($"test@heedbook.com");
            task.Wait();
            var OkResult = task.Result as OkObjectResult;
            var result = OkResult.Value.ToString();

            //Assert
            Assert.IsTrue(result == "Removed");
        }
    }
    public class MockInterfaceProviders : ApiServiceTest
    {
        public Mock<ILoginService> MockILoginService(Mock<ILoginService> moqILoginService)
        {
            moqILoginService.Setup(p => p.CheckUserLogin(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
            moqILoginService.Setup(p => p.SaveErrorLoginHistory(It.IsAny<Guid>(), It.IsAny<string>()))
                .Returns(true);
            var dict = new Dictionary<string, string>
            {
                {"role", "role"},
                {"companyId", "b1ec1819-5782-4215-86d0-b3ccbeaddaef"},
                {"applicationUserId", "2d10136f-8341-4758-b218-785409a11e98"}
            };
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
        private int GetStatus(string status)
        {
            var value = status == "Active" ? 3 : (status == "Inactive" ? 5 : 0);
            return value;
        }   
        public Mock<IAnalyticContentProvider> MockIAnalyticContentProvider(Mock<IAnalyticContentProvider> moqIAnalyticContentProvider)
        {
            moqIAnalyticContentProvider.Setup(p => p.GetSlideShowsForOneDialogueAsync(It.IsAny<Dialogue>()))
                .Returns(Task.FromResult(new List<SlideShowInfo>()
                {
                    new SlideShowInfo()
                    {
                        ContentName = "Content1",
                        IsPoll = false,
                        ContentType = "content",
                        ContentId = Guid.Parse("7ad7ceaf-cca5-4b6a-9d0e-59b7407ac2e1"),
                        Url = "https://test1.com"
                    }, 
                    new SlideShowInfo()
                    {
                        ContentName = "Content2",
                        IsPoll = false,
                        ContentType = "media",
                        ContentId = Guid.Parse("7ad7ceaf-cca5-4b6a-9d0e-59b7407ac2e2"),
                        Url = "https://test2.com"
                    },
                    new SlideShowInfo()
                    {
                        ContentName = "Content3",
                        IsPoll = true,
                        ContentType = "url",
                        ContentId = Guid.Parse("7ad7ceaf-cca5-4b6a-9d0e-59b7407ac2e3"),
                        Url = "https://test3.com"
                    }
                }));
            moqIAnalyticContentProvider.Setup(p => p.GetSlideShowFilteredByPoolAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<bool>()))
                .Returns(Task.FromResult(new List<SlideShowInfo>()
                {
                    new SlideShowInfo(){BegTime = new DateTime(), EndTime = new DateTime(), DialogueId = Guid.NewGuid(), Age = 20, Gender = "male"}, 
                    new SlideShowInfo(){BegTime = new DateTime(), EndTime = new DateTime(), DialogueId = Guid.NewGuid(), Age = 22, Gender = "female"}, 
                    new SlideShowInfo(){BegTime = new DateTime(), EndTime = new DateTime(), DialogueId = Guid.NewGuid(), Age = 27, Gender = "male"}
                }));
            moqIAnalyticContentProvider.Setup(p => p.GetAnswersInOneDialogueAsync(
                    It.IsAny<List<SlideShowInfo>>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Guid>()))
                .Returns(Task.FromResult(new List<CampaignContentAnswer>(){}));
            moqIAnalyticContentProvider.Setup(p => p.GetAnswersForOneContent(
                    It.IsAny<List<AnswerInfo.AnswerOne>>(),
                    It.IsAny<Guid?>()))
                .Returns(new List<AnswerInfo.AnswerOne>(){});
            moqIAnalyticContentProvider.Setup(p => p.GetConversion(
                    It.IsAny<double>(),
                    It.IsAny<double>()))
                .Returns(0.5d);
            moqIAnalyticContentProvider.Setup(p => p.GetAnswersFullAsync(
                    It.IsAny<List<SlideShowInfo>>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<List<Guid>>()))
                .Returns(Task.FromResult(new List<AnswerInfo.AnswerOne>(){}));
            moqIAnalyticContentProvider.Setup(p => p.AddDialogueIdToShow(It.IsAny<List<SlideShowInfo>>(), It.IsAny<List<DialogueInfoWithFrames>>()))
                .Returns(new List<SlideShowInfo>(){});
            moqIAnalyticContentProvider.Setup(p => p.EmotionsDuringAdv(It.IsAny<List<SlideShowInfo>>(), It.IsAny<List<DialogueInfoWithFrames>>()))
                .Returns(new EmotionAttention(){Positive = 0.3d, Negative = 0.3d, Neutral = 0.3d, Attention = 0.3d});
            moqIAnalyticContentProvider.Setup(p => p.EmotionDuringAdvOneDialogue(It.IsAny<List<SlideShowInfo>>(), It.IsAny<List<DialogueFrame>>()))
                .Returns(new EmotionAttention(){Positive = 0.3d, Negative = 0.3d, Neutral = 0.3d, Attention = 0.3d});
            return moqIAnalyticContentProvider;
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

    }
}