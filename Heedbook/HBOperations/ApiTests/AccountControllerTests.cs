using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using UserOperations.Controllers;
using UserOperations.AccountModels;
using HBData.Models.AccountViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ApiTests
{
    public class AccountControllerTests : ApiServiceTest
    {                
        [SetUp]
        public void Setup()
        {           
            base.Setup();
        }
        protected override void InitServices()
        {
            base.moqILoginService = MockILoginService(base.moqILoginService);
            base.mailSenderMock = MockIMailSender(base.mailSenderMock);
            base.accountProviderMock = MockIAccountProvider(base.accountProviderMock);
            base.helpProvider = MockIHelpProvider(base.helpProvider);
        }

        [Test]
        public void RegisterPostTest()
        {
            //Arrange
            var accountController = new AccountController(moqILoginService.Object, mailSenderMock.Object, accountProviderMock.Object, helpProvider.Object);

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
            var accountController = new AccountController(moqILoginService.Object, mailSenderMock.Object, accountProviderMock.Object, helpProvider.Object);

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
            var accountController = new AccountController(moqILoginService.Object, mailSenderMock.Object, accountProviderMock.Object, helpProvider.Object);           

            //Act
            var task = accountController.UserChangePasswordAsync(new AccountAuthorization(){Password = "password"}, TestData.token);
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
            var accountController = new AccountController(moqILoginService.Object, mailSenderMock.Object, accountProviderMock.Object, helpProvider.Object);

            //Act
            var task = accountController.UserChangePasswordOnDefaultAsync(TestData.email);
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
            var accountController = new AccountController(moqILoginService.Object, mailSenderMock.Object, accountProviderMock.Object, helpProvider.Object);

            //Act
            var task = accountController.Unblock(TestData.email, TestData.token);
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
            var accountController = new AccountController(moqILoginService.Object, mailSenderMock.Object, accountProviderMock.Object, helpProvider.Object);

            //Act
            var task = accountController.AccountDelete(TestData.email);
            task.Wait();
            var OkResult = task.Result as OkObjectResult;
            var result = OkResult.Value.ToString();

            //Assert
            Assert.IsTrue(result == "Removed");
        }
    }
}