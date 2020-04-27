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

namespace ApiTests
{
    public class AnalyticServiceQualityTests : ApiServiceTest
    {   
        private AnalyticServiceQualityService analyticReportService;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            analyticReportService = new AnalyticServiceQualityService(
                moqILoginService.Object,
                requestFiltersMock.Object,
                repositoryMock.Object,
                analyticServiceQualityUtils.Object
            );
        }
        [Test]
        public async Task ServiceQualityComponentsGetTest()
        {
            //Arrange
            var applicationUserId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var loyaltyPhraseId = Guid.NewGuid();
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
            repositoryMock.Setup(p => p.GetAsQueryable<PhraseType>())
                .Returns(new TestAsyncEnumerable<PhraseType>(new List<PhraseType>
                {
                    new PhraseType()
                    {
                        PhraseTypeId = loyaltyPhraseId,
                        PhraseTypeText = "Loyalty",
                        Colour = "Green"
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
                        SlideShowSessions = new List<SlideShowSession>{new SlideShowSession{}},
                        DialogueAudio = new List<DialogueAudio>{new DialogueAudio
                        {
                            PositiveTone = 0.4d,
                            NegativeTone = 0.6d,
                            NeutralityTone = 0.3d
                        }},
                        DialogueSpeech = new List<DialogueSpeech>{new DialogueSpeech
                        {
                            PositiveShare = 0.7d
                        }},
                        DialogueVisual = new List<DialogueVisual>{new DialogueVisual
                        {
                            HappinessShare = 0.9d,
                            NeutralShare = 0.4d,
                            SurpriseShare = 0.3d,
                            SadnessShare = 0.4d,
                            AngerShare = 0.1d,
                            DisgustShare = 0.6d,
                            ContemptShare = 0.1d,
                            FearShare = 0.4d,
                            AttentionShare = 0.6d
                        }},
                        DialoguePhraseCount = new List<DialoguePhraseCount>{new DialoguePhraseCount
                        {
                            PhraseTypeId = loyaltyPhraseId,
                            PhraseCount = 25
                        }}
                    }
                }.AsQueryable()));
            analyticServiceQualityUtils.Setup(p => p.LoyaltyIndex(It.IsAny<List<ComponentsDialogueInfo>>()))
                .Returns(0.6d);
            
            //Act
            var result = await analyticReportService.ServiceQualityComponents(
                TestData.beg,
                TestData.end,
                new List<Guid?>{applicationUserId},
                new List<Guid>{companyId},
                new List<Guid>{corporationId},
                new List<Guid>{deviceId}
            );

            //Assert
            Assert.NotNull(result);

        }
        [Test]
        public async Task ServiceQualityDashboardGetTest()
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
            moqILoginService.Setup(p => p.GetIsExtended())
                .Returns(true);

            requestFiltersMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
                .Returns(DateTime.Now.AddDays(-6));
            requestFiltersMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
                .Returns(DateTime.Now);
            var companyIds = new List<Guid>{companyId};
            requestFiltersMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter(ref companyIds, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));
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
                        DialogueClientSatisfaction = new List<DialogueClientSatisfaction>{new DialogueClientSatisfaction()
                        {
                            MeetingExpectationsTotal = 0.7d,
                            BegMoodByNN = 0.77d,
                            EndMoodByNN = 0.8d
                        }},
                        DialogueFrame = new List<DialogueFrame>{new DialogueFrame
                        {
                            HappinessShare = 0.6d
                        }}
                    }
                }.AsQueryable()));
            analyticServiceQualityUtils.Setup(p => p.SatisfactionIndex(It.IsAny<List<DialogueInfo>>()))
                .Returns(0.7d);
            analyticServiceQualityUtils.Setup(p => p.DialoguesCount(It.IsAny<List<DialogueInfo>>(), It.IsAny<Guid>(), It.IsAny<DateTime>()))
                .Returns(5);
            analyticServiceQualityUtils.Setup(p => p.BestEmployee(It.IsAny<List<DialogueInfo>>()))
                .Returns("TestUser");
            analyticServiceQualityUtils.Setup(p => p.BestEmployeeSatisfaction(It.IsAny<List<DialogueInfo>>()))
                .Returns(0.7d);
            analyticServiceQualityUtils.Setup(p => p.BestProgressiveEmployee(It.IsAny<List<DialogueInfo>>(), It.IsAny<DateTime>()))
                .Returns("TestUser");
            analyticServiceQualityUtils.Setup(p => p.BestProgressiveEmployeeDelta(It.IsAny<List<DialogueInfo>>(), It.IsAny<DateTime>()))
                .Returns(0.7d);

            //Act
            var result = analyticReportService.ServiceQualityDashboard(
                TestData.beg,
                TestData.end,
                new List<Guid?>{applicationUserId},
                new List<Guid>{companyId},
                new List<Guid>{corporationId},
                new List<Guid>{deviceId}
            );
            System.Console.WriteLine($"result: \n{result}");

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task ServiceQualityRatingGetTest()
        {
            //Arrange
            var applicationUserId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            var phraseTypeId = Guid.NewGuid();
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
            repositoryMock.Setup(p => p.GetAsQueryable<PhraseType>())
                .Returns(new TestAsyncEnumerable<PhraseType>(new List<PhraseType>
                {
                    new PhraseType()
                    {
                        PhraseTypeId = phraseTypeId,
                        PhraseTypeText = "Loyalty"
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
                        DialoguePhrase = new List<DialoguePhrase>{new DialoguePhrase{PhraseTypeId = phraseTypeId}},
                        DialogueClientSatisfaction = new List<DialogueClientSatisfaction>{new DialogueClientSatisfaction()
                        {
                            MeetingExpectationsTotal = 0.7d,
                            BegMoodByNN = 0.77d,
                            EndMoodByNN = 0.8d
                        }},
                        DialogueFrame = new List<DialogueFrame>{new DialogueFrame
                        {
                            HappinessShare = 0.6d
                        }},
                        DialogueAudio = new List<DialogueAudio>
                        {
                            new DialogueAudio{PositiveTone = 0.6d}
                        },
                        DialogueVisual = new List<DialogueVisual>
                        {
                            new DialogueVisual
                            {
                                AttentionShare = 0.6d,
                                SurpriseShare = 0.7d,
                                HappinessShare = 0.5d
                            }
                        },
                        DialogueSpeech = new List<DialogueSpeech>
                        {
                            new DialogueSpeech
                            {
                                PositiveShare = 0.44d
                            }
                        }

                    }
                }.AsQueryable()));
            analyticServiceQualityUtils.Setup(p => p.LoyaltyIndex(It.IsAny<IGrouping<Guid?, RatingDialogueInfo>>()))
                .Returns(0.3d);

            //Act
            var result = await analyticReportService.ServiceQualityRating(
                TestData.beg,
                TestData.end,
                new List<Guid?>{applicationUserId},
                new List<Guid>{companyId},
                new List<Guid>{corporationId},
                new List<Guid>{deviceId}
            );
            System.Console.WriteLine($"result: \n{result}");
            var listRatingRatingInfo = JsonConvert.DeserializeObject<List<RatingRatingInfo>>(result);

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(listRatingRatingInfo.Count > 0);
        }
        [Test]
        public async Task ServiceQualitySatisfactionStats()
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
            moqILoginService.Setup(p => p.GetIsExtended())
                .Returns(true);
            requestFiltersMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
                .Returns(DateTime.Now.AddDays(-6));
            requestFiltersMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
                .Returns(DateTime.Now);
            var companyIds = new List<Guid>{companyId};
            requestFiltersMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter(ref companyIds, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));
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
                        DialogueClientSatisfaction = new List<DialogueClientSatisfaction>{new DialogueClientSatisfaction()
                        {
                            MeetingExpectationsTotal = 0.7d
                        }},
                        DialogueFrame = new List<DialogueFrame>{new DialogueFrame
                        {
                            HappinessShare = 0.6d
                        }}
                    }
                }.AsQueryable()));


            //Act
            var result = await analyticReportService.ServiceQualitySatisfactionStats(
                TestData.beg,
                TestData.end,
                new List<Guid?>{applicationUserId},
                new List<Guid>{companyId},
                new List<Guid>{corporationId},
                new List<Guid>{deviceId}
            );
            System.Console.WriteLine($"result: \n{result}");
            var listRatingRatingInfo = JsonConvert.DeserializeObject<SatisfactionStatsInfo>(result);

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsFalse(listRatingRatingInfo is null);
        }
    }
}