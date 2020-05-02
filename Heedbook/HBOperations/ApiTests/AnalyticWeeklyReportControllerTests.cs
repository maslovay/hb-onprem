using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using System;
using HBData.Models;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using UserOperations.Models.AnalyticModels;
using UserOperations.Services;
using UserOperations.Models.Get.AnalyticServiceQualityController;
using UserOperations.Models.Get.AnalyticSpeechController;

namespace ApiTests
{
    public class AnalyticWeeklyReportControllerTests : ApiServiceTest
    {   
        private AnalyticWeeklyReportService analyticWeeklyReportService;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            analyticWeeklyReportService = new AnalyticWeeklyReportService(
                moqILoginService.Object,
                requestFiltersMock.Object,
                repositoryMock.Object,
                analyticWeeklyReportUtils.Object);
        }
        [Test]
        public async Task UserGetTask()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var typeIdCross = Guid.NewGuid();
            var typeIdAlert = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            requestFiltersMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
                .Returns(DateTime.Now.AddDays(-6));
            requestFiltersMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
                .Returns(DateTime.Now);
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationUser>())
                .Returns(new TestAsyncEnumerable<ApplicationUser>(new List<ApplicationUser>
                {
                    new ApplicationUser
                    {
                        Id = userId,
                        CompanyId = companyId,
                        UserRoles = new List<ApplicationUserRole>
                        {
                            new ApplicationUserRole
                            {
                                RoleId = roleId
                            }
                        }
                    }
                }));
            repositoryMock.Setup(p => p.GetAsQueryable<Company>())
                .Returns(new TestAsyncEnumerable<Company>(new List<Company>
                {
                    new Company
                    {
                        CompanyId = companyId,
                        CorporationId = corporationId
                    }
                }));
            repositoryMock.Setup(p => p.GetAsQueryable<ApplicationRole>())
                .Returns(new TestAsyncEnumerable<ApplicationRole>(new List<ApplicationRole>
                {
                    new ApplicationRole
                    {
                        Id = roleId,
                        Name = "Employee"
                    }
                }));
            repositoryMock.Setup(p => p.GetAsQueryable<Company>())
                .Returns(new TestAsyncEnumerable<Company>(new List<Company>
                {
                    new Company
                    {
                        CompanyId = companyId,
                        CorporationId = corporationId,
                        ApplicationUser = new List<ApplicationUser>
                        {
                            new ApplicationUser
                            {
                                Id = userId,
                                UserRoles = new List<ApplicationUserRole>
                                {
                                    new ApplicationUserRole
                                    {
                                        RoleId = roleId
                                    }
                                }
                            }
                        }
                    }
                }));
            repositoryMock.Setup(p => p.GetAsQueryable<VSessionUserWeeklyReport>())
                .Returns(new TestAsyncEnumerable<VSessionUserWeeklyReport>(new List<VSessionUserWeeklyReport>
                {
                    new VSessionUserWeeklyReport
                    {
                        AspNetUserId = userId,
                        Day = DateTime.Now.AddMinutes(-2)
                    },
                    new VSessionUserWeeklyReport
                    {
                        AspNetUserId = userId,
                        Day = DateTime.Now.AddDays(-7)
                    }
                }));
            repositoryMock.Setup(p => p.GetAsQueryable<VWeeklyUserReport>())
                .Returns(new TestAsyncEnumerable<VWeeklyUserReport>(new List<VWeeklyUserReport>
                {
                    new VWeeklyUserReport
                    {
                        AspNetUserId = userId,
                        Day = DateTime.Now.AddMinutes(-2)
                    },
                    new VWeeklyUserReport
                    {
                        AspNetUserId = userId,
                        Day = DateTime.Now.AddDays(-7)
                    }
                }));
            analyticWeeklyReportUtils.Setup(p => p.TotalAvg(It.IsAny<List<VWeeklyUserReport>>(), It.IsAny<string>()))
                .Returns(0.5d);
            analyticWeeklyReportUtils.Setup(p => p.AvgPerDay(It.IsAny<List<VWeeklyUserReport>>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(new Dictionary<DateTime, double>
                {
                    {DateTime.Now.AddDays(-2), 0.5d}
                });
            analyticWeeklyReportUtils.Setup(p => p.OfficeRatingSatisfactionPlace(It.IsAny<List<VWeeklyUserReport>>(), It.IsAny<Guid>()))
                .Returns(1);
            analyticWeeklyReportUtils.Setup(p => p.OfficeRatingSatisfactionPlace(It.IsAny<List<VWeeklyUserReport>>(), It.IsAny<Guid>()))
                .Returns(1);
            analyticWeeklyReportUtils.Setup(p => p.OfficeRatingSatisfactionPlace(It.IsAny<List<VWeeklyUserReport>>(), It.IsAny<Guid>()))
                .Returns(1);
            analyticWeeklyReportUtils.Setup(p => p.OfficeRatingPositiveEmotPlace(It.IsAny<List<VWeeklyUserReport>>(), It.IsAny<Guid>()))
                .Returns(1);
            analyticWeeklyReportUtils.Setup(p => p.OfficeRatingPositiveIntonationPlace(It.IsAny<List<VWeeklyUserReport>>(), It.IsAny<Guid>()))
                .Returns(1);
            analyticWeeklyReportUtils.Setup(p => p.OfficeRatingSpeechEmotPlace(It.IsAny<List<VWeeklyUserReport>>(), It.IsAny<Guid>()))
                .Returns(1);
            analyticWeeklyReportUtils.Setup(p => p.AvgNumberOfDialoguesPerDay(It.IsAny<List<VWeeklyUserReport>>(), It.IsAny<DateTime>()))
                .Returns(new Dictionary<DateTime, double>
                {
                    {DateTime.Now.AddDays(-2), 0.5d}
                });
            analyticWeeklyReportUtils.Setup(p => p.OfficeRatingDialoguesAmount(It.IsAny<List<VWeeklyUserReport>>(), It.IsAny<Guid>()))
                .Returns(1);
            analyticWeeklyReportUtils.Setup(p => p.AvgWorkingHoursPerDay(It.IsAny<List<VSessionUserWeeklyReport>>(), It.IsAny<DateTime>()))
                .Returns(new Dictionary<DateTime, double>
                {
                    {DateTime.Now.AddDays(-2), 0.5d}
                });
            analyticWeeklyReportUtils.Setup(p => p.OfficeRatingWorkingHours(It.IsAny<List<VSessionUserWeeklyReport>>(), It.IsAny<Guid>()))
                .Returns(1);
            analyticWeeklyReportUtils.Setup(p => p.AvgDialogueTimeTotal(It.IsAny<List<VWeeklyUserReport>>()))
                .Returns(0.5d);
            analyticWeeklyReportUtils.Setup(p => p.AvgDialogueTimePerDay(It.IsAny<List<VWeeklyUserReport>>(), It.IsAny<DateTime>()))
                .Returns(new Dictionary<DateTime, double>
                {
                    {DateTime.Now.AddDays(-2), 0.5d}
                });
            analyticWeeklyReportUtils.Setup(p => p.OfficeRatingDialogueTime(It.IsAny<List<VWeeklyUserReport>>(), It.IsAny<Guid>()))
                .Returns(1);
            analyticWeeklyReportUtils.Setup(p => p.WorkloadTotal(It.IsAny<List<VWeeklyUserReport>>(), It.IsAny<List<VSessionUserWeeklyReport>>()))
                .Returns(0.5d);
            analyticWeeklyReportUtils.Setup(p => p.AvgWorkloadPerDay(It.IsAny<List<VWeeklyUserReport>>(), It.IsAny<List<VSessionUserWeeklyReport>>(), It.IsAny<DateTime>()))
                .Returns(new Dictionary<DateTime, double>
                {
                    {DateTime.Now.AddDays(-2), 0.5d}
                });
            analyticWeeklyReportUtils.Setup(p => p.OfficeRatingWorkload(It.IsAny<List<VWeeklyUserReport>>(), It.IsAny<List<VSessionUserWeeklyReport>>(), It.IsAny<Guid>()))
                .Returns(1);
            analyticWeeklyReportUtils.Setup(p => p.PhraseTotalAvg(It.IsAny<List<VWeeklyUserReport>>(), It.IsAny<string>()))
                .Returns(0.6d);
            analyticWeeklyReportUtils.Setup(p => p.PhraseAvgPerDay(It.IsAny<List<VWeeklyUserReport>>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(new Dictionary<DateTime, double>
                {
                    {DateTime.Now.AddDays(-2), 0.5d}
                });

            //Act
            var result = analyticWeeklyReportService.User(
                userId,
                TestData.beg,
                TestData.end);
            System.Console.WriteLine($"result:\n{JsonConvert.SerializeObject(result)}");

            //Assert
            Assert.IsFalse(result is null);
        }
    }
}