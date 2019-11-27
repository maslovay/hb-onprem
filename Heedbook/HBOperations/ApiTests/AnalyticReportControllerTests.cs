using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using UserOperations.Controllers;
using UserOperations.AccountModels;
using HBData.Models.AccountViewModels;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Providers;
using System.Linq.Expressions;
using System;
using HBData.Models;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.EntityFrameworkCore.Query.Internal;
using UserOperations.Models.AnalyticModels;

namespace ApiTests
{public class AnalyticReportControllerTests : ApiServiceTest
    {   
        private Mock<IAnalyticReportProvider> analyticReportProviderMock;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
        }
        [Test]
        public async Task ReportActiveEmployeeTest()
        {
            //Arrange
            analyticReportProviderMock = new Mock<IAnalyticReportProvider>();
            var task = TestData.GetSessions();
            task.Wait();
            var sessionInfos = task.Result.ToList();
            analyticReportProviderMock.Setup(p => p.GetSessions(It.IsAny<List<Guid>>(), It.IsAny<List<Guid>>(), It.IsAny<List<Guid>>()))
                .Returns(sessionInfos);
            

            var analyticReportController = new AnalyticReportController(
                configMock.Object, 
                moqILoginService.Object,
                dbOperationMock.Object,
                filterMock.Object,
                analyticReportProviderMock.Object);
            //Act
            var result = analyticReportController.ReportActiveEmployee(
                new List<Guid>(),
                new List<Guid>(),
                new List<Guid>(),
                new List<Guid>(),
                "Bearer token"
            );
            //Assert
            var okResult = result as OkObjectResult;
            System.Console.WriteLine($"{okResult.Value.ToString()}");
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.That(okResult.Value != null);
            var deserialized = JsonConvert.DeserializeObject<List<SessionInfo>>(okResult.Value.ToString());
            Assert.That(deserialized != null);
        }
        [Test]
        public void ReportUserPartialTest()
        {
            //Arrange
            analyticReportProviderMock = new Mock<IAnalyticReportProvider>();
            var task = TestData.GetSessions();
            var sessionsInfo = task.Result.ToList();
            var dialogueInfo = TestData.GetDialogueInfo();
            var users = TestData.GetUsers().ToList();
            analyticReportProviderMock.Setup(p => p.GetEmployeeRoleId()).Returns(Guid.Parse("b54d371a-1111-48a0-9731-ab1d9a1f171a"));
            var begDate = new DateTime(2019, 11, 26);
            var endDate = new DateTime(2019, 11, 27);
        
            analyticReportProviderMock.Setup(p => p.GetSessions(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<Guid>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>()
            ))
            .Returns(sessionsInfo);
            analyticReportProviderMock.Setup(p => p.GetDialogues(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>()
            ))
            .Returns(dialogueInfo);
            analyticReportProviderMock.Setup(p => p.GetApplicationUsersToAdd(
                It.IsAny<DateTime>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<Guid>()
            ))
            .Returns(users);
            var analyticReportController = new AnalyticReportController(
                configMock.Object,
                moqILoginService.Object,
                dbOperationMock.Object,
                filterMock.Object,
                analyticReportProviderMock.Object
            );
            filterMock.Setup(p => p.GetBegDate(It.IsAny<string>())).Returns(begDate);
            filterMock.Setup(p => p.GetEndDate(It.IsAny<string>())).Returns(endDate);
            var refList = new List<Guid>();
            filterMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter(ref refList, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));

            //Act
            var result = analyticReportController.ReportUserPartial(
                "20191127",
                "20191127",
                new List<Guid>(),
                new List<Guid>(),
                new List<Guid>(),
                new List<Guid>(),
                "Bearer token"
            );

            //Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.That(okResult.Value != null);
            var deserialized = JsonConvert.DeserializeObject<List<SessionInfo>>(okResult.Value.ToString());
            Assert.That(deserialized != null);
            Assert.IsTrue(deserialized.Count > 0);
        }
        [Test]
        public void ReportUserFull()
        {
            //Arrange
            var task = TestData.GetSessions();
            var sessionsInfo = task.Result.ToList();
            var dialogueInfo = TestData.GetDialogueInfo();
            analyticReportProviderMock.Setup(p => p.GetSessions(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<Guid>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>()
            ))
            .Returns(sessionsInfo);
            analyticReportProviderMock.Setup(p => p.GetDialoguesWithWorkerType(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<Guid>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>()
            ))
            .Returns(dialogueInfo);

            var analyticReportController = new AnalyticReportController(
                configMock.Object,
                moqILoginService.Object,
                dbOperationMock.Object,
                filterMock.Object,
                analyticReportProviderMock.Object
            );
            
            //Act
            var result = analyticReportController.ReportUserFull(
                "20191126",
                "20191127",
                new List<Guid>{Guid.Parse("2c88e4b6-f5af-49ce-95db-171514607361")},
                new List<Guid>{},
                new List<Guid>{},
                new List<Guid>{},
                "Bearer Token"
            );

            //Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.That(okResult.Value != null);
            var deserialized = JsonConvert.DeserializeObject<List<SessionInfo>>(okResult.Value.ToString());
            Assert.That(deserialized != null);
            Assert.IsTrue(deserialized.Count > 0);
        }
    }
}