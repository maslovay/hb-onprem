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
    public class FillingFileFrameControllerTest : ApiServiceTest
    {
        private FillingFileFrameService fillingFileFrameService;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            fillingFileFrameService = new FillingFileFrameService(
                repositoryMock.Object);
        }
        [Test]
        public async Task FillingFileFramePostTest()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var frames = new List<FileFramePostModel>
            {
                new FileFramePostModel
                {
                    Age = 20,
                    Gender = "male",
                    Yaw = 0.6d,
                    Smile = 0.5d,
                    ApplicationUserId = userId,
                    DeviceId = deviceId,
                    Time = DateTime.Now,
                    Descriptor = new double[]{0.3, 0.4, 0.5, 0.6},
                    FaceArea = 30,
                    Top = 30,
                    Left = 50,
                    VideoHeight = 200,
                    VideoWidth = 300
                }
            };
            repositoryMock.Setup(p => p.GetWithIncludeOne<Device>(It.IsAny<Expression<Func<Device, bool>>>(), It.IsAny<Expression<Func<Device, object>>[]>()))
                .Returns(
                    new Device
                    {
                        Code = "AAAAAA",
                        CompanyId = companyId,
                        Company = new Company
                        {
                            CompanyId = companyId,
                            IsExtended = false
                        },
                        StatusId = 3
                    });
            repositoryMock.Setup(p => p.CreateRange<FileFrame>(It.IsAny<List<FileFrame>>()));
            repositoryMock.Setup(p => p.CreateRange<FrameAttribute>(It.IsAny<List<FrameAttribute>>()));
            repositoryMock.Setup(p => p.CreateRange<FrameEmotion>(It.IsAny<List<FrameEmotion>>()));
            repositoryMock.Setup(p => p.Save());
            
            //Act
            var fileFrames = fillingFileFrameService.FillingFileFrame(frames);
            
            //Assert
            Assert.IsTrue(fileFrames.Count > 0);
        }
    }
}