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

namespace ApiTests
{
    public class AccountControllerTests
    {
        [SetUp]
        public void Setup()
        {
            
        }
       
        [Test]
        public void RegisterPostTest()
        {
            //Arrange
            MockInterfaces mockProvider = new MockInterfaces();

            var moqILoginService = new Mock<ILoginService>();
            moqILoginService = mockProvider.MockILoginService(moqILoginService);            
            
            var moqIMailSender = new Mock<IMailSender>();
            moqIMailSender = mockProvider.MockIMailSender(moqIMailSender);

            var moqIAccountProvider = new Mock<IAccountProvider>();
            moqIAccountProvider = mockProvider.MockIAccountProvider(moqIAccountProvider);

            var accountController = new AccountController(moqILoginService.Object, moqIMailSender.Object, moqIAccountProvider.Object);

            //Act
            var actionResult = accountController.UserRegister(new UserRegister());
            System.Console.WriteLine(JsonConvert.SerializeObject(actionResult));
            var task = actionResult;
            task.Wait();
            var okResult = task.Result as OkObjectResult;
            var result = okResult.Value.ToString();            
            System.Console.WriteLine(result);

            //Assert
            Assert.IsTrue(result == "Registred");
        }
        public void GenerateTokenPostTest()
        {
            //Arrange

            //Act

            //Assert
            Assert.Pass();
        }
        public void ChangePasswordPostTest()
        {
            //Arrange

            //Act

            //Assert
            Assert.Pass();
        }
        public void UserChangePasswordOnDefaultAsyncPostTest()
        {
            //Arrange

            //Act

            //Assert
            Assert.Pass();
        }
        public void UnblockPostTest()
        {
            //Arrange

            //Act

            //Assert
            Assert.Pass();
        }
        public void RemoveDeleteTest()
        {
            //Arrange

            //Act

            //Assert
            Assert.Pass();
        }
    }
    public class MockInterfaces
    {
        public Mock<ILoginService> MockILoginService(Mock<ILoginService> moqILoginService)
        {
            moqILoginService.Setup(p => p.CheckUserLogin("test", "test"))
                .Returns(true);
            moqILoginService.Setup(p => p.SaveErrorLoginHistory(Guid.NewGuid(), "test"))
                .Returns(true);
            var dict = new Dictionary<string, string>{};
            moqILoginService.Setup(p => p.GetDataFromToken("Token", out dict, ""))
                .Returns(true);
            moqILoginService.Setup(p => p.GeneratePasswordHash("password"))
                .Returns("Hash");
            moqILoginService.Setup(p => p.SavePasswordHistory(Guid.NewGuid(), "passwordHash"))
                .Returns(true);
            moqILoginService.Setup(p => p.GeneratePass(6))
                .Returns("123456");
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
                .Returns(GetStatus(It.IsAny<string>()));
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
            moqIAccountProvider.Setup(p => p.GetUserIncludeCompany("email"))
                .Returns(new ApplicationUser());
            moqIAccountProvider.Setup(p => p.GetUserIncludeCompany(Guid.NewGuid(), new AccountAuthorization()))
                .Returns(new ApplicationUser());
            moqIAccountProvider.Setup(p => p.RemoveAccount("email"))
                .Callback(() => {});          
            return moqIAccountProvider;
        }
        private int GetStatus(string status)
        {
            return status == "Active" ? 3 : (status == "Inactive" ? 5 : 0);
        }
    }
}