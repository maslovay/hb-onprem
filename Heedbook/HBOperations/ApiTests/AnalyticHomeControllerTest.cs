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
        Mock<IRequestFilters> filterMock;
        Mock<IConfiguration> configMock;
        Mock<ILoginService> loginMock;
        Mock<IDBOperations> dbOperationMock;
        Mock<IAnalyticHomeProvider> homeProviderMock;
        Mock<IAnalyticCommonProvider> commonProviderMock;

        [Test]
        public async Task GetDashboard_Behavior()
        {
            string beg = "20191001";
            string end = "20191002";
            DateTime begDate = (new DateTime(2019, 10, 03)).Date;
            DateTime endDate = (new DateTime(2019, 10, 05)).Date;
            DateTime prevDate = (new DateTime(2019, 10, 01)).Date;
            string token = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhbm5zYW1vbHVrX3Rlc3RAZ21haWwuY29tIiwianRpIjoiZmY3Yjc4NGQtMTEzMi00ZmY0LThlN2ItODU4YTBhMDVhMzE3IiwiYXBwbGljYXRpb25Vc2VySWQiOiJhNmI2NjgzNS1hNDEyLTRjMjAtODBiNy0yZGNhN2VhZTRjZDYiLCJhcHBsaWNhdGlvblVzZXJOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlJZCI6ImJkNGM0MmIwLWRlNmEtNDkxNS1hYzY5LWE1ZjAzNjAxOWM5ZCIsImNvcnBvcmF0aW9uSWQiOiIiLCJsYW5ndWFnZUNvZGUiOiIxIiwicm9sZSI6Ik1hbmFnZXIiLCJmdWxsTmFtZSI6ImFubnNhbW9sdWtfdGVzdEBnbWFpbC5jb20iLCJhdmF0YXIiOiIiLCJleHAiOjE1NzQ5MzQxNDUsImlzcyI6Imh0dHBzOi8vaGVlZGJvb2suY29tIiwiYXVkIjoiaHR0cHM6Ly9oZWVkYm9vay5jb20ifQ.rRRAcst-r0mD4jkn80L8yKLf9xGPhGxVaNy0tRgKUXM";
            Dictionary<string, string> tokenclaims = GetClaims();
            List<Guid> companyIds = GetCompanyIds();

            //arrange

            loginMock.Setup(log => log.GetDataFromToken(It.IsAny<string>(), out tokenclaims, null)).Returns(true);
            filterMock.Setup(f => f.GetBegDate(It.IsAny<string>())).Returns(begDate);
            filterMock.Setup(f => f.GetEndDate(It.IsAny<string>())).Returns(endDate);
            filterMock.Setup(f => f.CheckRolesAndChangeCompaniesInFilter(ref companyIds, null, It.IsAny<string>(), It.IsAny<Guid>()));
            commonProviderMock.Setup(c => c.GetSessionInfoAsync(prevDate, endDate, companyIds, null, null)).Returns(GetSessions());
            commonProviderMock.Setup(c => c.GetCrossPhraseTypeIdAsync()).Returns(GetCrossPhraseId());
            commonProviderMock.Setup(c => c.GetDialoguesIncludedPhrase(prevDate, endDate, companyIds, null, null)).Returns(GetDialogues());
            homeProviderMock.Setup(h => h.GetBenchmarksList(begDate, endDate, companyIds)).Returns(GetBenchmarkList());

            var controller = new AnalyticHomeController(configMock.Object, loginMock.Object, dbOperationMock.Object, filterMock.Object, commonProviderMock.Object, homeProviderMock.Object);
            
            // Act

            await controller.GetDashboard(beg, end, companyIds, null, null, token);

            // Assert

            loginMock.Verify(log => log.GetDataFromToken(token, out tokenclaims, null), Times.Once());
            filterMock.Verify(f => f.GetBegDate(beg), Times.Once());
            filterMock.Verify(f => f.GetEndDate(end), Times.Once());
            filterMock.Verify(f => f.CheckRolesAndChangeCompaniesInFilter(ref companyIds, null, "Supervisor", Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182")));
            commonProviderMock.Verify(c => c.GetSessionInfoAsync(prevDate, endDate, companyIds, null, null));
            commonProviderMock.Verify(c => c.GetCrossPhraseTypeIdAsync());
            commonProviderMock.Verify(c => c.GetDialoguesIncludedPhrase(prevDate, endDate, companyIds, null, null));
            homeProviderMock.Verify(h => h.GetBenchmarksList(begDate, endDate, companyIds));
        }

        [Test]
        public async Task GetDashboard_OkResult()
        {
            string beg = "20191001";
            string end = "20191002";
            DateTime begDate = (new DateTime(2019, 10, 03)).Date;
            DateTime endDate = (new DateTime(2019, 10, 05)).Date;
            DateTime prevDate = (new DateTime(2019, 10, 01)).Date;
            string token = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhbm5zYW1vbHVrX3Rlc3RAZ21haWwuY29tIiwianRpIjoiZmY3Yjc4NGQtMTEzMi00ZmY0LThlN2ItODU4YTBhMDVhMzE3IiwiYXBwbGljYXRpb25Vc2VySWQiOiJhNmI2NjgzNS1hNDEyLTRjMjAtODBiNy0yZGNhN2VhZTRjZDYiLCJhcHBsaWNhdGlvblVzZXJOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlJZCI6ImJkNGM0MmIwLWRlNmEtNDkxNS1hYzY5LWE1ZjAzNjAxOWM5ZCIsImNvcnBvcmF0aW9uSWQiOiIiLCJsYW5ndWFnZUNvZGUiOiIxIiwicm9sZSI6Ik1hbmFnZXIiLCJmdWxsTmFtZSI6ImFubnNhbW9sdWtfdGVzdEBnbWFpbC5jb20iLCJhdmF0YXIiOiIiLCJleHAiOjE1NzQ5MzQxNDUsImlzcyI6Imh0dHBzOi8vaGVlZGJvb2suY29tIiwiYXVkIjoiaHR0cHM6Ly9oZWVkYm9vay5jb20ifQ.rRRAcst-r0mD4jkn80L8yKLf9xGPhGxVaNy0tRgKUXM";
            Dictionary<string, string> tokenclaims = GetClaims();
            List<Guid> companyIds = GetCompanyIds();

            //arrange           

            loginMock.Setup(log => log.GetDataFromToken(It.IsAny<string>(), out tokenclaims, null)).Returns(true);
            filterMock.Setup(f => f.GetBegDate(It.IsAny<string>())).Returns(begDate);
            filterMock.Setup(f => f.GetEndDate(It.IsAny<string>())).Returns(endDate);
            filterMock.Setup(f => f.CheckRolesAndChangeCompaniesInFilter(ref companyIds, null, It.IsAny<string>(), It.IsAny<Guid>()));
            commonProviderMock.Setup(c => c.GetSessionInfoAsync(prevDate, endDate, companyIds, null, null)).Returns(GetSessions());
            commonProviderMock.Setup(c => c.GetCrossPhraseTypeIdAsync()).Returns(GetCrossPhraseId());
            commonProviderMock.Setup(c => c.GetDialoguesIncludedPhrase(prevDate, endDate, companyIds, null, null)).Returns(GetDialogues());
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
            //Assert.IsInstanceOf<DashboardInfo>(okResult.Value);
        }

        [Test]
        public async Task GetDashboard_CantParseDate()
        {
            string beg = "01012019";
            string end = "01122019";
            string token = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhbm5zYW1vbHVrX3Rlc3RAZ21haWwuY29tIiwianRpIjoiZmY3Yjc4NGQtMTEzMi00ZmY0LThlN2ItODU4YTBhMDVhMzE3IiwiYXBwbGljYXRpb25Vc2VySWQiOiJhNmI2NjgzNS1hNDEyLTRjMjAtODBiNy0yZGNhN2VhZTRjZDYiLCJhcHBsaWNhdGlvblVzZXJOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlJZCI6ImJkNGM0MmIwLWRlNmEtNDkxNS1hYzY5LWE1ZjAzNjAxOWM5ZCIsImNvcnBvcmF0aW9uSWQiOiIiLCJsYW5ndWFnZUNvZGUiOiIxIiwicm9sZSI6Ik1hbmFnZXIiLCJmdWxsTmFtZSI6ImFubnNhbW9sdWtfdGVzdEBnbWFpbC5jb20iLCJhdmF0YXIiOiIiLCJleHAiOjE1NzQ5MzQxNDUsImlzcyI6Imh0dHBzOi8vaGVlZGJvb2suY29tIiwiYXVkIjoiaHR0cHM6Ly9oZWVkYm9vay5jb20ifQ.rRRAcst-r0mD4jkn80L8yKLf9xGPhGxVaNy0tRgKUXM";
            Dictionary<string, string> tokenclaims = GetClaims();

            //arrange           

            loginMock.Setup(log => log.GetDataFromToken(It.IsAny<string>(), out tokenclaims, null)).Returns(true);
            filterMock.Setup(f => f.GetBegDate(It.IsAny<string>())).Throws(new FormatException("wrong date format"));
           // filterMock.Setup(f => f.GetEndDate(It.IsAny<string>())).Throws(new FormatException("wrong date format"));

            var controller = new AnalyticHomeController(configMock.Object, loginMock.Object, dbOperationMock.Object, filterMock.Object, commonProviderMock.Object, homeProviderMock.Object);

            // Act

            var result = await controller.GetDashboard(beg, end, null, null, null, token);

            // Assert

            var badResult = result as BadRequestObjectResult;
            Assert.IsNotNull(badResult);
            Assert.That(badResult.Value.ToString() == "wrong date format");
        }

        [Test]
        public async Task GetDashboard_WrongToken()
        {
            string beg = "20191001";
            string end = "20191002";
            string wrongToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkVaNy0tRgKUXM";
            Dictionary<string, string> tokenclaims = null;

            //arrange 

            loginMock.Setup(log => log.GetDataFromToken(It.IsAny<string>(), out tokenclaims, null)).Returns(false);
            var controller = new AnalyticHomeController(configMock.Object, loginMock.Object, dbOperationMock.Object, filterMock.Object, commonProviderMock.Object, homeProviderMock.Object);

            // Act

            var result = await controller.GetDashboard(beg, end, null, null, null, wrongToken);

            // Assert

            var badResult = result as BadRequestObjectResult;
            Assert.IsNotNull(badResult);
            Assert.That(badResult.Value.ToString() == "Token wrong");
        }

        [Test]
        public async Task GetDashboard_SessionsDialoguesNull()
        {
            string beg = "20191001";
            string end = "20191002";
            DateTime begDate = (new DateTime(2019, 10, 03)).Date;
            DateTime endDate = (new DateTime(2019, 10, 05)).Date;
            DateTime prevDate = (new DateTime(2019, 10, 01)).Date;
            string token = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhbm5zYW1vbHVrX3Rlc3RAZ21haWwuY29tIiwianRpIjoiZmY3Yjc4NGQtMTEzMi00ZmY0LThlN2ItODU4YTBhMDVhMzE3IiwiYXBwbGljYXRpb25Vc2VySWQiOiJhNmI2NjgzNS1hNDEyLTRjMjAtODBiNy0yZGNhN2VhZTRjZDYiLCJhcHBsaWNhdGlvblVzZXJOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlJZCI6ImJkNGM0MmIwLWRlNmEtNDkxNS1hYzY5LWE1ZjAzNjAxOWM5ZCIsImNvcnBvcmF0aW9uSWQiOiIiLCJsYW5ndWFnZUNvZGUiOiIxIiwicm9sZSI6Ik1hbmFnZXIiLCJmdWxsTmFtZSI6ImFubnNhbW9sdWtfdGVzdEBnbWFpbC5jb20iLCJhdmF0YXIiOiIiLCJleHAiOjE1NzQ5MzQxNDUsImlzcyI6Imh0dHBzOi8vaGVlZGJvb2suY29tIiwiYXVkIjoiaHR0cHM6Ly9oZWVkYm9vay5jb20ifQ.rRRAcst-r0mD4jkn80L8yKLf9xGPhGxVaNy0tRgKUXM";
            Dictionary<string, string> tokenclaims = GetClaims();
            List<Guid> companyIds = GetCompanyIds();

            //arrange

            loginMock.Setup(log => log.GetDataFromToken(It.IsAny<string>(), out tokenclaims, null)).Returns(true);
            filterMock.Setup(f => f.GetBegDate(It.IsAny<string>())).Returns(begDate);
            filterMock.Setup(f => f.GetEndDate(It.IsAny<string>())).Returns(endDate);
            filterMock.Setup(f => f.CheckRolesAndChangeCompaniesInFilter(ref companyIds, null, It.IsAny<string>(), It.IsAny<Guid>()));
            commonProviderMock.Setup(c => c.GetSessionInfoAsync(prevDate, endDate, companyIds, null, null)).Returns(GetEmptySessions());
            commonProviderMock.Setup(c => c.GetCrossPhraseTypeIdAsync()).Returns(GetCrossPhraseId());
            commonProviderMock.Setup(c => c.GetDialoguesIncludedPhrase(prevDate, endDate, companyIds, null, null)).Returns(GetEmptyDialogues());
            homeProviderMock.Setup(h => h.GetBenchmarksList(begDate, endDate, companyIds)).Returns(GetBenchmarkList());

            var controller = new AnalyticHomeController(configMock.Object, loginMock.Object, dbOperationMock.Object, filterMock.Object, commonProviderMock.Object, homeProviderMock.Object);

            // Act

            var result = await controller.GetDashboard(beg, end, companyIds, null, null, token);

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
            //  base.Setup();

            filterMock = new Mock<IRequestFilters>(MockBehavior.Loose);
            configMock = new Mock<IConfiguration>();
            loginMock = new Mock<ILoginService>(MockBehavior.Loose);
            dbOperationMock = new Mock<IDBOperations>();
            homeProviderMock = new Mock<IAnalyticHomeProvider>();
            commonProviderMock = new Mock<IAnalyticCommonProvider>();
        }

        /// <summary>
        ///     GET DATA
        /// </summary>
        /// <returns></returns>

        private Dictionary<string, string> GetClaims()
        {
            return new Dictionary<string, string>
            {
                {"sub", "tuisv@heedbook.com"} ,{"jti", "afd7fc64 - 802e-486b - a9b2 - 4ef824cb3b89"},
                {"applicationUserId", "8d5cd62c-2ea0-406e-8ec1-a544d048a9d0" },
                {"applicationUserName", "tuisv@heedbook.com"},
                {"companyName", "TUI Supervisor"},
                {"companyId", "82560395-2cc3-46e8-bcef-c844f1048182"},
                {"corporationId", "71aa39f1-649d-48d6-b1ae-10c518ed5979"},
                {"languageCode", "2"},
                {"role", "Supervisor"},
                {"fullName", "tuisv@heedbook.com"},
                {"avatar",null},
                {"exp", "1575021022"},
                {"iss", "https://heedbook.com"},
                {"aud", "https://heedbook.com"}
         };
        }

        private List<Guid> GetCompanyIds()
        {
            return new List<Guid>
            {
                Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182")//,
              //  Guid.Parse("ddaa6e0b-3439-4c78-915e-2e0245332db3"),
              //  Guid.Parse("ddaa7e0b-3439-4c78-915e-2e0245332db4")
            };
        }

        private async Task<IEnumerable<SessionInfo>> GetSessions()
        {
            return new List<SessionInfo>
            {
                new SessionInfo
                {
                    ApplicationUserId = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0"),
                    BegTime = new DateTime(2019,10,04, 12, 19,00),
                    EndTime = new DateTime(2019,10,04,12,20,25),
                    CompanyId = Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182"),
                    FullName = "tuisv@heedbook.com",
                    IndustryId = Guid.Parse("99960395-2cc3-46e8-bcef-c844f1048999")
                },
                  new SessionInfo
                {
                    ApplicationUserId = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0"),
                    BegTime = new DateTime(2019,10,04, 18, 19,00),
                    EndTime = new DateTime(2019,10,04,18,25,30),
                    CompanyId = Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182"),
                    FullName = "tuisv@heedbook.com",
                    IndustryId = Guid.Parse("99960395-2cc3-46e8-bcef-c844f1048999")
                }
            };
        }


        private async Task<IEnumerable<SessionInfo>> GetEmptySessions()
        {
            return null;
        }

        private async Task<Guid> GetCrossPhraseId()
        {
            return Guid.Parse("55560395-2cc3-46e8-bcef-c844f1048555");
        }

        private IQueryable<Dialogue> GetDialogues()
        {
            var dialogues =  new List<Dialogue>
            {
                new Dialogue
                {
                    ApplicationUserId = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0"),
                    BegTime = new DateTime(2019,10,04, 12, 19,00),
                    EndTime = new DateTime(2019,10,04,12,20,25),
                    CreationTime = new DateTime(2019,10,04, 12, 19,00),
                    InStatistic = true,
                    StatusId = 3,
                    PersonId = Guid.Parse("1d1cd12c-2ea0-406e-8ec1-a544d018a1d1"),
                    DialogueId = Guid.Parse("2d2cd22c-2ea0-406e-8ec1-a544d012a2d2"),
                    LanguageId = 2                    ,
                    DialoguePhrase = new List<DialoguePhrase>
                    {
                        new DialoguePhrase
                        {
                            DialogueId = Guid.Parse("2d2cd22c-2ea0-406e-8ec1-a544d012a2d2"),
                            DialoguePhraseId = Guid.Parse("5d5cd55c-5ea0-406e-8ec1-a544d012a2d2"),
                            IsClient = true,
                            PhraseId = Guid.Parse("6d6cd66c-6ea0-606e-8ec1-a544d012a2d2"),
                            PhraseTypeId =  Guid.Parse("55560395-2cc3-46e8-bcef-c844f1048555")//cross
        }
                    },
                   ApplicationUser = new ApplicationUser
                   {
                       FullName = "tuisv@heedbook.com"
                   },
                   DialogueClientSatisfaction = new List<DialogueClientSatisfaction>
                   {
                       new DialogueClientSatisfaction
                       {
                           MeetingExpectationsTotal = 0.45,
                           BegMoodByNN = 0.4,
                           EndMoodByNN = 0.7
                       }
                   }
                },
                  new Dialogue
                {
                    ApplicationUserId = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0"),
                    BegTime = new DateTime(2019,10,04, 18, 19,00),
                    EndTime = new DateTime(2019,10,04,18,25,30),
                    CreationTime = new DateTime(2019,10,04, 19, 19,00),
                    InStatistic = true,
                    StatusId = 3,
                    PersonId = Guid.Parse("3d3cd13c-2ea0-406e-8ec1-a544d018a333"),
                    DialogueId = Guid.Parse("4d4cd44c-2ea0-406e-8ec1-a544d012a3d3"),
                    LanguageId = 2,
                    DialoguePhrase = new List<DialoguePhrase>
                    {
                        new DialoguePhrase
                        {
                            DialogueId = Guid.Parse("4d4cd44c-2ea0-406e-8ec1-a544d012a3d3"),
                            DialoguePhraseId = Guid.Parse("7d5cd55c-5ea0-406e-8ec1-a544d012a2d7"),
                            IsClient = true,
                            PhraseId = Guid.Parse("6d6cd66c-6ea0-606e-8ec1-a544d012a2d2"),
                            PhraseTypeId = Guid.Parse("7d7cd77c-7ea0-406e-7ec1-a544d012a2d2")
                        }
                    },
                   ApplicationUser = new ApplicationUser
                   {
                       FullName = "tuisv@heedbook.com"
                   },
                   DialogueClientSatisfaction = new List<DialogueClientSatisfaction>
                   {
                       new DialogueClientSatisfaction
                       {
                           MeetingExpectationsTotal = 0.5,
                           BegMoodByNN = 0.5,
                           EndMoodByNN = 0.6
                       }
                   }
                }
            };
            return dialogues.AsQueryable();
        }

        private IQueryable<Dialogue> GetEmptyDialogues()
        {
            return new List<Dialogue>().AsQueryable();
        }

        private async Task<IEnumerable<BenchmarkModel>> GetBenchmarkList()
        {
            return new List<BenchmarkModel>{
                new BenchmarkModel
                {
                    Name = "SatisfactionIndexIndustryAvg",
                    Value = 0.7
                }};

        }


        ////  protected AnalyticHomeProvider _analyticHomeProvider;
        //[Test]
        //public async Task GetBenchmarksListAsyncReturned()
        //{
        //    DateTime beg = new DateTime(2019, 10, 01);
        //    DateTime end = new DateTime(2019, 10, 02);
        //    var companies = await _repository.FindAllAsync<Company>();
        //    var ids = companies.Take(10).Select(x => x.CompanyId).ToList();
        //    var benchmarkList = await _analyticHomeProvider.GetBenchmarksList(beg, end, ids);
        //    // Assert
        //    Assert.AreNotEqual(benchmarkList.Count(), 0);


        //    //arrange

        //    //act

        //    //assert
        //}
    }
}
