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
    public class DemonstrationV2ControllerTests : ApiServiceTest
    {
        private DemonstrationV2Service demonstrationV2Service;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            demonstrationV2Service = new DemonstrationV2Service(
                repositoryMock.Object,
                moqILoginService.Object);
        }
        [Test]
        public async Task FlushStatsPostTest()
        {
            //Arrange
            var campaignContentId = Guid.NewGuid();
            repositoryMock.Setup(p => p.GetAsQueryable<CampaignContent>())
                .Returns(new TestAsyncEnumerable<CampaignContent>(new List<CampaignContent>
                {
                    new CampaignContent
                    {
                        CampaignContentId = campaignContentId,
                        Content = new Content
                        {
                            JSONData = "testData",
                        }
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.CreateAsync<SlideShowSession>(It.IsAny<SlideShowSession>()))
                .Returns(Task.FromResult(0));
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));
            var slideShowSessions = new List<SlideShowSession>
            {
                new SlideShowSession
                {
                    ContentType = "url",
                    IsPoll = true,
                    CampaignContentId = campaignContentId
                }
            };

            //Act
            await demonstrationV2Service.FlushStats(slideShowSessions);

            //Assert
            Assert.IsTrue(true);
        }
        [Test]
        public async Task PollAnswerPostTest()
        {
            //Arrange
            repositoryMock.Setup(p => p.CreateAsync<CampaignContentAnswer>(It.IsAny<CampaignContentAnswer>()))
                .Returns(Task.FromResult(0));
            repositoryMock.Setup(p => p.SaveAsync())
                .Returns(Task.FromResult(0));
            var campaignContentId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var model = new CampaignContentAnswerModel
            {
                Answer = "Answer",
                AnswerText = "AnswerText",
                CampaignContentId = campaignContentId,
                DeviceId = deviceId,
                ApplicationUserId = userId,
                Time = DateTime.Now
            };

            //Act
            var result = await demonstrationV2Service.PollAnswer(model);

            //Assert
            Assert.IsTrue(result == "Saved");
        }
    }
}