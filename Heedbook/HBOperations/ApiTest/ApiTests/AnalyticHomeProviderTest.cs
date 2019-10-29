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

namespace ApiTests
{
    [TestFixture]
    public class AnalyticHomeProviderTest : ApiServiceTest
    {
        //  protected AnalyticHomeProvider _analyticHomeProvider;
        [Test]
        public async Task GetBenchmarksListAsyncReturned()
        {
            DateTime beg = new DateTime(2019, 10, 01);
            DateTime end = new DateTime(2019, 10, 02);
            var companies = await _repository.FindAllAsync<Company>();
            var ids = companies.Take(10).Select(x => x.CompanyId).ToList();
            var benchmarkList = await _analyticHomeProvider.GetBenchmarksList(beg, end, ids);
            // Assert
            Assert.AreNotEqual(benchmarkList.Count(), 0);


            //arrange

            //act

            //assert
        }

        [Test]
        public async Task GetDashboard_CallAll()
        {
            string beg = "20191001";
            string end = "20191002";
            string token = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhbm5zYW1vbHVrX3Rlc3RAZ21haWwuY29tIiwianRpIjoiZmY3Yjc4NGQtMTEzMi00ZmY0LThlN2ItODU4YTBhMDVhMzE3IiwiYXBwbGljYXRpb25Vc2VySWQiOiJhNmI2NjgzNS1hNDEyLTRjMjAtODBiNy0yZGNhN2VhZTRjZDYiLCJhcHBsaWNhdGlvblVzZXJOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlJZCI6ImJkNGM0MmIwLWRlNmEtNDkxNS1hYzY5LWE1ZjAzNjAxOWM5ZCIsImNvcnBvcmF0aW9uSWQiOiIiLCJsYW5ndWFnZUNvZGUiOiIxIiwicm9sZSI6Ik1hbmFnZXIiLCJmdWxsTmFtZSI6ImFubnNhbW9sdWtfdGVzdEBnbWFpbC5jb20iLCJhdmF0YXIiOiIiLCJleHAiOjE1NzQ5MzQxNDUsImlzcyI6Imh0dHBzOi8vaGVlZGJvb2suY29tIiwiYXVkIjoiaHR0cHM6Ly9oZWVkYm9vay5jb20ifQ.rRRAcst-r0mD4jkn80L8yKLf9xGPhGxVaNy0tRgKUXM";

            //arrange
            var filterMock = new Mock<IRequestFilters>();
            var config = new Mock<IConfiguration>();
            var login = new Mock<ILoginService>();
            var dbOperation = new Mock<IDBOperations>();
            var homeProvider = new Mock<IAnalyticHomeProvider>();
            var commonProvider = new Mock<IAnalyticCommonProvider>();

            var controller = new AnalyticHomeController(config.Object, login.Object, dbOperation.Object, filterMock.Object, commonProvider.Object, homeProvider.Object);

         //   filterMock.Setup(lw => lw.GetBegDate(It.IsAny<string>()));
            filterMock.Setup(lw => lw.GetEndDate(It.IsAny<string>()));

            var companyIds = new List<Guid> { new Guid() };
        //    filterMock.Setup(lw => lw.CheckRolesAndChangeCompaniesInFilter(ref companyIds, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));
         //   filterMock.Setup(lw => lw.CheckAbilityToDeleteUser(It.IsAny<string>(), It.IsAny<Guid>()));

            // Act
            await controller.GetDashboard(beg, end, null, null, null, token);

            // Assert
            // Checking that Write method of the ILogWriter was called
            filterMock.VerifyAll();
        }
        [SetUp]
        public void Setup()
        {
            // base.Setup(() => { }, true);
            base.Setup();
        }

        //protected override Task CleanTestData()
        //{
        //    return new Task(() => { });
        //}

        //protected override void InitServices()
        //{
        //    Services.AddScoped<AnalyticContentProvider>();
        //    Services.AddScoped<AnalyticCommonProvider>();
        //    Services.AddScoped<AnalyticHomeProvider>();

        //    _analyticHomeProvider = ServiceProvider.GetService<AnalyticHomeProvider>();
        //}

        //protected override Task PrepareTestData()
        //{
        //    return new Task(() => { });
        //}
    }
}
