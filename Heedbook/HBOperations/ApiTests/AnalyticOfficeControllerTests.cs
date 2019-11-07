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
using UserOperations.Providers.Interfaces;
using UserOperations.Models.AnalyticModels;

namespace ApiTests
{
    public class AnalyticOfficeControllerTests : ApiServiceTest
    {   
        protected Mock<IAnalyticOfficeProvider> analyticOfficeProviderMock;
        protected Mock<IHelpProvider> helpProviderMock;
        protected MockInterfaceProviders mockProvider;
        [SetUp]
        public void Setup()
        {
            mockProvider = new MockInterfaceProviders();
            analyticOfficeProviderMock = new Mock<IAnalyticOfficeProvider>();
            helpProviderMock = new Mock<IHelpProvider>();
            base.Setup();
        }
        [Test]
        public async Task UserRegister()
        {
            //Arrange
            loginMock = mockProvider.MockILoginService(loginMock);

            filterMock = mockProvider.MockIRequestFiltersProvider(filterMock);            

            analyticOfficeProviderMock = mockProvider.MockIAnalyticOfficeProvider(analyticOfficeProviderMock);

            dbOperationMock = mockProvider.MockIDBOperations(dbOperationMock);

            var accountController = new AnalyticOfficeController(
                configMock.Object, 
                loginMock.Object, 
                dbOperationMock.Object, 
                filterMock.Object, 
                analyticOfficeProviderMock.Object);
            var sessions = await GetSessions();
            
            //Act
            var result = accountController.Efficiency(
                "20191105",
                "20191106",
                new List<Guid>(){Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()},
                new List<Guid>(){Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()},
                new List<Guid>(){Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()},
                new List<Guid>(){Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()},
                "Bearer Token"
            );

            //Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.That(okResult.Value != null);
            var deserialized = JsonConvert.DeserializeObject<Dictionary<string, object>>(okResult.Value.ToString());
            Assert.That(deserialized != null);
        }
    }
}