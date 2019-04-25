using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using HBData.Models;
using HBData.Repository;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;


namespace FaceAnalyzeService.Tests
{
    [TestFixture]
    public class FaceAnalyzeServiceIntegrationTests : ServiceTest
    {
        private FaceAnalyze _faceAnalyzeService;
        private Startup _startup;
        private string frameFileRemotePath;
        private Guid testfileFrameId;
    
        [SetUp]
        public async Task Setup()
        {
            await base.Setup(() =>
            {
                _startup = new Startup(Config);
                _startup.ConfigureServices(Services);
            }, true);
        }

        protected override async Task PrepareTestData()
        {
            var currentDir = Environment.CurrentDirectory;

            var testFramePath = Directory
                .GetFiles(Path.Combine(currentDir, "Resources/Frames"), "testid*.jpg")
                .FirstOrDefault();

            if (testFramePath == null)
                throw new Exception("Can't get test frames for preparing a testset!");

            // filling frames 
            var testFileFramePath = Path.GetFileName(testFramePath);
            var testFrameCorrectFileName = testFileFramePath
                .Replace("testid", TestUserId.ToString());

            if (!(await _sftpClient.IsFileExistsAsync("frames/" + testFrameCorrectFileName)))
                await _sftpClient.UploadAsync(testFramePath, "frames/", testFrameCorrectFileName);

            // if frame doesn't exist => let's create it!
            var testFileFrame = _repository.Get<FileFrame>()
                .FirstOrDefault(ff => ff.FileName == testFrameCorrectFileName);

            if (testFileFrame != null)
                ClearFrameEmotions(testFileFrame.FileFrameId);
            else
            {
                testFileFrame = new FileFrame
                {
                    FileFrameId = Guid.NewGuid(),
                    ApplicationUserId = TestUserId,
                    FileExist = true,
                    FileName = testFrameCorrectFileName,
                    FileContainer = "frames",
                    StatusId = 5,
                    StatusNNId = null,
                    Time = GetDateTimeFromFileFrameName(testFrameCorrectFileName),
                    IsFacePresent = true,
                    FaceLength = null
                };

                await _repository.CreateAsync(testFileFrame);
                testfileFrameId = testFileFrame.FileFrameId;
            }
            
            frameFileRemotePath = "frames/" + testFrameCorrectFileName;
            await _repository.SaveAsync();
            //await _sftpClient.DisconnectAsync();
        }

        protected override void InitServices()
        {
            _repository = ServiceProvider.GetService<IGenericRepository>();
            _faceAnalyzeService = ServiceProvider.GetService<FaceAnalyze>();
        }

        [Test]
        public void EnsureCreatesFrameEmotion()
        {
            _faceAnalyzeService.Run(frameFileRemotePath);
            Assert.IsTrue(_repository.Get<FrameEmotion>().Any(ff => ff.FileFrameId == testfileFrameId));
        }

        private void ClearFrameEmotions(Guid fileFrameId)
        {
            _repository.Delete<FrameEmotion>(fe => fe.FileFrameId == fileFrameId);
        }
    }
}