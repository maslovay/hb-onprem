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
using UserOperations.Providers;
using System.Collections.Generic;
using UserOperations.Models.AnalyticModels;
using UserOperations.Providers.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ApiTests
{
    [TestFixture]
    public class AnalyticContentControllerTest : ApiServiceTest
    {
        protected MockInterfaceProviders mockProvider;
        protected Mock<IAnalyticContentProvider> analyticContentProviderMock;
        protected Mock<IHelpProvider> helpProvider;
        [SetUp]
        public void Setup()
        {
            mockProvider = new MockInterfaceProviders();
            analyticContentProviderMock = new Mock<IAnalyticContentProvider>();
            helpProvider = new Mock<IHelpProvider>();
            base.Setup();
        }
        [Test]
        public async Task ContentShows()
        {
            //Arrange
            loginMock = mockProvider.MockILoginService(loginMock);

            filterMock = mockProvider.MockIRequestFiltersProvider(filterMock);

            analyticContentProviderMock = mockProvider.MockIAnalyticContentProvider(analyticContentProviderMock);

            commonProviderMock.Setup(p => p.GetDialogueIncludedFramesByIdAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new Dialogue(){DialogueFrame = new List<DialogueFrame>(){new DialogueFrame()}}));

            var analyticContentController = new AnalyticContentController(
                analyticContentProviderMock.Object,
                commonProviderMock.Object,
                helpProvider.Object,
                loginMock.Object,
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
            loginMock = mockProvider.MockILoginService(loginMock);

            filterMock = mockProvider.MockIRequestFiltersProvider(filterMock);

            analyticContentProviderMock = mockProvider.MockIAnalyticContentProvider(analyticContentProviderMock);

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
                loginMock.Object,
                filterMock.Object
            );

            //Act
            var task = analyticContentController.Efficiency(
                "20191105",
                "20191106",
                new List<Guid>(){Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()},
                new List<Guid>(){Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()},
                new List<Guid>(){Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()},
                new List<Guid>(){Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()},
                "Bearer Token"
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
            loginMock = mockProvider.MockILoginService(loginMock);

            filterMock = mockProvider.MockIRequestFiltersProvider(filterMock);

            analyticContentProviderMock = mockProvider.MockIAnalyticContentProvider(analyticContentProviderMock);

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
                loginMock.Object,
                filterMock.Object
            );

            var task = analyticContentController.Poll(
                "20191105",
                "20191106",
                new List<Guid>(){Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()},
                new List<Guid>(){Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()},
                new List<Guid>(){Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()},
                new List<Guid>(){Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()},
                "Bearer Token",
                "json"
            );
            task.Wait();
            var okResult = task.Result as OkObjectResult;

            //Act

            //Assert
            Assert.IsNotNull(okResult);
        }
        

    }
}
