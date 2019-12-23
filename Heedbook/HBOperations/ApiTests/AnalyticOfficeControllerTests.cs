using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using UserOperations.Controllers;
using System;
using System.Threading.Tasks;
using UserOperations.Providers;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using UserOperations.Models.AnalyticModels;

namespace ApiTests
{
    public class AnalyticOfficeControllerTests : ApiServiceTest
    {   
      //  private Mock<IAnalyticOfficeProvider> analyticOfficeProviderMock;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
        }
        protected override void InitServices()
        {
         //   analyticOfficeProviderMock = new Mock<IAnalyticOfficeProvider>();
            var sessionsInfo = new List<SessionInfoFull>
            {
                new SessionInfoFull
                {
                    ApplicationUserId = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0"),
                    BegTime = new DateTime(2019,10,04, 12, 19,00),
                    EndTime = new DateTime(2019,10,04,12,20,25),
                    CompanyId = Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182"),
                    FullName = "tuisv@heedbook.com",
                    IndustryId = Guid.Parse("99960395-2cc3-46e8-bcef-c844f1048999")
                },
                  new SessionInfoFull
                {
                    ApplicationUserId = Guid.Parse("8d5cd62c-2ea0-406e-8ec1-a544d048a9d0"),
                    BegTime = new DateTime(2019,10,04, 18, 19,00),
                    EndTime = new DateTime(2019,10,04,18,25,30),
                    CompanyId = Guid.Parse("82560395-2cc3-46e8-bcef-c844f1048182"),
                    FullName = "tuisv@heedbook.com",
                    IndustryId = Guid.Parse("99960395-2cc3-46e8-bcef-c844f1048999")
                }
            };
        //    analyticOfficeProviderMock.Setup(p => p.GetSessionsInfo(
        //        It.IsAny<DateTime>(),
        //        It.IsAny<DateTime>(),
        //        It.IsAny<List<Guid>>(),
        //        It.IsAny<List<Guid>>(),
        //        It.IsAny<List<Guid>>()))
        //        .Returns(sessionsInfo);
        //    var dialogues = new List<DialogueInfo>()
        //    {
        //        new DialogueInfo(){BegTime = new DateTime(2019, 10, 29, 18, 30, 00), EndTime = new DateTime(2019, 10, 29, 19, 00, 00)},
        //        new DialogueInfo(){BegTime = new DateTime(2019, 10, 30, 18, 30, 00), EndTime = new DateTime(2019, 10, 30, 19, 00, 00)},
        //        new DialogueInfo(){BegTime = new DateTime(2019, 10, 30, 19, 10, 00), EndTime = new DateTime(2019, 10, 30, 19, 40, 00)},
        //        new DialogueInfo(){BegTime = new DateTime(2019, 10, 30, 19, 50, 00), EndTime = new DateTime(2019, 10, 30, 20, 20, 00)},
        //        new DialogueInfo(){BegTime = new DateTime(2019, 10, 30, 19, 10, 00), EndTime = new DateTime(2019, 10, 30, 19, 40, 00)},
        //        new DialogueInfo(){BegTime = new DateTime(2019, 10, 30, 20, 30, 00), EndTime = new DateTime(2019, 10, 30, 21, 00, 00)},
        //        new DialogueInfo(){BegTime = new DateTime(2019, 10, 30, 21, 10, 00), EndTime = new DateTime(2019, 10, 30, 21, 40, 00)},
        //        new DialogueInfo(){BegTime = new DateTime(2019, 10, 31, 18, 30, 00), EndTime = new DateTime(2019, 10, 31, 19, 00, 00)},
        //        new DialogueInfo(){BegTime = new DateTime(2019, 11, 01, 18, 30, 00), EndTime = new DateTime(2019, 11, 01, 19, 00, 00)}
        //    };
        //    analyticOfficeProviderMock.Setup(p => p.GetDialoguesInfo(
        //        It.IsAny<DateTime>(),
        //        It.IsAny<DateTime>(),
        //        It.IsAny<List<Guid>>(),
        //        It.IsAny<List<Guid>>(),
        //        It.IsAny<List<Guid>>()))
        //        .Returns(dialogues);
        //    InitMockILoginService();
        //}
        //[Test]
        //public async Task UserRegister()
        //{
        //    //Arrange
        //    var controller = new AnalyticOfficeController(
        //        configMock.Object,
        //        moqILoginService.Object, 
        //        dbOperationMock.Object, 
        //        filterMock.Object, 
        //        analyticOfficeProviderMock.Object);
        //    var sessions = await TestData.GetSessions();
            
        //    //Act
        //    var result = controller.Efficiency(
        //        TestData.beg, TestData.end,
        //        TestData.GetGuids(), TestData.GetGuids(), TestData.GetGuids(), TestData.GetGuids(),
        //        TestData.token
        //    );

        //    //Assert
        //    var okResult = result as OkObjectResult;
        //    Assert.IsNotNull(okResult);
        //    Assert.AreEqual(200, okResult.StatusCode);
        //    Assert.That(okResult.Value != null);
        //    var deserialized = JsonConvert.DeserializeObject<Dictionary<string, object>>(okResult.Value.ToString());
        //    Assert.That(deserialized != null);
        }
    }
}