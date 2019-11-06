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
using System.Linq;

namespace ApiTests
{
    public class AnalyticClientProfileControllerTests : ApiServiceTest
    {   
        protected Mock<IAccountProvider> accountProviderMock;
        protected MockInterfaceProviders mockProvider;
        [SetUp]
        public void Setup()
        {
            mockProvider = new MockInterfaceProviders();
            accountProviderMock = new Mock<IAccountProvider>();
            base.Setup();
        }
        [Test]
        public void EfficiencyDashboard()
        {
            //Arrange
            var dialogues = new List<Dialogue>()
                {
                    new Dialogue
                    {
                        DialogueId = Guid.NewGuid(),
                        PersonId = Guid.NewGuid(),
                        DialogueClientProfile = new List<DialogueClientProfile>(){new DialogueClientProfile{Age = 20, Gender = "male"}}
                    },
                    new Dialogue
                    {
                        DialogueId = Guid.NewGuid(),
                        PersonId = Guid.NewGuid(),
                        DialogueClientProfile = new List<DialogueClientProfile>(){new DialogueClientProfile{Age = 25, Gender = "female"}}
                    },
                }
                .AsQueryable();          
            var list = new List<Guid>(){};

            filterMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter( ref list, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));
            filterMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
                .Returns(new DateTime(2019, 10, 30));
            filterMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
                .Returns(new DateTime(2019, 11, 01));

            commonProviderMock.Setup(p => p.GetPersondIdsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<List<Guid>>()))
                .Returns(Task.FromResult(new List<Guid?>(){}));
            commonProviderMock.Setup(p => p.GetDialoguesIncludedClientProfile(
                    It.IsAny<DateTime>(), 
                    It.IsAny<DateTime>(), 
                    It.IsAny<List<Guid>>(), 
                    It.IsAny<List<Guid>>(), 
                    It.IsAny<List<Guid>>()))
                .Returns(dialogues);            
            loginMock = mockProvider.MockILoginService(loginMock);      
            
            var analyticClientProfileController = new AnalyticClientProfileController(
                commonProviderMock.Object, 
                loginMock.Object, 
                dbOperationMock.Object, 
                filterMock.Object);            

            //Act
            var task = analyticClientProfileController.EfficiencyDashboard(
                "20191030",
                "20191101",
                new List<Guid>(){},
                new List<Guid>(){},
                new List<Guid>(){},
                new List<Guid>(){},
                $"Bearer Token");

            task.Wait();
            var okResult = task.Result as OkObjectResult;
            var result = okResult.Value.ToString();
            var dictionary = JsonConvert.DeserializeObject<Dictionary<object, object>>(result);

            //Assert
            Assert.IsNotNull(dictionary);
            Assert.NotZero(dictionary.Count);
        }
    }    
}     