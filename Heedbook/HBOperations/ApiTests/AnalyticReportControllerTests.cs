using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using UserOperations.Controllers;
using UserOperations.AccountModels;
using HBData.Models.AccountViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System;
using HBData.Models;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.EntityFrameworkCore.Query.Internal;
using UserOperations.Models.AnalyticModels;
using UserOperations.Services;

namespace ApiTests
{public class AnalyticReportControllerTests : ApiServiceTest
    {   
        private AnalyticReportService analyticReportService;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            analyticReportService = new AnalyticReportService(
                configMock.Object,
                moqILoginService.Object,
                requestFiltersMock.Object,
                repositoryMock.Object,
                analyticReportUtils.Object
            );
        }
        [Test]
        public async Task ReportActiveEmployeeGetTest()
        {
            //Arrange
            var applicationUserId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            var companyIds = new List<Guid>{companyId};
            requestFiltersMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter(ref companyIds, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));
            repositoryMock.Setup(p => p.GetAsQueryable<Session>())
                .Returns(new TestAsyncEnumerable<Session>(new List<Session>
                {
                    new Session()
                    {
                        ApplicationUserId = applicationUserId,
                        ApplicationUser = new ApplicationUser{FullName = "TestUser"},
                        BegTime = DateTime.Now.AddMinutes(-3),
                        EndTime = DateTime.Now,
                        StatusId = 6,
                        DeviceId = deviceId,
                        Device = new Device{DeviceId = deviceId, CompanyId = companyId}
                    }
                }.AsQueryable()));

