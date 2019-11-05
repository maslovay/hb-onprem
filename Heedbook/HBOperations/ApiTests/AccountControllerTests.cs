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
    }
}