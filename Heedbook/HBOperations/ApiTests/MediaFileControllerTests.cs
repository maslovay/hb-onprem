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
using HBLib;

namespace ApiTests
{
    public class MediaFileControllerTests : ApiServiceTest
    {
        private MediaFileService mediaFileService;
        [SetUp]
        public new void Setup()
        {
            base.Setup();
            mediaFileService = new MediaFileService(
                moqILoginService.Object,
                sftpClient.Object,
                fileRefUtils.Object,
                elasticClient.Object,
                new URLSettings(){Host = "https://heedbookapitest.westeurope.cloudapp.azure.com/"});
        }
        [Test]
        public async Task FileGetGetTest()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            fileRefUtils.Setup(p => p.GetFileLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns("fileRef");
            sftpClient.Setup(p => p.CreateIfDirNoExistsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(0));
            sftpClient.Setup(p => p.GetFileNames(It.IsAny<string>()))
                .Returns(Task.FromResult(new List<string>
                {
                    "file"
                }));
            elasticClient.Setup(p => p.Info(It.IsAny<string>(), It.IsAny<object[]>()))
                .Verifiable();

            //Act
            var result = await mediaFileService.FileGet(
                "testContainer",
                "testFile",
                DateTime.Now);

            //Assert
            Assert.IsFalse(result is null);
        }
        [Test]
        public async Task FilePostPostTest()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            sftpClient.Setup(p => p.UploadAsMemoryStreamAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(0));
            fileRefUtils.Setup(p => p.GetFileLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns("fileRef");
            var formData = new FormCollection
            (
                new Dictionary<string, StringValues>
                {
                    {"containerName", "containerName"}
                },
                new FormFileCollection
                {
                    new FormFile(new MemoryStream(), 100, 100, "testName", "testFile")
                }
            );

            //Act
            var result = await mediaFileService.FilePost(formData);

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(result.Count > 0);
        }
        [Test]
        public async Task FilePutPutTest()
        {
            //Arrange
            var companyId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            sftpClient.Setup(p => p.DeleteFileIfExistsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(0));
            sftpClient.Setup(p => p.UploadAsMemoryStreamAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(0));
            fileRefUtils.Setup(p => p.GetFileLink(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns("fileRef");
            var formData = new FormCollection
            (
                new Dictionary<string, StringValues>
                {
                    {"containerName", "containerName"},
                    {"fileName", "testFile"}
                },
                new FormFileCollection
                {
                    new FormFile(new MemoryStream(), 100, 100, "testName", "testFile")
                }
            );

            //Act
            var result = await mediaFileService.FilePut(formData);

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue(result == "fileRef");
        }
        [Test]
        public async Task FileDeleteDeleteTest()
        {
            //Arrange
            //Arrange
            var companyId = Guid.NewGuid();
            moqILoginService.Setup(p => p.GetCurrentCompanyId())
                .Returns(companyId);
            sftpClient.Setup(p => p.DeleteFileIfExistsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(0));

            //Act
            var result = await mediaFileService.FileDelete(
                "containerName",
                "fileName");

            //Assert
            Assert.IsFalse(result is null);
            Assert.IsTrue((string)result == "OK");
        }
    }
}