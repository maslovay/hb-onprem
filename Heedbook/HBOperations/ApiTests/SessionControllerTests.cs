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

namespace ApiTests
{
    public class SessionControllerTests : ApiServiceTest
    {
        private SessionService sessionService;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            sessionService = new SessionService(repositoryMock.Object);
        }
        [Test]
        public async Task SessionStatusOpenPostTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            repositoryMock.Setup(p => p.GetAsQueryable<Session>())
                .Returns(new TestAsyncEnumerable<Session>(new List<Session>
                {
                    new Session
                    {
                        SessionId = Guid.NewGuid(),
                        DeviceId = deviceId,
                        BegTime = DateTime.Now.AddMinutes(-10),
                        EndTime = DateTime.Now.AddMinutes(-1),
                        StatusId = 7
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.Create<Session>(It.IsAny<Session>()));
            repositoryMock.Setup(p => p.Save());
            var model = new SessionParams
            {
                ApplicationUserId = userId,
                DeviceId = deviceId,
                Action = "open",
                IsDesktop = true
            };

            //Act
            var responce = sessionService.SessionStatus(model);

            //Assert
            Assert.IsTrue(responce.Message == "Session successfully opened");
        }
        [Test]
        public async Task SessionStatusClosePostTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            repositoryMock.Setup(p => p.GetAsQueryable<Session>())
                .Returns(new TestAsyncEnumerable<Session>(new List<Session>
                {
                    new Session
                    {
                        SessionId = Guid.NewGuid(),
                        DeviceId = deviceId,
                        BegTime = DateTime.Now.AddMinutes(-10),
                        EndTime = DateTime.Now.AddMinutes(-1),
                        StatusId = 6
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.Create<Session>(It.IsAny<Session>()));
            repositoryMock.Setup(p => p.Save());
            var model = new SessionParams
            {
                ApplicationUserId = userId,
                DeviceId = deviceId,
                Action = "close",
                IsDesktop = true
            };

            //Act
            var responce = sessionService.SessionStatus(model);
            System.Console.WriteLine(responce.Message);
            //Assert
            Assert.IsTrue(responce.Message == "session successfully closed");
        }
        [Test]
        public async Task SessionStatusGetTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            repositoryMock.Setup(p => p.GetAsQueryable<Session>())
                .Returns(new TestAsyncEnumerable<Session>(new List<Session>
                {
                    new Session
                    {
                        ApplicationUserId = userId,
                        SessionId = Guid.NewGuid(),
                        DeviceId = deviceId,
                        BegTime = DateTime.Now.AddMinutes(-10),
                        EndTime = DateTime.Now.AddMinutes(-1),
                        StatusId = 6
                    }
                }.AsQueryable()));

            //Act
            var result = sessionService.SessionStatus(deviceId, userId);

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task AlertNotSmilePostTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var alertTypeId = Guid.NewGuid();
            repositoryMock.Setup(p => p.GetAsQueryable<AlertType>())
                .Returns(new TestAsyncEnumerable<AlertType>(new List<AlertType>
                {
                    new AlertType
                    {
                        AlertTypeId = alertTypeId,
                        Name = "client does not smile"
                    }
                }.AsQueryable()));
            repositoryMock.Setup(p => p.Create<Alert>(It.IsAny<Alert>()));
            repositoryMock.Setup(p => p.Save());
            var model = new AlertModel
            {
                ApplicationUserId = userId,
                DeviceId = deviceId
            };

            //Act
            var result = sessionService.AlertNotSmile(model);

            //Assert
            Assert.IsTrue(result.Equals("Alert saved"));
        }
    }
}
