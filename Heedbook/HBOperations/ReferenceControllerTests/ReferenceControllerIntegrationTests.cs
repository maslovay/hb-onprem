using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ReferenceController;
using TeacherAPI;
using UnitTestExtensions;


namespace ReferenceController.Tests
{
    [TestFixture]
    public class ReferenceControllerIntegrationTests : ServiceTest
    {
        private string _testFrameFilename;
        private string linkCheckRegex = "\\[\"http\\:\\/\\/(.*)\\?path=(.*)&expirationDate=(.*)&token=(.*)\"\\]";
        private DateTime fileDateTime;
        
        [SetUp]
        public async Task Setup()
        {
            await base.Setup(() =>
            {
                var _startup = new Startup(Config);
                _startup.ConfigureServices(Services);
                StartupExtensions.MockRabbitPublisher(Services);
            }, true);
        }

        [TearDown]
        public async Task TearDown()
        {
            await base.TearDown();
        }

        protected override async Task PrepareTestData()
        {
            var currentDir = Environment.CurrentDirectory;
            var testFrameFilepath = Directory
                .GetFiles(Path.Combine(currentDir, "Resources/Frames"), "testid*.jpg").FirstOrDefault();

            _testFrameFilename = Path.GetFileName(testFrameFilepath);

            await _sftpClient.UploadAsync(testFrameFilepath, "frames", _testFrameFilename);

            fileDateTime = DateTime.Now.AddDays(1);
        }

        protected override async Task CleanTestData()
        {
            await _sftpClient.DeleteFileIfExistsAsync("frames/" + _testFrameFilename);
        }

        protected override void InitServices()
        {
        }

        [Test]
        public async Task CheckReferenceGeneration()
        {
            var _referenceController = new FileRefController(Config, _sftpClient);
            var reference = _referenceController.GetNewReference("frames", _testFrameFilename, fileDateTime);
            var value = ((OkObjectResult) reference).Value.ToString();

            Assert.NotNull(reference);
            Assert.True(Regex.IsMatch(value, linkCheckRegex));
        }
        
        [Test]
        public async Task CheckFileGetting()
        {
            var _referenceController = InitReference(out var token);


            _sftpClient.ChangeDirectoryToDefault();
            var result = await _referenceController.GetFile("frames/" + _testFrameFilename, fileDateTime, token);
            Assert.IsInstanceOf<FileStreamResult>(result);
        }


        [Test]
        public async Task CheckReturnsBadRequestOnWrongExpirationDate()
        {
            var _referenceController = InitReference(out var token);
            _sftpClient.ChangeDirectoryToDefault();

            var result = await _referenceController.GetFile("frames/" + _testFrameFilename, fileDateTime.AddDays(2), token);
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task CheckReturnsBadRequestOnWrongFilename()
        {
            var _referenceController = InitReference(out var token);
            _sftpClient.ChangeDirectoryToDefault();

            var result = await _referenceController.GetFile("frames/" + _testFrameFilename + "_", fileDateTime, token);
            Assert.IsInstanceOf<BadRequestResult>(result);
        }
        

        private FileRefController InitReference(out string token)
        {
            var _referenceController = new FileRefController(Config, _sftpClient);
            var reference = _referenceController.GetNewReference("frames", _testFrameFilename, fileDateTime);
            var value = ((OkObjectResult) reference).Value.ToString();

            Assert.NotNull(reference);

            token = ExtractToken(value);
            return _referenceController;
        }

        
        private string ExtractToken(string value)
        {
            var tokenRegex = new Regex(linkCheckRegex);
            var match = tokenRegex.Match(value);
            var token = (match != null) ? match.Groups[4].Value : null;

            return token;
        }
    }
}