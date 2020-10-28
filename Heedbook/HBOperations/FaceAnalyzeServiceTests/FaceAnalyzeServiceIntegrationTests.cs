using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using HBData.Models;
using HBData.Repository;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using UnitTestExtensions;
using Moq;
using HBLib.Utils;
using HBMLHttpClient;
using HBData;
using System.Collections.Generic;
using HBMLHttpClient.Model;

namespace FaceAnalyzeService.Tests
{
    [TestFixture]
    public class FaceAnalyzeServiceIntegrationTests : ServiceTest
    {
        public SftpClient _sftpClient;
        public IServiceScopeFactory _serviceScopeFactory;
        public Mock<HbMlHttpClient> _hbMlHttpClientMock;
        public ElasticClient _log;
        public RecordsContext _context;
        private FaceAnalyze _faceAnalyzeService;
        private Startup _startup;
        private string frameFileRemotePath;
        private string testFrameCorrectFileName;
        private Guid testFileFrameId;
    
        [SetUp]
        public async Task Setup()
        {
            await base.Setup(() =>
            {
                _startup = new Startup(Config);
                _startup.ConfigureServices(Services);
                StartupExtensions.MockRabbitPublisher(Services);
            }, true);
        }

        [TearDown]
        public async new Task TearDown() => await base.TearDown();

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
            
            var tmpFrameCorrectFileName = testFileFramePath.Replace("testid_", "");
            testFrameCorrectFileName = $"{TestUserId}_{TestDeviceId}_{tmpFrameCorrectFileName}";
            frameFileRemotePath = "frames/" + testFrameCorrectFileName;
           
            if (!await _sftpClient.IsFileExistsAsync("frames/" + testFrameCorrectFileName))
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
                    DeviceId = TestDeviceId,
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
                testFileFrameId = testFileFrame.FileFrameId;
            }
            
            await _sftpClient.DisconnectAsync();
            await _repository.SaveAsync();
        }

        protected override async Task CleanTestData()
        {
            var taskList = await _sftpClient.DeleteFileIfExistsBulkAsync("frames/", $"{TestUserId}*.jpg");
            
            ClearFrameEmotions(testFileFrameId);
            _repository.Delete<FileFrame>(ff => ff.DeviceId == TestDeviceId);
            await _repository.SaveAsync();

            Task.WaitAll(taskList.ToArray());
        }

        protected override void InitServices()
        {
            try{
            _sftpClient = ServiceProvider.GetService<SftpClient>();
            _serviceScopeFactory = ServiceProvider.GetService<IServiceScopeFactory>();
            _hbMlHttpClientMock = new Mock<HbMlHttpClient>();
            _hbMlHttpClientMock.Setup(p => p.GetFaceResult(It.IsAny<String>()))
                .Returns(Task.FromResult<List<FaceResult>>(new List<FaceResult>
                    {
                        new FaceResult
                        {
                            Descriptor = new double[256],
                            Rectangle = new FaceRectangle
                            {
                                Top = 10,
                                Width =10,
                                Height = 10,
                                Left = 10
                            },
                            Attributes = new FaceAttributes
                            {
                                Age = 20,
                                Gender = "male",
                                
                            },
                            Emotions = new FaceEmotions
                            {
                                Anger = 0.5,
                                Contempt = 0.5,
                                Disgust = 0.5,
                                Fear = 0.5,
                                Happiness = 0.5,
                                Neutral = 0.5,
                                Sadness = 0.5,
                                Surprise = 0.5
                            },
                            Headpose = new Headpose
                            {
                                Yaw = 10,
                                Pitch = 10,
                                Roll = 10
                            }
                        }
                    }));
            _log = ServiceProvider.GetService<ElasticClient>();
            _context = ServiceProvider.GetService<RecordsContext>();
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
            }
        }

        [Test, Retry(3)]
        public async Task EnsureCreatesFrameEmotion()
        {
            //Arrange
            try{
            _faceAnalyzeService = new FaceAnalyze(
                _sftpClient,
                _serviceScopeFactory,
                _hbMlHttpClientMock.Object,
                _log,
                _context
            );
            System.Console.WriteLine($"_faceAnalyzeService is null: {_faceAnalyzeService is null}");
            //Act
            await _faceAnalyzeService.Run(frameFileRemotePath);
            //Assert
            Assert.IsTrue(_repository.GetWithInclude<FrameEmotion>(f => f.FileFrameId != Guid.Empty, opt => opt.FileFrame)
                .Any(ff => ff.FileFrame.FileName == testFrameCorrectFileName));
                }
            catch(Exception e)
            {
                System.Console.WriteLine(e);
            }
        }

        private void ClearFrameEmotions(Guid fileFrameId)
            => _repository.Delete<FrameEmotion>(fe => fe.FileFrameId == fileFrameId);
    }
}