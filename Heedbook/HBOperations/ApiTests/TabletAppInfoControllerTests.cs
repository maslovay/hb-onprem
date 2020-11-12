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
using Newtonsoft.Json.Linq;
using UserOperations.Models.Session;
using UserOperations.Models.Post;

namespace ApiTests
{
    public class TabletAppInfoControllerTests : ApiServiceTest
    {
        private TabletAppInfoService tabletAppInfoService;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            tabletAppInfoService = new TabletAppInfoService(
                moqILoginService.Object,
                repositoryMock.Object);
        }
        [Test]
        public async Task AddCurrentTabletAppVersionGetTest()
        {
            //Arrange
            repositoryMock.Setup(p => p.GetAsQueryable<TabletAppInfo>())
                .Returns(new TestAsyncEnumerable<TabletAppInfo>(new List<TabletAppInfo>
                {
                    new TabletAppInfo
                    {
                        TabletAppVersion = "version 1",
                        ReleaseDate = DateTime.Now.AddDays(-20)
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.CreateAsync<TabletAppInfo>(It.IsAny<TabletAppInfo>()));
            repositoryMock.Setup(p => p.Save());

            //Act
            var result = (TabletAppInfo)tabletAppInfoService.AddCurrentTabletAppVersion("version 2");

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task GetCurrentTabletAppVersionGetTest()
        {
            //Arrange
            repositoryMock.Setup(p => p.GetAsQueryable<TabletAppInfo>())
                .Returns(new TestAsyncEnumerable<TabletAppInfo>(new List<TabletAppInfo>
                {
                    new TabletAppInfo
                    {
                        TabletAppVersion = "version 1",
                        ReleaseDate = DateTime.Now.AddDays(-20)
                    }
                }.AsQueryable()));

            //Act
            var result = (TabletAppInfo)tabletAppInfoService.GetCurrentTabletAppVersion();

            //Assert
            Assert.IsFalse(result is null);
        }
    }
}