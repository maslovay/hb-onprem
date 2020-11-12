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
using UserOperations.Models;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http.Internal;
using System.Linq.Expressions;
using UserOperations.Controllers;

namespace ApiTests
{
    public class CompanyReportControllerTests : ApiServiceTest
    {
        private CompanyReportController companyReportController;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            companyReportController = new CompanyReportController(
                moqILoginService.Object,
                repositoryMock.Object);
        }
        [Test]
        public async Task GetReportGetTest()
        {
            //Arrange
            var dialogueId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentUserId())
                .Returns(userId);
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
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
                        Language = new Language
                        {
                            LanguageName = "NewTestlanguage"
                        },
                        Device = new Device
                        {
                            DeviceId = deviceId, 
                            CompanyId = companyId
                        },
                        ApplicationUserId = userId,
                        ApplicationUser = new ApplicationUser
                        {
                            Id = userId,
                            CompanyId = companyId,
                            Company = new Company
                            {
                                CompanyId = companyId,
                                CompanyName = "TestCompany"
                            }
                        },
                        DialoguePhrase = new List<DialoguePhrase>
                        {
                            new DialoguePhrase
                            {
                                Phrase = new Phrase
                                {
                                    PhraseText = "TestPhrase"
                                }
                            }
                        },
                        DialogueClientSatisfaction = new List<DialogueClientSatisfaction>
                        {
                            new DialogueClientSatisfaction
                            {
                                MeetingExpectationsTotal = 0.7d,
                                BegMoodTotal = 0.8d,
                                EndMoodTotal = 0.6d,
                                MeetingExpectationsByClient = 0.5d,
                                MeetingExpectationsByEmpoyee = 0.4d,

                            }
                        },
                        DialogueFrame = new List<DialogueFrame>{new DialogueFrame{}},
                        SlideShowSessions = new List<SlideShowSession>{new SlideShowSession{}},
                        DialogueHint = new List<DialogueHint>
                        {
                            new DialogueHint
                            {
                                HintText = "TestHint"
                            }
                        },
                        DialogueClientProfile = new List<DialogueClientProfile>
                        {
                            new DialogueClientProfile
                            {
                                Age = 50,
                                Gender = "male",
                                Avatar = "TestAvatar"
                            }
                        },
                        DialogueVisual = new List<DialogueVisual>
                        {
                            new DialogueVisual
                            {
                                AttentionShare = 0.7d,
                                ContemptShare = 0.8d,
                                DisgustShare = 0.6d,
                                FearShare = 0.5d,
                                HappinessShare = 0.4d,
                                NeutralShare = 0.3d,
                                SurpriseShare = 0.2d,
                                SadnessShare = 0.6d,
                                AngerShare = 0.5d                                
                            }
                        },
                        DialogueAudio = new List<DialogueAudio>
                        {
                            new DialogueAudio
                            {
                                NegativeTone = 0.5d,
                                NeutralityTone = 0.6d,
                                PositiveTone = 0.7d 
                            }
                        },
                        DialogueSpeech = new List<DialogueSpeech>
                        {
                            new DialogueSpeech
                            {
                                PositiveShare = 0.5d,
                                SilenceShare = 0.4d,
                                SpeechSpeed = 0.7d
                            }
                        },
                        DialogueWord = new List<DialogueWord>
                        {
                            new DialogueWord
                            {
                                Words = "TestWord"
                            }
                        }
                    }
                }.AsQueryable()));

            //Act
            var fileResult = companyReportController.GetReport(
                DateTime.Now.AddDays(-2).ToString($"yyyyMMddHHmmss"),
                new List<Guid>
                {
                    companyId
                });

            //Assert
            Assert.IsFalse(fileResult is null);
        }
    }
}