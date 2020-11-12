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
    public class AnalyticSpeechControllerTests : ApiServiceTest
    {   
        private AnalyticSpeechService analyticReportService;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            analyticReportService = new AnalyticSpeechService(
                moqILoginService.Object,
                requestFiltersMock.Object,
                repositoryMock.Object,
                analyticSpeechUtils.Object
            );
        }
        [Test]
        public async Task SpeechEmployeeRatingGetTest()
        {
            //Arrange
            var applicationUserId = Guid.NewGuid();
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
            var companyIds = new List<Guid>{companyId};
            requestFiltersMock.Setup(p => p.CheckRolesAndChangeCompaniesInFilter(ref companyIds, It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<Guid>()));
            repositoryMock.Setup(p => p.GetAsQueryable<PhraseType>())
                .Returns(new TestAsyncEnumerable<PhraseType>(new List<PhraseType>
                {
                    new PhraseType()
                    {
                        PhraseTypeId = typeIdCross,
                        PhraseTypeText = "Cross"
                    },
                    new PhraseType()
                    {
                        PhraseTypeId = typeIdAlert,
                        PhraseTypeText = "Alert"
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
                        DialoguePhrase = new List<DialoguePhrase>
                        {
                            new DialoguePhrase
                            {
                                PhraseTypeId = typeIdCross
                            },
                            new DialoguePhrase
                            {
                                PhraseTypeId = typeIdAlert
                            }
                        },
                        DialogueClientSatisfaction = new List<DialogueClientSatisfaction>
                        {
                            new DialogueClientSatisfaction()
                            {
                                MeetingExpectationsTotal = 0.6d
                            }
                        }
                    }
                }.AsQueryable()));
            analyticSpeechUtils.Setup(p => p.CrossIndex(It.IsAny<IGrouping<Guid?, DialogueInfo>>()))
                .Returns(0.5d);
            analyticSpeechUtils.Setup(p => p.AlertIndex(It.IsAny<IGrouping<Guid?, DialogueInfo>>()))
                .Returns(0.5d);

            //Act
            var result = analyticReportService.SpeechEmployeeRating(
                TestData.beg,
                TestData.end,
                new List<Guid?>{applicationUserId},
                new List<Guid>{companyId},
                new List<Guid>{corporationId},
                new List<Guid>{deviceId});
            System.Console.WriteLine($"result: {result}");
            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task SpeechPhraseTableGetTest()
        {
            //Arrange
            var applicationUserId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            var phraseTypeId = Guid.NewGuid();
            var dialogueId = Guid.NewGuid();
            var phraseId = Guid.NewGuid();
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
            repositoryMock.Setup(p => p.GetAsQueryable<Dialogue>())
                .Returns(new TestAsyncEnumerable<Dialogue>(new List<Dialogue>
                {
                    new Dialogue()
                    {
                        DialogueId = dialogueId,
                        BegTime = DateTime.Now.AddMinutes(-3),
                        EndTime = DateTime.Now.AddMinutes(-1),
                        StatusId = 3,
                        InStatistic = true,
                        DeviceId = deviceId,
                        Device = new Device(){DeviceId = deviceId, CompanyId = companyId},
                        ApplicationUserId = applicationUserId,
                        ApplicationUser = new ApplicationUser{FullName = "TestUser", UserRoles = new List<ApplicationUserRole>{new ApplicationUserRole{RoleId = roleId}}},
                        DialoguePhrase = new List<DialoguePhrase>
                        {
                            new DialoguePhrase
                            {
                                PhraseTypeId = phraseTypeId
                            }
                        }
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.GetAsQueryable<DialoguePhrase>())
                .Returns(new TestAsyncEnumerable<DialoguePhrase>(new List<DialoguePhrase>
                {
                    new DialoguePhrase
                    {
                        DialogueId = dialogueId,
                        PhraseId = phraseId,
                        PhraseTypeId = phraseTypeId,
                        IsClient = true,
                        Dialogue = new Dialogue
                        {
                            ApplicationUserId = applicationUserId,
                            ApplicationUser = new ApplicationUser
                            {
                                FullName = "TestUser" 
                            }
                        },
                        Phrase = new Phrase
                        {
                            PhraseId = phraseId,
                            PhraseText = "Добрый день",
                            PhraseTypeId = phraseTypeId,
                            PhraseType = new PhraseType
                            {
                                PhraseTypeText = "Cross"
                            }
                        }
                    }
                }.AsQueryable()));
            
            //Act
            var result = analyticReportService.SpeechPhraseTable(
                TestData.beg,
                TestData.end,
                new List<Guid?>{applicationUserId},
                new List<Guid>{companyId},
                new List<Guid>{corporationId},
                new List<Guid>{deviceId},
                new List<Guid>{phraseId},
                new List<Guid>{phraseTypeId});

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task SpeechPhraseTypeCountGetTest()
        {
            //Arrange
            var applicationUserId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            var phraseTypeId = Guid.NewGuid();
            var dialogueId = Guid.NewGuid();
            var phraseId = Guid.NewGuid();
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
            repositoryMock.Setup(p => p.GetAsQueryable<Dialogue>())
                .Returns(new TestAsyncEnumerable<Dialogue>(new List<Dialogue>
                {
                    new Dialogue()
                    {
                        DialogueId = dialogueId,
                        BegTime = DateTime.Now.AddMinutes(-3),
                        EndTime = DateTime.Now.AddMinutes(-1),
                        StatusId = 3,
                        InStatistic = true,
                        DeviceId = deviceId,
                        Device = new Device(){DeviceId = deviceId, CompanyId = companyId},
                        ApplicationUserId = applicationUserId                        
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.GetAsQueryable<DialoguePhrase>())
                .Returns(new TestAsyncEnumerable<DialoguePhrase>(new List<DialoguePhrase>
                {
                    new DialoguePhrase
                    {
                        DialogueId = dialogueId,
                        PhraseId = phraseId,
                        PhraseTypeId = phraseTypeId,
                        IsClient = true,
                        Phrase = new Phrase
                        {
                            PhraseId = phraseId,
                            PhraseText = "Добрый день",
                            PhraseTypeId = phraseTypeId,
                            PhraseType = new PhraseType
                            {
                                PhraseTypeText = "Cross",
                                Colour = "Red"
                            }
                        }
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.GetAsQueryable<PhraseType>())
                .Returns(new TestAsyncEnumerable<PhraseType>(new List<PhraseType>
                {
                    new PhraseType()
                    {
                        PhraseTypeText = "Cross"
                    }
                }.AsQueryable()));

            //Act
            var result = analyticReportService.SpeechPhraseTypeCount(
                TestData.beg,
                TestData.end,
                new List<Guid?>{applicationUserId},
                new List<Guid>{companyId},
                new List<Guid>{corporationId},
                new List<Guid>{deviceId},
                new List<Guid>{phraseId},
                new List<Guid>{phraseTypeId});

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task SpeechWordCloudGetTest()
        {

            //Arrange
            var applicationUserId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            var phraseTypeId = Guid.NewGuid();
            var dialogueId = Guid.NewGuid();
            var phraseId = Guid.NewGuid();
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
            repositoryMock.Setup(p => p.GetAsQueryable<Dialogue>())
                .Returns(new TestAsyncEnumerable<Dialogue>(new List<Dialogue>
                {
                    new Dialogue()
                    {
                        DialogueId = dialogueId,
                        BegTime = DateTime.Now.AddMinutes(-3),
                        EndTime = DateTime.Now.AddMinutes(-1),
                        StatusId = 3,
                        InStatistic = true,
                        DeviceId = deviceId,
                        Device = new Device(){DeviceId = deviceId, CompanyId = companyId},
                        ApplicationUserId = applicationUserId                        
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.GetAsQueryable<DialoguePhrase>())
                .Returns(new TestAsyncEnumerable<DialoguePhrase>(new List<DialoguePhrase>
                {
                    new DialoguePhrase
                    {
                        DialogueId = dialogueId,
                        PhraseId = phraseId,
                        PhraseTypeId = phraseTypeId,
                        IsClient = true,
                        Phrase = new Phrase
                        {
                            PhraseId = phraseId,
                            PhraseText = "Добрый день",
                            PhraseTypeId = phraseTypeId,
                            PhraseType = new PhraseType
                            {
                                PhraseTypeText = "Cross",
                                Colour = "Red"
                            }
                        }
                    }
                }.AsQueryable()));

            //Act
            var result = analyticReportService.SpeechWordCloud(
                TestData.beg,
                TestData.end,
                new List<Guid?>{applicationUserId},
                new List<Guid>{companyId},
                new List<Guid>{corporationId},
                new List<Guid>{deviceId},
                new List<Guid>{phraseId},
                new List<Guid>{phraseTypeId});

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task PhraseSalesStageCountGetTest()
        {
            //Arrange
            var applicationUserId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var corporationId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            var phraseTypeId = Guid.NewGuid();
            var dialogueId = Guid.NewGuid();
            var phraseId = Guid.NewGuid();
            var salesStageId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentRoleName())
                .Returns("Supervisor");
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            requestFiltersMock.Setup(p => p.GetBegDate(It.IsAny<string>()))
                .Returns(DateTime.Now.AddDays(-6));
            requestFiltersMock.Setup(p => p.GetEndDate(It.IsAny<string>()))
                .Returns(DateTime.Now);
            repositoryMock.Setup(p => p.GetAsQueryable<Company>())
                .Returns(new TestAsyncEnumerable<Company>(new List<Company>
                {
                    new Company
                    {
                        CompanyId = companyId,
                        CorporationId = corporationId
                    }
                }));
            repositoryMock.Setup(p => p.GetAsQueryable<Corporation>())
                .Returns(new TestAsyncEnumerable<Corporation>(new List<Corporation>
                {
                    new Corporation
                    {
                        Id = corporationId,
                        Companies = new List<Company>{new Company{CompanyId = companyId}}
                    }
                }));
            repositoryMock.Setup(p => p.GetAsQueryable<DialoguePhrase>())
                .Returns(new TestAsyncEnumerable<DialoguePhrase>(new List<DialoguePhrase>
                {
                    new DialoguePhrase
                    {
                        DialogueId = dialogueId,
                        PhraseId = phraseId,
                        PhraseTypeId = phraseTypeId,
                        IsClient = true,
                        Phrase = new Phrase
                        {
                            PhraseId = phraseId,
                            PhraseText = "Добрый день",
                            PhraseTypeId = phraseTypeId,
                            PhraseType = new PhraseType
                            {
                                PhraseTypeText = "Cross",
                                Colour = "Red"
                            }
                        }
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.GetAsQueryable<SalesStagePhrase>())
                .Returns(new TestAsyncEnumerable<SalesStagePhrase>(new List<SalesStagePhrase>
                {
                    new SalesStagePhrase
                    {
                        SalesStageId = salesStageId,
                        CompanyId = companyId,
                        CorporationId = corporationId,
                        SalesStage = new SalesStage
                        {
                            Name = "Предпродажи",   
                            SequenceNumber = 7
                        }
                    }
                }.AsQueryable()));

            //Act
            var result = analyticReportService.PhraseSalesStageCount(
                TestData.beg,
                TestData.end,
                corporationId,
                new List<Guid>{companyId},
                new List<Guid?>{applicationUserId},
                new List<Guid>{deviceId},
                new List<Guid>{phraseId},
                new List<Guid>{salesStageId});
            var salesStagePhraseModels = JsonConvert.DeserializeObject<List<SalesStagePhraseModel>>(result);

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(salesStagePhraseModels.Count > 0);
        }
    }
}