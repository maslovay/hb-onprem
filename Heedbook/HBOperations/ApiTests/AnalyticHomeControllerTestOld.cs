//using System;
//using System.Threading.Tasks;
//using NUnit.Framework;
//using Moq;
//using UserOperations.Controllers;
//using System.Collections.Generic;
//using UserOperations.Models.AnalyticModels;
//using Microsoft.AspNetCore.Mvc;
//using Newtonsoft.Json;

//namespace ApiTests
//{
//    [TestFixture]
//    public class AnalyticHomeControllerTestOld : ApiServiceTest
//    {
//        protected override void InitData()
//        {
//            beg = "20191001";
//            end = "20191002";
//            begDate = (new DateTime(2019, 10, 03)).Date;
//            endDate = (new DateTime(2019, 10, 05)).Date;
//            prevDate = (new DateTime(2019, 10, 01)).Date;
//            token = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhbm5zYW1vbHVrX3Rlc3RAZ21haWwuY29tIiwianRpIjoiZmY3Yjc4NGQtMTEzMi00ZmY0LThlN2ItODU4YTBhMDVhMzE3IiwiYXBwbGljYXRpb25Vc2VySWQiOiJhNmI2NjgzNS1hNDEyLTRjMjAtODBiNy0yZGNhN2VhZTRjZDYiLCJhcHBsaWNhdGlvblVzZXJOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlJZCI6ImJkNGM0MmIwLWRlNmEtNDkxNS1hYzY5LWE1ZjAzNjAxOWM5ZCIsImNvcnBvcmF0aW9uSWQiOiIiLCJsYW5ndWFnZUNvZGUiOiIxIiwicm9sZSI6Ik1hbmFnZXIiLCJmdWxsTmFtZSI6ImFubnNhbW9sdWtfdGVzdEBnbWFpbC5jb20iLCJhdmF0YXIiOiIiLCJleHAiOjE1NzQ5MzQxNDUsImlzcyI6Imh0dHBzOi8vaGVlZGJvb2suY29tIiwiYXVkIjoiaHR0cHM6Ly9oZWVkYm9vay5jb20ifQ.rRRAcst-r0mD4jkn80L8yKLf9xGPhGxVaNy0tRgKUXM";
//            tokenclaims = GetClaims();
//            companyIds = GetCompanyIds();
//        }

//        [Test]
//        public async Task GetDashboard_Behavior()
//        {
//            //arrange

//            loginMock.Setup(log => log.GetDataFromToken(It.IsAny<string>(), out tokenclaims, null)).Returns(true);
//            filterMock.Setup(f => f.GetBegDate(It.IsAny<string>())).Returns(begDate);
//            filterMock.Setup(f => f.GetEndDate(It.IsAny<string>())).Returns(endDate);
//            filterMock.Setup(f => f.CheckRolesAndChangeCompaniesInFilter(ref companyIds, null, It.IsAny<string>(), It.IsAny<Guid>()));
//            commonProviderMock.Setup(c => c.GetSessionInfoAsync(prevDate, endDate, companyIds, null, null)).Returns(GetSessions());
//            commonProviderMock.Setup(c => c.GetCrossPhraseTypeIdAsync()).Returns(GetCrossPhraseId());
//            commonProviderMock.Setup(c => c.GetDialoguesIncludedPhrase(prevDate, endDate, companyIds, null, null)).Returns(GetDialoguesWithUserPhrasesSatisfaction());
//            homeProviderMock.Setup(h => h.GetBenchmarksList(begDate, endDate, companyIds)).Returns(GetBenchmarkList());

//            var controller = new AnalyticHomeController(configMock.Object, loginMock.Object, dbOperationMock.Object, filterMock.Object, commonProviderMock.Object, homeProviderMock.Object);

//            // Act

//            await controller.GetDashboard(beg, end, companyIds, null, null, token);

//            // Assert

//            loginMock.Verify(log => log.GetDataFromToken(token, out tokenclaims, null), Times.Once());
//            filterMock.Verify(f => f.GetBegDate(beg), Times.Once());
//            filterMock.Verify(f => f.GetEndDate(end), Times.Once());
//            filterMock.Verify(f => f.CheckRolesAndChangeCompaniesInFilter(ref companyIds, null, "Supervisor", Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182")));
//            commonProviderMock.Verify(c => c.GetSessionInfoAsync(prevDate, endDate, companyIds, null, null));
//            commonProviderMock.Verify(c => c.GetCrossPhraseTypeIdAsync());
//            commonProviderMock.Verify(c => c.GetDialoguesIncludedPhrase(prevDate, endDate, companyIds, null, null));
//            homeProviderMock.Verify(h => h.GetBenchmarksList(begDate, endDate, companyIds));
//        }

//        [Test]
//        public async Task GetDashboard_OkResult()
//        {
//            //arrange           

//            loginMock.Setup(log => log.GetDataFromToken(It.IsAny<string>(), out tokenclaims, null)).Returns(true);
//            filterMock.Setup(f => f.GetBegDate(It.IsAny<string>())).Returns(begDate);
//            filterMock.Setup(f => f.GetEndDate(It.IsAny<string>())).Returns(endDate);
//            filterMock.Setup(f => f.CheckRolesAndChangeCompaniesInFilter(ref companyIds, null, It.IsAny<string>(), It.IsAny<Guid>()));
//            commonProviderMock.Setup(c => c.GetSessionInfoAsync(prevDate, endDate, companyIds, null, null)).Returns(GetSessions());
//            commonProviderMock.Setup(c => c.GetCrossPhraseTypeIdAsync()).Returns(GetCrossPhraseId());
//            commonProviderMock.Setup(c => c.GetDialoguesIncludedPhrase(prevDate, endDate, companyIds, null, null)).Returns(GetDialoguesWithUserPhrasesSatisfaction());
//            homeProviderMock.Setup(h => h.GetBenchmarksList(begDate, endDate, companyIds)).Returns(GetBenchmarkList());

