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
        protected Mock<IHelpProvider> helpProvider;

        [SetUp]
        public void Setup()
        {
            accountProviderMock = new Mock<IAccountProvider>();
            helpProvider = new Mock<IHelpProvider>();
            base.Setup();
        }
       
        [Test]
        public void RegisterPostTest()
        {
            //Arrange
            base.loginMock = MockILoginService(base.loginMock);            
            
            base.mailSenderMock = MockIMailSender(base.mailSenderMock);

            accountProviderMock = MockIAccountProvider(accountProviderMock);

            helpProvider = MockIHelpProvider(helpProvider);

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
            loginMock = MockILoginService(loginMock);            
            
            mailSenderMock = MockIMailSender(mailSenderMock);

            accountProviderMock = MockIAccountProvider(accountProviderMock);

            helpProvider = MockIHelpProvider(helpProvider);

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
            loginMock = MockILoginService(loginMock);            
            
            mailSenderMock = MockIMailSender(mailSenderMock);

            accountProviderMock = MockIAccountProvider(accountProviderMock);

            var accountController = new AccountController(loginMock.Object, mailSenderMock.Object, accountProviderMock.Object, helpProvider.Object);

            helpProvider = MockIHelpProvider(helpProvider);

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
            loginMock = MockILoginService(loginMock);            
            
            mailSenderMock = MockIMailSender(mailSenderMock);

            accountProviderMock = MockIAccountProvider(accountProviderMock);

            helpProvider = MockIHelpProvider(helpProvider);

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
            loginMock = MockILoginService(loginMock);            
            
            mailSenderMock = MockIMailSender(mailSenderMock);

            accountProviderMock = MockIAccountProvider(accountProviderMock);

            helpProvider = MockIHelpProvider(helpProvider);

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
            loginMock = MockILoginService(loginMock);            
            
            mailSenderMock = MockIMailSender(mailSenderMock);

            accountProviderMock = MockIAccountProvider(accountProviderMock);

            helpProvider = MockIHelpProvider(helpProvider);

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
}