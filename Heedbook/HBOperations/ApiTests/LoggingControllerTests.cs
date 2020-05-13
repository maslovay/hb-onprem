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

namespace ApiTests
{
    public class LoggingControllerTests : ApiServiceTest
    {
        private LoggingController loggingController;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            loggingController = new LoggingController();
        }
        [Test]
        public async Task SendLogPostTest()
        {
            //Arrange

            //Act
            var result = await loggingController.SendLogPost(
                "message",
                "severity",
                "functionName",
                new JObject()
            );
            //Assert
            Assert.IsTrue(result.StatusCode == 200);
            Assert.IsTrue((string)result.Value == "Logged");
        }
        [Test]
        public async Task SendLogGetTest()
        {
            //Arrange

            //Act
            var result = await loggingController.SendLog(
                "message",
                "severity",
                "functionName");

            //Assert
            Assert.IsTrue(result.StatusCode == 200);
            Assert.IsTrue((string)result.Value == "logged");
        }
    }
}