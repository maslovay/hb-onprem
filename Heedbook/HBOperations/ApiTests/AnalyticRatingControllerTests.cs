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
using UserOperations.Models.Get.AnalyticRatingController;

namespace ApiTests
{
    public class AnalyticRatingControllerTests : ApiServiceTest
    {
        private AnalyticRatingService analyticRatingService;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            analyticRatingService = new AnalyticRatingService(
                moqILoginService.Object,
                requestFiltersMock.Object,
                analyticRatingUtils.Object,
                repositoryMock.Object,
                dBOperations.Object);
        }
        [Test]
        public async Task RatingProgressGetTests()
        {
            //Arrange
            var applicationUserId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName()).Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId()).Returns(companyId);
            requestFiltersMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
                .Returns(DateTime.Now.Date.AddDays(-6));
            requestFiltersMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
                .Returns(DateTime.Now.Date);
            var companyIds = new List<Guid>{companyId};
            requestFiltersMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter(ref companyIds, It.IsAny<List<Guid>>()));
            repositoryMock.Setup(p => p.GetAsQueryable<PhraseType>())
                .Returns(new TestAsyncEnumerable<PhraseType>(new List<PhraseType>
                {
                    new PhraseType()
                    {
                        PhraseTypeId = Guid.NewGuid(),
                        PhraseTypeText = "Cross"
                    }
                }.AsQueryable()));
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
            analyticRatingUtils.Setup(p => p.LoadIndex(It.IsAny<double?>(), It.IsAny<double?>()))
                .Returns(0.5d);
            analyticRatingUtils.Setup(p => p.SessionAverageHours(It.IsAny<List<SessionInfo>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(0.5d);
            analyticRatingUtils.Setup(p => p.DialogueSumDuration(It.IsAny<IGrouping<DateTime, DialogueInfo>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(10.0d);
            analyticRatingUtils.Setup(p => p.DialogueAverageDuration(It.IsAny<IGrouping<DateTime, DialogueInfo>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(10.0d);
            analyticRatingUtils.Setup(p => p.CrossIndex(It.IsAny<IGrouping<DateTime, DialogueInfo>>()))
                .Returns(10.0d);
            

            //Act
            var result = await analyticRatingService.RatingProgress(
                TestData.beg,
                TestData.end,
                new List<Guid?>{applicationUserId},
                new List<Guid>{companyId},
                new List<Guid>{corporationId},
                new List<Guid>{deviceId});

            //Assert
            Assert.IsTrue(result.Length > 0);
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task RatingUsersGetTests()
        {
            //Arrange
            var applicationUserId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName()).Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId()).Returns(companyId);
            requestFiltersMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
                .Returns(DateTime.Now.AddDays(-6));
            requestFiltersMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
                .Returns(DateTime.Now);
            repositoryMock.Setup(p => p.GetAsQueryable<Session>())
                .Returns(new TestAsyncEnumerable<Session>(new List<Session>
                {
                    new Session()
                    {
                        ApplicationUserId = applicationUserId,
                        ApplicationUser = new ApplicationUser
                        {
                            Id = applicationUserId,
                            FullName = "TestUser"
                        },
                        BegTime = DateTime.Now.AddMinutes(-3),
                        EndTime = DateTime.Now.AddMinutes(-1),
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
            dBOperations.Setup(p => p.WorkingTimeDoubleListForOneUserInCompanys(It.IsAny<WorkingTime[]>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),It.IsAny<List<Guid>>(), It.IsAny<List<Device>>(), It.IsAny<string>()))
                .Returns(new List<CompanyTimeTable>
                {
                    new CompanyTimeTable
                    {
                        CompanyId = companyId,
                        TimeTable = new List<double>{0.5d, 0.4d, 0.3d, 0.2d, 0.1d}
                    }
                });
            analyticRatingUtils.Setup(p => p.SatisfactionIndex(It.IsAny<IGrouping<Guid?, DialogueInfo>>()))
                .Returns(0.5d);
            analyticRatingUtils.Setup(p => p.LoadIndexWithTimeTableForUser(It.IsAny<List<CompanyTimeTable>>(), It.IsAny<List<DialogueInfo>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(0.5d);
            analyticRatingUtils.Setup(p => p.CrossIndex(It.IsAny<IGrouping<Guid?, DialogueInfo>>()))
                .Returns(0.5d);

            //Act
            var result = await analyticRatingService.RatingUsers(
                TestData.beg,
                TestData.end,
                new List<Guid?>{applicationUserId},
                new List<Guid>{companyId},
                new List<Guid>{corporationId},
                new List<Guid>{deviceId});
            var ratingUserInfo = JsonConvert.DeserializeObject<List<RatingUserInfo>>(result);

            //Assert
            Assert.IsTrue(result.Length > 0);
            Assert.IsTrue(ratingUserInfo is List<RatingUserInfo>);
        }
        [Test]
        public async Task RatingOfficesGetTests()
        {
            //Arrange
            var applicationUserId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName()).Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId()).Returns(companyId);
            requestFiltersMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
                .Returns(DateTime.Now.AddDays(-6));
            requestFiltersMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
                .Returns(DateTime.Now);
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
            var phraseTypeId = Guid.NewGuid();
            repositoryMock.Setup(p => p.GetAsQueryable<PhraseType>())
                .Returns(new TestAsyncEnumerable<PhraseType>(new List<PhraseType>
                {
                    new PhraseType()
                    {
                        PhraseTypeId = phraseTypeId,
                        PhraseTypeText = "Cross"
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
                        Device = new Device
                        {
                            DeviceId = deviceId, 
                            CompanyId = companyId,
                            Company = new Company{CompanyName = "TestCompany"}
                        },
                        ApplicationUserId = applicationUserId,
                        DialoguePhrase = new List<DialoguePhrase>{new DialoguePhrase{PhraseTypeId = phraseTypeId}},
                        DialogueClientSatisfaction = new List<DialogueClientSatisfaction>{new DialogueClientSatisfaction(){MeetingExpectationsTotal = 0.5d}},
                        DialogueFrame = new List<DialogueFrame>{new DialogueFrame{}},
                        SlideShowSessions = new List<SlideShowSession>{new SlideShowSession{}}
                    }
                }.AsQueryable()));
            dBOperations.Setup(p => p.WorklLoadByTimeIndex(It.IsAny<List<double>>(), It.IsAny<List<DialogueInfo>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(0.5d);
            dBOperations.Setup(p => p.WorkingTimeDoubleListInMinForOneCompany(It.IsAny<WorkingTime[]>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid>(), It.IsAny<int>()))
                .Returns(new List<double>{10, 12, 12, 12, 12, 21});
            analyticRatingUtils.Setup(p => p.CrossIndex(It.IsAny<IGrouping<Guid?, DialogueInfo>>()))
                .Returns(0.5d);
            analyticRatingUtils.Setup(p => p.DialogueAverageDuration(It.IsAny<IGrouping<DateTime, DialogueInfo>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(0.5d);
            analyticRatingUtils.Setup(p => p.DialogueHourAveragePause(It.IsAny<List<double>>(), It.IsAny<List<DialogueInfo>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(0.5d);
            dBOperations.Setup(p => p.WorkingTimeDoubleListInMinForOneCompany(It.IsAny<WorkingTime[]>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Guid>(), It.IsAny<int>()))
                .Returns(new List<double>{10, 12, 12, 12, 12, 21});
            dBOperations.Setup(p => p.CheckIfDialogueInWorkingTime(It.IsAny<Dialogue>(), It.IsAny<WorkingTime[]>()))
                .Returns(true);

            //Act
            var result = await analyticRatingService.RatingOffices(
                TestData.beg,
                TestData.end,
                new List<Guid>{companyId},
                new List<Guid>{corporationId});
            var ratingOfficeInfo = JsonConvert.DeserializeObject<List<RatingOfficeInfo>>(result);

            //Assert
            System.Console.WriteLine(result.Length);
            Assert.IsTrue(result.Length > 0);
            Assert.IsTrue(ratingOfficeInfo is List<RatingOfficeInfo>);
        }
    }
}