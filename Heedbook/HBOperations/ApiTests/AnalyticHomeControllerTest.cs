using System;
using System.Threading.Tasks;
using System.Linq;

using HBData.Models;
using NUnit.Framework;
using Moq;
using UserOperations.Utils;
using UserOperations.Controllers;
using Microsoft.Extensions.Configuration;
using UserOperations.Services;
using UserOperations.Providers;
using System.Collections.Generic;
using UserOperations.Models.AnalyticModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ApiTests
{
    [TestFixture]
    public class AnalyticHomeControllerTest : ApiServiceTest
    {

        private Mock<IAnalyticHomeProvider> homeProviderMock;
        [SetUp]
        public new void Setup()
        {
              base.Setup();
        }
        protected override void InitServices()
        {
            homeProviderMock = new Mock<IAnalyticHomeProvider>();
        }

        [Test]
        public async Task GetDashboard()
        {
            //arrange           
            base.moqILoginService.Setup(log => log.GetDataFromToken(It.IsAny<string>(), out TestData.tokenclaims, null)).Returns(true);
            base.filterMock.Setup(f => f.GetBegDate(It.IsAny<string>())).Returns(TestData.begDate);
            base.filterMock.Setup(f => f.GetEndDate(It.IsAny<string>())).Returns(TestData.endDate);
            base.filterMock.Setup(f => f.CheckRolesAndChangeCompaniesInFilter(ref TestData.companyIds, null, It.IsAny<string>(), It.IsAny<Guid>()));
            base.commonProviderMock.Setup(c => c.GetSessionInfoAsync(TestData.prevDate, TestData.endDate, TestData.companyIds, null, null)).Returns(TestData.GetSessions());
            base.commonProviderMock.Setup(c => c.GetCrossPhraseTypeIdAsync()).Returns(TestData.GetCrossPhraseId());
            base.commonProviderMock.Setup(c => c.GetDialoguesIncludedPhrase(TestData.prevDate, TestData.endDate, TestData.companyIds, null, null)).Returns(TestData.GetDialoguesWithUserPhrasesSatisfaction());
            homeProviderMock.Setup(h => h.GetBenchmarksList(TestData.begDate, TestData.endDate, TestData.companyIds)).Returns(TestData.GetBenchmarkList());

            var controller = new AnalyticHomeController(commonProviderMock.Object, homeProviderMock.Object, configMock.Object, moqILoginService.Object, dbOperationMock.Object, filterMock.Object);

            // Act

            var result = await controller.GetDashboard(TestData.beg, TestData.end, null, null, null, TestData.token);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.That(okResult.Value != null);
            var deserialized = JsonConvert.DeserializeObject<DashboardInfo>(okResult.Value.ToString());
            Assert.That(deserialized != null);
        }
        [Test]
        public async Task GetDashboardFiltered()
        {
            //Arrange
            base.moqILoginService.Setup(log => log.GetDataFromToken(It.IsAny<string>(), out TestData.tokenclaims, null)).Returns(true);
            base.filterMock.Setup(f => f.GetBegDate(It.IsAny<string>())).Returns(TestData.begDate);
            base.filterMock.Setup(f => f.GetEndDate(It.IsAny<string>())).Returns(TestData.endDate);
            base.filterMock.Setup(f => f.CheckRolesAndChangeCompaniesInFilter(ref TestData.companyIds, null, It.IsAny<string>(), It.IsAny<Guid>()));
            base.commonProviderMock.Setup(c => c.GetSessionInfoAsync(TestData.prevDate, TestData.endDate, TestData.companyIds, null, null)).Returns(TestData.GetSessions());
            base.commonProviderMock.Setup(c => c.GetCrossPhraseTypeIdAsync()).Returns(TestData.GetCrossPhraseId());
            base.commonProviderMock.Setup(c => c.GetDialoguesIncludedPhrase(TestData.prevDate, TestData.endDate, TestData.companyIds, null, null)).Returns(TestData.GetDialoguesWithUserPhrasesSatisfaction());
            homeProviderMock.Setup(h => h.GetBenchmarksList(TestData.begDate, TestData.endDate, TestData.companyIds)).Returns(TestData.GetBenchmarkList());
            homeProviderMock.Setup(h => h.GetBenchmarkIndustryAvg(It.IsAny<List<BenchmarkModel>>(), It.IsAny<string>())).Returns(0.6d);
            homeProviderMock.Setup(h => h.GetBenchmarkIndustryMax(It.IsAny<List<BenchmarkModel>>(), It.IsAny<string>())).Returns(0.6d);

            var controller = new AnalyticHomeController(commonProviderMock.Object, homeProviderMock.Object, configMock.Object, moqILoginService.Object, dbOperationMock.Object, filterMock.Object);

            //Act
            var task = controller.GetDashboardFiltered(
                TestData.beg,
                TestData.end,
                new List<Guid>(){Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()},
                new List<Guid>(){Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()},
                new List<Guid>(){Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()},
                new List<Guid>(){Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()},
                TestData.token);
            task.Wait();

            var okResult = task.Result as OkObjectResult;

            //Assert
            Assert.NotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
        }        
    }
}
