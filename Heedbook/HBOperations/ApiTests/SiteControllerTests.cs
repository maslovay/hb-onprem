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
    public class SiteControllerTests : ApiServiceTest
    {
        private SiteService siteService;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            siteService = new SiteService(mailSenderMock.Object);
        }
        [Test]
        public async Task FeedbackPostTest()
        {
            //Arrange
            mailSenderMock.Setup(p => p.SendSimpleEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            var model = new FeedbackEntity
            {
                name = "TestName",
                phone = "88008888888",
                body = "<div>body text</div>",
                email = "info@heedbook.com"
            };
            
            //Act
            var result = siteService.Feedback(model);

            //Assert
            Assert.IsTrue(result == "Sended");
        }
    }
}