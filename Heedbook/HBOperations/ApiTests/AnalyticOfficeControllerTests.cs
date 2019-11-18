using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using UserOperations.Controllers;
using System;
using System.Threading.Tasks;
using UserOperations.Providers;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;

namespace ApiTests
{
    public class AnalyticOfficeControllerTests : ApiServiceTest
    {   
        private Mock<IAnalyticOfficeProvider> analyticOfficeProviderMock;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
        }
        protected override void InitServices()
        {
            analyticOfficeProviderMock = MockIAnalyticOfficeProvider(new Mock<IAnalyticOfficeProvider>());
            base.moqILoginService = MockILoginService(base.moqILoginService);
        }
        [Test]
        public async Task UserRegister()
        {
            //Arrange
            var controller = new AnalyticOfficeController(
                configMock.Object,
                moqILoginService.Object, 
                dbOperationMock.Object, 
                filterMock.Object, 
                analyticOfficeProviderMock.Object);
            var sessions = await TestData.GetSessions();
            
            //Act
            var result = controller.Efficiency(
                TestData.beg, TestData.end,
                TestData.GetGuids(), TestData.GetGuids(), TestData.GetGuids(), TestData.GetGuids(),
                TestData.token
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