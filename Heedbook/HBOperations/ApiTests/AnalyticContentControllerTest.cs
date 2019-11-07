using System;
using System.Threading.Tasks;
using System.Linq;
using HBData.Models;
using NUnit.Framework;
using Moq;
using UserOperations.Controllers;
using UserOperations.Providers;
using System.Collections.Generic;
using UserOperations.Models.AnalyticModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ApiTests
{
    [TestFixture]
    public class AnalyticContentControllerTest : ApiServiceTest
    {
        protected Mock<IAnalyticContentProvider> analyticContentProviderMock;
        [SetUp]
        public void Setup()
        {
            base.Setup();
        }
        protected override void InitServices()
        {
            base.moqILoginService = MockILoginService(base.moqILoginService);
            base.accountProviderMock = MockIAccountProvider(base.accountProviderMock);
            analyticContentProviderMock = MockIAnalyticContentProvider(new Mock<IAnalyticContentProvider>());
        }
    

        [Test]
        public async Task ContentShows()
        {
            //Arrange
            commonProviderMock.Setup(p => p.GetDialogueIncludedFramesByIdAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Dialogue(){DialogueFrame = new List<DialogueFrame>(){new DialogueFrame()}}));

            var analyticContentController = new AnalyticContentController(
                analyticContentProviderMock.Object,
                commonProviderMock.Object,
                helpProvider.Object,
                moqILoginService.Object,
                filterMock.Object
            );

            //Act
            var task = analyticContentController.ContentShows(Guid.Parse("5c35b338-6695-4fc0-8145-c655c365f969"), "Token");
            task.Wait();
            var okResult = task.Result as OkObjectResult;
            System.Console.WriteLine(JsonConvert.SerializeObject(okResult));
            var dictionary = okResult.Value as Dictionary<string, object>;
            var result = okResult.Value;

            //Assert
            Assert.IsNotNull(result);
            Assert.NotZero(dictionary.Count);
            //Assert.NotZero(dictionary.Count);
        }
        [Test]
        public async Task Efficiency()
        {
            //Arrange
            commonProviderMock.Setup(p => p.GetDialoguesInfoWithFramesAsync(
                It.IsAny<DateTime>(), 
                It.IsAny<DateTime>(), 
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>()))
                .Returns(Task.FromResult( TestData.GetDialogueInfoWithFrames().ToList()));
                
            var analyticContentController = new AnalyticContentController(
                analyticContentProviderMock.Object,
                commonProviderMock.Object,
                helpProvider.Object,
                moqILoginService.Object,
                filterMock.Object
            );

            //Act
            var task = analyticContentController.Efficiency(
                TestData.beg, TestData.end,
                TestData.GetGuids(), TestData.GetGuids(), TestData.GetGuids(), TestData.GetGuids(),
                TestData.token
            );
            task.Wait();
            System.Console.WriteLine($"taskResult: {JsonConvert.SerializeObject(task.Result)}");
            var okResult = task.Result as OkObjectResult;
            var dictionary = okResult.Value as Dictionary<string, object>;
            var result = okResult.Value;     

            //Assert
            Assert.IsNotNull(result);     
            Assert.NotZero(dictionary.Count);       
        }
        [Test]
        public async Task Poll()
        {
            //Aaarnge
            commonProviderMock.Setup(p => p.GetDialoguesInfoWithFramesAsync(
                It.IsAny<DateTime>(), 
                It.IsAny<DateTime>(), 
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>(),
                It.IsAny<List<Guid>>()))
                .Returns(Task.FromResult(new List<DialogueInfoWithFrames>()
                {
                    new DialogueInfoWithFrames(){BegTime = new DateTime(), EndTime = new DateTime()},
                    new DialogueInfoWithFrames(){BegTime = new DateTime(), EndTime = new DateTime()},
                    new DialogueInfoWithFrames(){BegTime = new DateTime(), EndTime = new DateTime()}
                }));
                
            var analyticContentController = new AnalyticContentController(
                analyticContentProviderMock.Object,
                commonProviderMock.Object,
                helpProvider.Object,
                moqILoginService.Object,
                filterMock.Object
            );

            //Act
            var task = analyticContentController.Poll(
                TestData.beg, TestData.end,
                TestData.GetGuids(), TestData.GetGuids(), TestData.GetGuids(), TestData.GetGuids(),
                TestData.token,
                "json"
            );
            task.Wait();

            //Assert
            var okResult = task.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
        }

        private Mock<IAnalyticContentProvider> MockIAnalyticContentProvider(Mock<IAnalyticContentProvider> moqIAnalyticContentProvider)
        {
            moqIAnalyticContentProvider.Setup(p => p.GetSlideShowsForOneDialogueAsync(It.IsAny<Dialogue>()))
                .Returns(Task.FromResult( TestData.GetSlideShowInfosSimple() ));

            moqIAnalyticContentProvider.Setup(p => p.GetSlideShowFilteredByPoolAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<bool>()))
                .Returns(Task.FromResult(TestData.GetSlideShowInfosSimple()));

            moqIAnalyticContentProvider.Setup(p => p.GetAnswersInOneDialogueAsync(
                    It.IsAny<List<SlideShowInfo>>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<Guid>()))
                .Returns(Task.FromResult(new List<CampaignContentAnswer>() { }));
            moqIAnalyticContentProvider.Setup(p => p.GetAnswersForOneContent(
                    It.IsAny<List<AnswerInfo.AnswerOne>>(),
                    It.IsAny<Guid?>()))
                .Returns(new List<AnswerInfo.AnswerOne>() { });
            moqIAnalyticContentProvider.Setup(p => p.GetConversion(
                    It.IsAny<double>(),
                    It.IsAny<double>()))
                .Returns(0.5d);
            moqIAnalyticContentProvider.Setup(p => p.GetAnswersFullAsync(
                    It.IsAny<List<SlideShowInfo>>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<List<Guid>>()))
                .Returns(Task.FromResult(new List<AnswerInfo.AnswerOne>() { }));
            moqIAnalyticContentProvider.Setup(p => p.AddDialogueIdToShow(It.IsAny<List<SlideShowInfo>>(), It.IsAny<List<DialogueInfoWithFrames>>()))
                .Returns(new List<SlideShowInfo>() { });
            moqIAnalyticContentProvider.Setup(p => p.EmotionsDuringAdv(It.IsAny<List<SlideShowInfo>>(), It.IsAny<List<DialogueInfoWithFrames>>()))
                .Returns(new EmotionAttention() { Positive = 0.3d, Negative = 0.3d, Neutral = 0.3d, Attention = 0.3d });
            moqIAnalyticContentProvider.Setup(p => p.EmotionDuringAdvOneDialogue(It.IsAny<List<SlideShowInfo>>(), It.IsAny<List<DialogueFrame>>()))
                .Returns(new EmotionAttention() { Positive = 0.3d, Negative = 0.3d, Neutral = 0.3d, Attention = 0.3d });
            return moqIAnalyticContentProvider;
        }

    }
}