            //Act
            var result = analyticReportService.ReportActiveEmployee(
                new List<Guid?>{(Guid?)applicationUserId},
                new List<Guid>{companyId},
                new List<Guid>{corporationId},
                new List<Guid>{deviceId}
            );
            var sessions = JsonConvert.DeserializeObject<List<SessionInfo>>(result);

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(sessions.Count > 0);
        }
        [Test]
        public void ReportUserPartialGetTest()
        {
            //Arrange
            var applicationUserId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            requestFiltersMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
                .Returns(DateTime.Now.AddDays(-6));
            requestFiltersMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
                .Returns(DateTime.Now);
            var companyIds = new List<Guid>{companyId};
            requestFiltersMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter(ref companyIds, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));
            repositoryMock.Setup(p => p.GetAsQueryable<Session>())
                .Returns(new TestAsyncEnumerable<Session>(new List<Session>
                {
                    new Session()
                    {
                        ApplicationUserId = applicationUserId,
                        ApplicationUser = new ApplicationUser{FullName = "TestUser"},
                        BegTime = DateTime.Now.AddMinutes(-3),
                        EndTime = DateTime.Now.AddMinutes(-1),
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
                        ApplicationUser = new ApplicationUser{FullName = "TestUser", UserRoles = new List<ApplicationUserRole>{new ApplicationUserRole{RoleId = roleId}}},
                        DialoguePhrase = new List<DialoguePhrase>{new DialoguePhrase{}},
                        DialogueClientSatisfaction = new List<DialogueClientSatisfaction>{new DialogueClientSatisfaction(){}},
                        DialogueFrame = new List<DialogueFrame>{new DialogueFrame{}},
                        SlideShowSessions = new List<SlideShowSession>{new SlideShowSession{}}
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationRole>())
                .Returns(new TestAsyncEnumerable<ApplicationRole>(new List<ApplicationRole>
                {
                    new ApplicationRole
                    {
                        Id = roleId,
                        Name = "Employee"
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationUser>())
                .Returns(new List<ApplicationUser>
                {
                    new ApplicationUser
                    {
                        Id = applicationUserId,
                        CreationDate = DateTime.Now.AddDays(-10),
                        CompanyId = companyId,
                        StatusId = 3,
                        UserRoles = new List<ApplicationUserRole>{new ApplicationUserRole{RoleId = roleId}}
                    }
                }.AsQueryable());
            analyticReportUtils.Setup(p => p.LoadIndex(It.IsAny<IGrouping<Guid?, SessionInfo>>(), It.IsAny<List<DialogueInfo>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(0.5d);
            analyticReportUtils.Setup(p => p.Min(It.IsAny<double>(), It.IsAny<double>()))
                .Returns(0.4d);
            analyticReportUtils.Setup(p => p.MaxDouble(It.IsAny<double>(), It.IsAny<double>()))
                .Returns(0.6d);
            analyticReportUtils.Setup(p => p.SessionAverageHours(It.IsAny<IGrouping<DateTime, SessionInfo>>()))
                .Returns(12.5d);
            analyticReportUtils.Setup(p => p.DialogueSumDuration(It.IsAny<IGrouping<DateTime, SessionInfo>>(), It.IsAny<List<DialogueInfo>>(), It.IsAny<Guid>()))
                .Returns(12.5d);
            analyticReportUtils.Setup(p => p.DialoguesCount(It.IsAny<List<DialogueInfo>>(), It.IsAny<Guid>(), It.IsAny<DateTime>()))
                .Returns(5);
            //Act
            var result = analyticReportService.ReportUserPartial(
                TestData.beg,
                TestData.end,
                new List<Guid?>{(Guid?)applicationUserId},
                new List<Guid>{companyId},
                new List<Guid>{corporationId},
                new List<Guid>{deviceId}
            );

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(result.Contains("FullName"));
        }
        [Test]
        public void ReportUserFullGetTest()
        {
            //Arrange
            var applicationUserId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var companyIds = new List<Guid>{companyId};
            var roleId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            requestFiltersMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
                .Returns(DateTime.Now.AddDays(-6));
            requestFiltersMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
                .Returns(DateTime.Now);
            requestFiltersMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter(ref companyIds, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationRole>())
                .Returns(new TestAsyncEnumerable<ApplicationRole>(new List<ApplicationRole>
                {
                    new ApplicationRole
                    {
                        Id = roleId,
                        Name = "Employee"
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.GetAsQueryable<Session>())
                .Returns(new TestAsyncEnumerable<Session>(new List<Session>
                {
                    new Session()
                    {
                        ApplicationUserId = applicationUserId,
                        ApplicationUser = new ApplicationUser{FullName = "TestUser"},
                        BegTime = DateTime.Now.AddMinutes(-3),
                        EndTime = DateTime.Now.AddMinutes(-1),
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
                        EndTime = DateTime.Now.AddMinutes(-1),
                        StatusId = 3,
                        InStatistic = true,
                        DeviceId = deviceId,
                        Device = new Device(){DeviceId = deviceId, CompanyId = companyId},
                        ApplicationUserId = applicationUserId,
                        ApplicationUser = new ApplicationUser{FullName = "TestUser", UserRoles = new List<ApplicationUserRole>{new ApplicationUserRole{RoleId = roleId}}},
                        DialoguePhrase = new List<DialoguePhrase>{new DialoguePhrase{}},
                        DialogueClientSatisfaction = new List<DialogueClientSatisfaction>{new DialogueClientSatisfaction(){}},
                        DialogueFrame = new List<DialogueFrame>{new DialogueFrame{}},
                        SlideShowSessions = new List<SlideShowSession>{new SlideShowSession{}}
                    }
                }.AsQueryable()));
            analyticReportUtils.Setup(p => p.LoadIndex(It.IsAny<IGrouping<Guid?, SessionInfo>>(), It.IsAny<List<DialogueInfo>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(0.5d);
            analyticReportUtils.Setup(p => p.DialogueSumDuration(It.IsAny<IGrouping<DateTime, SessionInfo>>(), It.IsAny<List<DialogueInfo>>(), It.IsAny<Guid>()))
                .Returns(12.5d);
            analyticReportUtils.Setup(p => p.SessionAverageHours(It.IsAny<IGrouping<DateTime, SessionInfo>>()))
                .Returns(12.5d);
            analyticReportUtils.Setup(p => p.TimeTable(It.IsAny<List<SessionInfo>>(), It.IsAny<List<DialogueInfo>>(), It.IsAny<Guid>(), It.IsAny<DateTime>()))
                .Returns(new List<ReportFullDayInfo>
                    {
                        new ReportFullDayInfo
                        {
                            ActivityType = 3,
                            Beg = DateTime.Now.AddMinutes(-4),
                            End = DateTime.Now.AddMinutes(-1),
                            DialogueId = Guid.NewGuid()
                        }
                    });
            
            
            //Act
            var result = analyticReportService.ReportUserFull(
                TestData.beg,
                TestData.end,
                new List<Guid?>{(Guid?)applicationUserId},
                new List<Guid>{companyId},
                new List<Guid>{corporationId},
                new List<Guid>{deviceId});
            System.Console.WriteLine($"result:\n{result}");
            var obj = JsonConvert.DeserializeObject<List<ReportFullPeriodInfo>>(result);

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(obj.Count > 0);
        }
    }
}