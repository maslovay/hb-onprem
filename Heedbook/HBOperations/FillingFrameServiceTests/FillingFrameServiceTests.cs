using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using Common;
using FillingFrameService.Exceptions;
using HBData.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources;
using NUnit.Framework;
using RabbitMqEventBus.Events;
using Renci.SshNet.Messages;
using UnitTestExtensions;

namespace FillingFrameService.Tests
{
    [TestFixture]
    public class FillingFrameServiceTests : ServiceTest
    {
        private DialogueCreation _fillingFrameService;
        private Startup startup;
        private ResourceManager resourceManager;
        private DialogueCreationRun dialogCreationRun;
        private List<FileFrame> fileFrames = new List<FileFrame>(5);
        
        [SetUp]
        public async Task Setup()
        {
            await base.Setup(() =>
            {
                startup = new Startup(Config);
                startup.ConfigureServices(Services);
                StartupExtensions.MockRabbitPublisher(Services);
            }, true);
        }

        public async void TearDown()
        {
            await base.TearDown();
        }

        protected override async Task PrepareTestData()
        {
            fileFrames.Clear();
            
            var currentDir = Environment.CurrentDirectory;
            var testVideoFilepath = Directory
                .GetFiles(Path.Combine(currentDir, "Resources/Videos"), "testid*.mkv").FirstOrDefault();

            if (testVideoFilepath == null)
                throw new Exception("Can't get a test video for preparing a testset!");
            
            var testVideoFilename = Path.GetFileName(testVideoFilepath);
            
            var testVideoCorrectFileName = testVideoFilename?.Replace("testid", TestUserId.ToString());

            if (!(await _sftpClient.IsFileExistsAsync("videos/" + testVideoCorrectFileName)))
                await _sftpClient.UploadAsync(testVideoFilepath, "videos/", testVideoCorrectFileName);
            
            var testFramesFilepath = Directory
                .GetFiles(Path.Combine(currentDir, "Resources/Frames"), "testid*.jpg");
            
            if (testFramesFilepath.Length == 0)
                throw new Exception("Can't get test frames for preparing a testset!");

            
            var videoDateTime = GetDateTimeFromFileVideoName(testVideoCorrectFileName);
            
            // Create a dialog object
            dialogCreationRun = new DialogueCreationRun()
            {
                ApplicationUserId = TestUserId,
                DialogueId = Guid.NewGuid(),
                BeginTime = DateTime.MinValue,
                EndTime = videoDateTime.AddDays(10)
            };
            
            var newDialog = new Dialogue
            {
                DialogueId = dialogCreationRun.DialogueId,
                CreationTime = videoDateTime.AddDays(-10),
                BegTime = videoDateTime.AddDays(-10),
                EndTime = videoDateTime.AddDays(10),
                ApplicationUserId = TestUserId,
                LanguageId = null,
                StatusId = null,
                SysVersion = "",
                InStatistic = false,
                Comment = "test dialog!!!"
            };
            
            // filling frames 
            foreach (var testFramePath in testFramesFilepath )
            {
                var testFileFramePath = Path.GetFileName(testFramePath);
                var testFrameCorrectFileName = testFileFramePath
                    .Replace("testid", TestUserId.ToString());

                if (!(await _sftpClient.IsFileExistsAsync("frames/" + testFrameCorrectFileName)))
                    await _sftpClient.UploadAsync(testFramePath, "frames/", testFrameCorrectFileName);

                FileFrame testFileFrame;
                
                // if frame doesn't exist => let's create it!
                if (_repository.Get<FileFrame>().All(ff => ff.FileName != testFrameCorrectFileName))
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
                }
                else
                {
                    testFileFrame = _repository.Get<FileFrame>().First(ff => ff.FileName == testFrameCorrectFileName);
                    
                    // clean emotions and attributes in order to create new ones
                    _repository.Delete<FrameEmotion>( e => e.FileFrameId == testFileFrame.FileFrameId );
                    _repository.Delete<FrameAttribute>( a => a.FileFrameId == testFileFrame.FileFrameId );
                    await _repository.SaveAsync();
                }
                
                fileFrames.Add(testFileFrame);
                
                var newFrameEmotion = new FrameEmotion
                {
                    FrameEmotionId = Guid.NewGuid(),
                    FileFrameId = testFileFrame.FileFrameId,
                    AngerShare = 0.1,
                    ContemptShare = 0.04,
                    DisgustShare = 0.1,
                    HappinessShare = 0.6,
                    NeutralShare = 0.2,
                    SadnessShare = 0.1,
                    SurpriseShare = 0.8,
                    FearShare = 0.2,
                    YawShare = 0.3
                };

                var newFrameAttribute = new FrameAttribute
                {
                    FrameAttributeId = Guid.NewGuid(),
                    FileFrameId = testFileFrame.FileFrameId,
                    Gender = "Female",
                    Age = 25,
                    Value = resourceManager.GetString("TestAttributeValue"),
                    Descriptor = resourceManager.GetString("TestFaceDescriptor")
                };

                await _repository.CreateAsync(newFrameEmotion);
                await _repository.CreateAsync(newFrameAttribute);
            }

            await _repository.CreateAsync(newDialog);
            await _repository.SaveAsync();
        }

        protected override Task CleanTestData()
        {
            return null;
        }

        protected override void InitServices()
        {
            resourceManager = new ResourceManager("FillingFrameServiceTests.Resources.StringResources", 
                Assembly.GetExecutingAssembly());
            _fillingFrameService = ServiceProvider.GetService<DialogueCreation>();
        }

        [Test]
        public async Task EnsureCreatesDialogueFrameRecords()
        {
            await _fillingFrameService.Run(dialogCreationRun);
            
            Assert.IsTrue(_repository.Get<DialogueVisual>().Any(dv => dv.DialogueId == dialogCreationRun.DialogueId));
            Assert.IsTrue(_repository.Get<DialogueClientProfile>().Any(pr => pr.DialogueId == dialogCreationRun.DialogueId));

            var resultDialogFrames = _repository.Get<DialogueFrame>()
                .Where(df => df.DialogueId == dialogCreationRun.DialogueId);
            
            var resultEmotions = _repository.Get<FrameEmotion>()
                .Where(e => fileFrames.Any( ff => ff.FileFrameId == e.FileFrameId));

            Assert.AreEqual(resultDialogFrames.Count(), resultEmotions.Count());
        }
    }
}