using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using UserOperations.Controllers;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Services;

namespace ApiTests
{
    public class AnalyticClientProfileServiceTests : ApiServiceTest
    {   
        [SetUp]
        public new void Setup()
        {
            base.Setup();
        }      

        [Test]
        public void EfficiencyDashboard()
        {
            //Arrange
            var dialogues = TestData.GetDialoguesSimple();
            var list = new List<Guid>(){};

            filterMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter( ref list, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));
            filterMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
                .Returns(TestData.begDate);
            filterMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
                .Returns(TestData.endDate);

            commonProviderMock.Setup(p => p.GetPersondIdsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<List<Guid>>()))
                .Returns(Task.FromResult(new List<Guid?>(){}));
            commonProviderMock.Setup(p => p.GetDialoguesIncludedClientProfile(
                    It.IsAny<DateTime>(), 
                    It.IsAny<DateTime>(), 
                    It.IsAny<List<Guid>>(), 
                    It.IsAny<List<Guid>>(), 
                    It.IsAny<List<Guid>>()))
                .Returns(dialogues);
            
            var analyticClientProfileController = new AnalyticClientProfileService(
                commonProviderMock.Object,
                moqILoginService.Object, 
                dbOperationMock.Object, 
                filterMock.Object);            

            //Act
            var task = analyticClientProfileController.EfficiencyDashboard(
                TestData.beg, TestData.end,
                TestData.GetGuids(), TestData.GetGuids(), TestData.GetGuids(), TestData.GetGuids(),
                TestData.token);

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