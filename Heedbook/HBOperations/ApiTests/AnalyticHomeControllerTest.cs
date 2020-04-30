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
using System.Collections.Generic;
using UserOperations.Models.AnalyticModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using UserOperations.Models.Get;
using System.Linq.Expressions;

namespace ApiTests
{
    [TestFixture]
    public class AnalyticHomeControllerTest : ApiServiceTest
    {
        private AnalyticHomeService analyticHomeService; 
        [SetUp]
        public new void Setup()
        {
                base.Setup();
                analyticHomeService = new AnalyticHomeService(
                    repositoryMock.Object,
                    moqILoginService.Object,
                    requestFiltersMock.Object,
                    analyticHomeUtils.Object,
                    dBOperations.Object);
        }

        [Test]
        public async Task GetDashboard()
        {
            //Arrange   
            var applicationUserId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName()).Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId()).Returns(companyId);
            requestFiltersMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
                .Returns(DateTime.Now.Date.AddDays(-6));
            requestFiltersMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
                .Returns(DateTime.Now.Date);
            var companyIds = new List<Guid>{companyId};
            requestFiltersMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter(ref companyIds, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));
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
            repositoryMock.Setup(p => p.GetAsQueryable<PhraseType>())
                .Returns(new TestAsyncEnumerable<PhraseType>(new List<PhraseType>
                {
                    new PhraseType()
                    {
                        PhraseTypeId = Guid.NewGuid(),
                        PhraseTypeText = "Cross"
                    }
                }.AsQueryable()));
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
                        SlideShowSessions = new List<SlideShowSession>{new SlideShowSession{}},
                    }
                }.AsQueryable()));
            dBOperations.Setup(p => p.CheckIfDialogueInWorkingTime(It.IsAny<Dialogue>(), It.IsAny<WorkingTime[]>()))
                .Returns(true);
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
            var intNumber = 5;
            analyticHomeUtils.Setup(p => p.DialoguesCount(It.IsAny<List<DialogueInfo>>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>()))
                .Returns(intNumber);
            var employeeRoleId = Guid.NewGuid();
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationUser>())
                .Returns(new TestAsyncEnumerable<ApplicationUser>(new List<ApplicationUser>
                {
                    new ApplicationUser()
                    {
                        Id = applicationUserId,
                        CreationDate = DateTime.Now.AddDays(-2),
                        StatusId = 3,
                        CompanyId = companyId,
                        UserRoles = new List<ApplicationUserRole>{new ApplicationUserRole{RoleId = employeeRoleId}}
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.FindOrNullOneByConditionAsync<ApplicationRole>(It.IsAny<Expression<Func<ApplicationRole, bool>>>()))
                .Returns(Task.FromResult<ApplicationRole>(new ApplicationRole{Id = employeeRoleId, Name = "Employee"}));
            analyticHomeUtils.Setup(p => p.SatisfactionIndex(It.IsAny<IGrouping<Guid?, DialogueInfo>>())).Returns(0.55d);
            analyticHomeUtils.Setup(p => p.LoadIndexWithTimeTable(It.IsAny<List<double>>(), It.IsAny<List<DialogueInfo>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(0.66d);
            analyticHomeUtils.Setup(p => p.CrossIndex(It.IsAny<IGrouping<Guid?, DialogueInfo>>())).Returns(0.77d);
            repositoryMock.Setup(p => p.GetAsQueryable<CampaignContentAnswer>())
                .Returns(new TestAsyncEnumerable<CampaignContentAnswer>(new List<CampaignContentAnswer>
                {
                    new CampaignContentAnswer()
                    {
                        CampaignContent = new CampaignContent{},
                        Time = DateTime.Now.AddDays(-1),
                        ApplicationUserId = applicationUserId,
                        DeviceId = deviceId,
                        Device = new Device{CompanyId = companyId}
                    }
                }.AsQueryable()));
            dBOperations.Setup(p => p.WorklLoadByTimeIndex(It.IsAny<List<double>>(), It.IsAny<List<DialogueInfo>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(0.88d);

            // Act
            var result = await analyticHomeService.GetDashboardFiltered(
                TestData.beg,
                TestData.end,
                new List<Guid?>{applicationUserId},
                new List<Guid>{companyId},
                new List<Guid>{Guid.NewGuid()},
                new List<Guid>{deviceId}
            );

            // Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task GetDashboardFilteredGetTest()
        {
            //Arrange   
            var applicationUserId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName()).Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId()).Returns(companyId);
            requestFiltersMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
                .Returns(DateTime.Now.Date.AddDays(-6));
            requestFiltersMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
                .Returns(DateTime.Now.Date);
            var companyIds = new List<Guid>{companyId};
            requestFiltersMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter(ref companyIds, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));
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
            repositoryMock.Setup(p => p.GetAsQueryable<PhraseType>())
                .Returns(new TestAsyncEnumerable<PhraseType>(new List<PhraseType>
                {
                    new PhraseType()
                    {
                        PhraseTypeId = Guid.NewGuid(),
                        PhraseTypeText = "Cross"
                    }
                }.AsQueryable()));
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
                        SlideShowSessions = new List<SlideShowSession>{new SlideShowSession{}},
                    }
                }.AsQueryable()));
            dBOperations.Setup(p => p.CheckIfDialogueInWorkingTime(It.IsAny<Dialogue>(), It.IsAny<WorkingTime[]>()))
                .Returns(true);
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
            var intNumber = 5;
            analyticHomeUtils.Setup(p => p.DialoguesCount(It.IsAny<List<DialogueInfo>>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>()))
                .Returns(intNumber);
            var employeeRoleId = Guid.NewGuid();
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationUser>())
                .Returns(new TestAsyncEnumerable<ApplicationUser>(new List<ApplicationUser>
                {
                    new ApplicationUser()
                    {
                        Id = applicationUserId,
                        CreationDate = DateTime.Now.AddDays(-2),
                        StatusId = 3,
                        CompanyId = companyId,
                        UserRoles = new List<ApplicationUserRole>{new ApplicationUserRole{RoleId = employeeRoleId}}
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.FindOrNullOneByConditionAsync<ApplicationRole>(It.IsAny<Expression<Func<ApplicationRole, bool>>>()))
                .Returns(Task.FromResult<ApplicationRole>(new ApplicationRole{Id = employeeRoleId, Name = "Employee"}));
            analyticHomeUtils.Setup(p => p.SatisfactionIndex(It.IsAny<IGrouping<Guid?, DialogueInfo>>())).Returns(0.55d);
            analyticHomeUtils.Setup(p => p.LoadIndexWithTimeTable(It.IsAny<List<double>>(), It.IsAny<List<DialogueInfo>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(0.66d);
            analyticHomeUtils.Setup(p => p.CrossIndex(It.IsAny<IGrouping<Guid?, DialogueInfo>>())).Returns(0.77d);
            repositoryMock.Setup(p => p.GetAsQueryable<CampaignContentAnswer>())
                .Returns(new TestAsyncEnumerable<CampaignContentAnswer>(new List<CampaignContentAnswer>
                {
                    new CampaignContentAnswer()
                    {
                        CampaignContent = new CampaignContent{},
                        Time = DateTime.Now.AddDays(-1),
                        ApplicationUserId = applicationUserId,
                        DeviceId = deviceId,
                        Device = new Device{CompanyId = companyId}
                    }
                }.AsQueryable()));
            dBOperations.Setup(p => p.WorklLoadByTimeIndex(It.IsAny<List<double>>(), It.IsAny<List<DialogueInfo>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(0.88d);

            // Act
            var result = await analyticHomeService.GetDashboardFiltered(
                TestData.beg,
                TestData.end,
                new List<Guid?>{applicationUserId},
                new List<Guid>{companyId},
                new List<Guid>{Guid.NewGuid()},
                new List<Guid>{deviceId}
            );

            // Assert
            Assert.IsFalse(result is null);
        }
    }
}
