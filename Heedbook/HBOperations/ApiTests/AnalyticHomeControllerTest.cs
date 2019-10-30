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
        protected override void InitData()
        {
            beg = "20191001";
            end = "20191002";
            begDate = (new DateTime(2019, 10, 03)).Date;
            endDate = (new DateTime(2019, 10, 05)).Date;
            prevDate = (new DateTime(2019, 10, 01)).Date;
            token = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhbm5zYW1vbHVrX3Rlc3RAZ21haWwuY29tIiwianRpIjoiZmY3Yjc4NGQtMTEzMi00ZmY0LThlN2ItODU4YTBhMDVhMzE3IiwiYXBwbGljYXRpb25Vc2VySWQiOiJhNmI2NjgzNS1hNDEyLTRjMjAtODBiNy0yZGNhN2VhZTRjZDYiLCJhcHBsaWNhdGlvblVzZXJOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlJZCI6ImJkNGM0MmIwLWRlNmEtNDkxNS1hYzY5LWE1ZjAzNjAxOWM5ZCIsImNvcnBvcmF0aW9uSWQiOiIiLCJsYW5ndWFnZUNvZGUiOiIxIiwicm9sZSI6Ik1hbmFnZXIiLCJmdWxsTmFtZSI6ImFubnNhbW9sdWtfdGVzdEBnbWFpbC5jb20iLCJhdmF0YXIiOiIiLCJleHAiOjE1NzQ5MzQxNDUsImlzcyI6Imh0dHBzOi8vaGVlZGJvb2suY29tIiwiYXVkIjoiaHR0cHM6Ly9oZWVkYm9vay5jb20ifQ.rRRAcst-r0mD4jkn80L8yKLf9xGPhGxVaNy0tRgKUXM";
            tokenclaims = GetClaims();
            companyIds = GetCompanyIds();
        }

        [Test]
        public async Task GetDashboard_OkResult()
        {
            //arrange           

            loginMock.Setup(log => log.GetDataFromToken(It.IsAny<string>(), out tokenclaims, null)).Returns(true);
            filterMock.Setup(f => f.GetBegDate(It.IsAny<string>())).Returns(begDate);
            filterMock.Setup(f => f.GetEndDate(It.IsAny<string>())).Returns(endDate);
            filterMock.Setup(f => f.CheckRolesAndChangeCompaniesInFilter(ref companyIds, null, It.IsAny<string>(), It.IsAny<Guid>()));
            commonProviderMock.Setup(c => c.GetSessionInfoAsync(prevDate, endDate, companyIds, null, null)).Returns(GetSessions());
            commonProviderMock.Setup(c => c.GetCrossPhraseTypeIdAsync()).Returns(GetCrossPhraseId());
            commonProviderMock.Setup(c => c.GetDialoguesIncludedPhrase(prevDate, endDate, companyIds, null, null)).Returns(GetDialoguesWithUserPhrasesSatisfaction());
            homeProviderMock.Setup(h => h.GetBenchmarksList(begDate, endDate, companyIds)).Returns(GetBenchmarkList());

            var controller = new AnalyticHomeController(configMock.Object, loginMock.Object, dbOperationMock.Object, filterMock.Object, commonProviderMock.Object, homeProviderMock.Object);

            // Act

            var result = await controller.GetDashboard(beg, end, null, null, null, token);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.That(okResult.Value != null);
            var deserialized = JsonConvert.DeserializeObject<DashboardInfo>(okResult.Value.ToString());
            Assert.That(deserialized != null);
        }


        [SetUp]
        public void Setup()
        {
            // base.Setup(() => { }, true);
              base.Setup();

         
        }
    }
}
