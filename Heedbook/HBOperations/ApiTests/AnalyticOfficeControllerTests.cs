using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using UserOperations.Controllers;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Models.AnalyticModels;
using UserOperations.Services;
using HBData.Models;
using System.Linq;

namespace ApiTests
{
    public class AnalyticOfficeControllerTests : ApiServiceTest
    {   
        private AnalyticOfficeService analyticOfficeService;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            analyticOfficeService = new AnalyticOfficeService(
                moqILoginService.Object,
                requestFiltersMock.Object,
                repositoryMock.Object,
                analyticOfficeUtils.Object,
                dBOperations.Object
            );
        }
        [Test]
        public async Task EfficiencyGetTest()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var applicationUserId = Guid.NewGuid();

            moqILoginService.Setup(p => p.GetCurrentRoleName()).Returns("Employee");
            moqILoginService.Setup(p => p.GetCurrentCompanyId()).Returns(companyId);
            requestFiltersMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
                .Returns(DateTime.Now.Date.AddDays(-6));
            requestFiltersMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
                .Returns(DateTime.Now.Date);
            var companyIds = new List<Guid>{companyId};
            requestFiltersMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter(ref companyIds, It.IsAny<List<Guid>>()));
            repositoryMock.Setup(p => p.GetAsQueryable<WorkingTime>())
                .Returns(new TestAsyncEnumerable<WorkingTime>(new List<WorkingTime>
                {
                    new WorkingTime()
                    {
                        CompanyId = companyId,
                        Day = 1
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.GetAsQueryable<Device>())
                .Returns(new TestAsyncEnumerable<Device>(new List<Device>
                {
                    new Device()
                    {
                        DeviceId = deviceId,
                        CompanyId = companyId,
                        StatusId = 3
                    }
                }.AsQueryable()));
            dBOperations.Setup(p => p.WorkingTimeDoubleList(It.IsAny<WorkingTime[]>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<List<Guid>>(), It.IsAny<List<Device>>(), It.IsAny<string>()))
                .Returns(new List<Double>{0.5d, 0.4d, 0.3d, 0.1d});
            repositoryMock.Setup(p => p.GetAsQueryable<Session>())
                .Returns(new TestAsyncEnumerable<Session>(new List<Session>
                {
                    new Session()
                    {
                        ApplicationUserId = applicationUserId,
                        BegTime = DateTime.Now.AddMinutes(-3),
                        EndTime = DateTime.Now,
                        StatusId = 7,
                        DeviceId = deviceId,
                        Device = new Device{DeviceId = deviceId, CompanyId = companyId}
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.GetAsQueryable<Dialogue>())
                .Returns(new TestAsyncEnumerable<Dialogue>(new List<Dialogue>
                {
                    new Dialogue()
                    {
                        DialogueId = Guid.NewGuid(),
                        BegTime = DateTime.Now.AddMinutes(-3),
                        EndTime = DateTime.Now,
                        StatusId = 3,
                        InStatistic = true,
                        DeviceId = deviceId,
                        Device = new Device(){DeviceId = deviceId, CompanyId = companyId},
                        ApplicationUserId = applicationUserId,
                        DialoguePhrase = new List<DialoguePhrase>{new DialoguePhrase{}},
                        DialogueClientSatisfaction = new List<DialogueClientSatisfaction>{new DialogueClientSatisfaction(){}},
                        DialogueFrame = new List<DialogueFrame>{new DialogueFrame{}},
                        SlideShowSessions = new List<SlideShowSession>{new SlideShowSession{}}
                    }
                }.AsQueryable()));
            var benchmarkId = Guid.NewGuid();
            var benchmarkNameId = Guid.NewGuid();
            repositoryMock.Setup(p => p.Get<Benchmark>())
                .Returns(new List<Benchmark>
                {
                    new Benchmark
                    {
                        Id = benchmarkId,
                        Day = DateTime.Now.Date.AddDays(-1),
                        BenchmarkNameId = benchmarkNameId,
                        BenchmarkName = new BenchmarkName{Id = benchmarkNameId, Name = "testBenchmark"}
                    }
                });
            repositoryMock.Setup(p => p.Get<BenchmarkName>())
                .Returns(new List<BenchmarkName>
                {
                    new BenchmarkName
                    {
                        Id = benchmarkNameId,
                        Name = "testBenchmark"
                    }
                });
            dBOperations.Setup(p => p.WorklLoadByTimeIndex(It.IsAny<List<double>>(), It.IsAny<List<DialogueInfo>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(0.88d);
            analyticOfficeUtils.Setup(p => p.DialoguesCount(It.IsAny<List<DialogueInfo>>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>()))
                .Returns(5);
            analyticOfficeUtils.Setup(p => p.SessionAverageHours(It.IsAny<List<SessionInfo>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(5.0d);
            analyticOfficeUtils.Setup(p => p.DialogueAverageDuration(It.IsAny<List<DialogueInfo>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(5.0d);
            analyticOfficeUtils.Setup(p => p.SatisfactionIndex(It.IsAny<List<DialogueInfo>>()))
                .Returns(5.0d);
            analyticOfficeUtils.Setup(p => p.LoadIndex(It.IsAny<List<SessionInfo>>(), It.IsAny<List<DialogueInfo>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(5.0d);
            analyticOfficeUtils.Setup(p => p.EmployeeCount(It.IsAny<List<DialogueInfo>>()))
                .Returns(5);
            analyticOfficeUtils.Setup(p => p.DeviceCount(It.IsAny<List<DialogueInfo>>()))
                .Returns(5);
            //Act
            var result = await analyticOfficeService.Efficiency(
                TestData.beg,
                TestData.end,
                new List<Guid?>{},
                new List<Guid>{companyId},
                new List<Guid>{},
                new List<Guid>{deviceId});

            //Assert
            Assert.IsTrue(result.Contains("Workload"));
            Assert.IsFalse(result is null);
        }
    }
}