//            var controller = new AnalyticHomeController(configMock.Object, loginMock.Object, dbOperationMock.Object, filterMock.Object, commonProviderMock.Object, homeProviderMock.Object);

//            // Act

//            var result = await controller.GetDashboard(beg, end, null, null, null, token);

//            // Assert
//            var okResult = result as OkObjectResult;
//            Assert.IsNotNull(okResult);
//            Assert.AreEqual(200, okResult.StatusCode);
//            Assert.That(okResult.Value != null);
//            var deserialized = JsonConvert.DeserializeObject<DashboardInfo>(okResult.Value.ToString());
//            Assert.That(deserialized != null);
//            //Assert.IsInstanceOf<DashboardInfo>(okResult.Value);
//        }

//        [Test]
//        public async Task GetDashboard_CantParseDate()
//        {
//            //arrange           

//            loginMock.Setup(log => log.GetDataFromToken(It.IsAny<string>(), out tokenclaims, null)).Returns(true);
//            filterMock.Setup(f => f.GetBegDate(It.IsAny<string>())).Throws(new FormatException("wrong date format"));
//            // filterMock.Setup(f => f.GetEndDate(It.IsAny<string>())).Throws(new FormatException("wrong date format"));

//            var controller = new AnalyticHomeController(configMock.Object, loginMock.Object, dbOperationMock.Object, filterMock.Object, commonProviderMock.Object, homeProviderMock.Object);

//            // Act

//            var result = await controller.GetDashboard(beg, end, null, null, null, token);

//            // Assert

//            var badResult = result as BadRequestObjectResult;
//            Assert.IsNotNull(badResult);
//            Assert.That(badResult.Value.ToString() == "wrong date format");
//        }

//        [Test]
//        public async Task GetDashboard_WrongToken()
//        {
//            string wrongToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkVaNy0tRgKUXM";
//            Dictionary<string, string> wrongtokenclaims = null;

//            //arrange 

//            loginMock.Setup(log => log.GetDataFromToken(It.IsAny<string>(), out wrongtokenclaims, null)).Returns(false);
//            var controller = new AnalyticHomeController(configMock.Object, loginMock.Object, dbOperationMock.Object, filterMock.Object, commonProviderMock.Object, homeProviderMock.Object);

//            // Act

//            var result = await controller.GetDashboard(beg, end, null, null, null, wrongToken);

//            // Assert

//            var badResult = result as BadRequestObjectResult;
//            Assert.IsNotNull(badResult);
//            Assert.That(badResult.Value.ToString() == "Token wrong");
//        }

//        [Test]
//        public async Task GetDashboard_SessionsDialoguesNull()
//        {
//            //arrange

//            loginMock.Setup(log => log.GetDataFromToken(It.IsAny<string>(), out tokenclaims, null)).Returns(true);
//            filterMock.Setup(f => f.GetBegDate(It.IsAny<string>())).Returns(begDate);
//            filterMock.Setup(f => f.GetEndDate(It.IsAny<string>())).Returns(endDate);
//            filterMock.Setup(f => f.CheckRolesAndChangeCompaniesInFilter(ref companyIds, null, It.IsAny<string>(), It.IsAny<Guid>()));
//            commonProviderMock.Setup(c => c.GetSessionInfoAsync(prevDate, endDate, companyIds, null, null)).Returns(GetEmptySessions());
//            commonProviderMock.Setup(c => c.GetCrossPhraseTypeIdAsync()).Returns(GetCrossPhraseId());
//            commonProviderMock.Setup(c => c.GetDialoguesIncludedPhrase(prevDate, endDate, companyIds, null, null)).Returns(GetEmptyDialogues());
//            homeProviderMock.Setup(h => h.GetBenchmarksList(begDate, endDate, companyIds)).Returns(GetBenchmarkList());

//            var controller = new AnalyticHomeController(configMock.Object, loginMock.Object, dbOperationMock.Object, filterMock.Object, commonProviderMock.Object, homeProviderMock.Object);

//            // Act

//            var result = await controller.GetDashboard(beg, end, companyIds, null, null, token);

//            // Assert

//            var okResult = result as OkObjectResult;
//            Assert.IsNotNull(okResult);
//            Assert.AreEqual(200, okResult.StatusCode);
//            Assert.That(okResult.Value != null);
//            var deserialized = JsonConvert.DeserializeObject<DashboardInfo>(okResult.Value.ToString());
//            Assert.That(deserialized != null);
//        }

//        [SetUp]
//        public void Setup()
//        {
//            // base.Setup(() => { }, true);
//            base.Setup();


//        }


//        ////  protected AnalyticHomeProvider _analyticHomeProvider;
//        //[Test]
//        //public async Task GetBenchmarksListAsyncReturned()
//        //{
//        //    DateTime beg = new DateTime(2019, 10, 01);
//        //    DateTime end = new DateTime(2019, 10, 02);
//        //    var companies = await _repository.FindAllAsync<Company>();
//        //    var ids = companies.Take(10).Select(x => x.CompanyId).ToList();
//        //    var benchmarkList = await _analyticHomeProvider.GetBenchmarksList(beg, end, ids);
//        //    // Assert
//        //    Assert.AreNotEqual(benchmarkList.Count(), 0);


//        //    //arrange

//        //    //act

//        //    //assert
//        //}
//    }
//}
