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

namespace ApiTests
{
    [TestFixture]
    public class AnalyticContentControllerTest : ApiServiceTest
    {
        [Test]
        public async Task GetContent_CallAll()
        {
            string token = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhbm5zYW1vbHVrX3Rlc3RAZ21haWwuY29tIiwianRpIjoiZmY3Yjc4NGQtMTEzMi00ZmY0LThlN2ItODU4YTBhMDVhMzE3IiwiYXBwbGljYXRpb25Vc2VySWQiOiJhNmI2NjgzNS1hNDEyLTRjMjAtODBiNy0yZGNhN2VhZTRjZDYiLCJhcHBsaWNhdGlvblVzZXJOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlOYW1lIjoiYW5uc2Ftb2x1a190ZXN0QGdtYWlsLmNvbSIsImNvbXBhbnlJZCI6ImJkNGM0MmIwLWRlNmEtNDkxNS1hYzY5LWE1ZjAzNjAxOWM5ZCIsImNvcnBvcmF0aW9uSWQiOiIiLCJsYW5ndWFnZUNvZGUiOiIxIiwicm9sZSI6Ik1hbmFnZXIiLCJmdWxsTmFtZSI6ImFubnNhbW9sdWtfdGVzdEBnbWFpbC5jb20iLCJhdmF0YXIiOiIiLCJleHAiOjE1NzQ5MzQxNDUsImlzcyI6Imh0dHBzOi8vaGVlZGJvb2suY29tIiwiYXVkIjoiaHR0cHM6Ly9oZWVkYm9vay5jb20ifQ.rRRAcst-r0mD4jkn80L8yKLf9xGPhGxVaNy0tRgKUXM";

            //arrange
            var filterMock = new Mock<IRequestFilters>();
            var login = new Mock<ILoginService>();
            var contentProvider = new Mock<IAnalyticContentProvider>();

            Task<Dialogue> dialogue = new Task<Dialogue>(() => { return new Dialogue(); });
            Guid guid = new Guid();
            contentProvider.Setup(log => log.GetDialogueIncludedFramesByIdAsync(It.IsAny<Guid>()));//.Returns(dialogue);

            Dictionary<string, string> tokenclaims = new Dictionary<string, string>();
            login.Setup(log => log.GetDataFromToken(It.IsAny<string>(), out It.Ref<Dictionary<string, string>>.IsAny, null)).Returns(true);


            var controller = new AnalyticContentController(contentProvider.Object, login.Object, filterMock.Object);
            // Act
            await controller.ContentShows(guid, token);

            // Assert
            login.Verify(log => log.GetDataFromToken(token, out tokenclaims, null), Times.Once());
            contentProvider.Verify(log => log.GetDialogueIncludedFramesByIdAsync(guid), Times.Once());
        }

        [SetUp]
        public void Setup()
        {
            // base.Setup(() => { }, true);
            base.Setup();
        }

    }
}
