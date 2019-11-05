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

            filterMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter( ref list, new List<Guid>(){}, "role", Guid.NewGuid()));
            commonProviderMock.Setup(p => p.GetPersondIdsAsync(new DateTime(), new DateTime(), new List<Guid>(){})).Returns(Task.FromResult(new List<Guid?>(){}));
                            
            commonProviderMock.Setup(p => p.GetDialoguesIncludedClientProfile(
                    It.IsAny<DateTime>(), 
                    It.IsAny<DateTime>(), 
                    It.IsAny<List<Guid>>(), 
                    It.IsAny<List<Guid>>(), 
                    It.IsAny<List<Guid>>()))
                .Returns(dialogues);
            
            var analyticClientProfileController = new AnalyticClientProfileController(
                commonProviderMock.Object, 
                loginMock.Object, 
                dbOperationMock.Object, 
                filterMock.Object);

            //Act
            var task = analyticClientProfileController.EfficiencyDashboard(
                "begTime", 
                "endTime", 
                new List<Guid>(){}, 
                new List<Guid>(){}, 
                new List<Guid>(){},
                new List<Guid>(){},
                $"Bearer Token");

            task.Wait();
            var okResult = task.Result as OkObjectResult;
            System.Console.WriteLine(okResult is null);
            var result = okResult.Value.ToString();
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(result);
            
            //Assert
            Assert.IsNotNull(dictionary);
            Assert.NotZero(dictionary.Count);
        }
    }    
}     