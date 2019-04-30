using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using HBData.Models;
using HBData.Repository;
using Microsoft.Extensions.DependencyInjection;
using DialogueVideoMergeService;
using NUnit.Framework;
using NUnit.Framework.Internal;
using RabbitMqEventBus.Events;

namespace DialogueVideoMerge.Tests
{
    [TestFixture]
    public class DialogueVideoMergeIntegrationTests : ServiceTest
    {
        private DialogueVideoMergeService.DialogueVideoMerge _dialogueVideoMergeService;
        private Startup _startup;
        private DialogueVideoMergeRun _dialogueVideoMergeRun;
        
        [SetUp]
        public async Task Setup()
        {
            await base.Setup(() =>
            {
                _startup = new Startup(Config);
                _startup.ConfigureServices(Services);
            }, true);
        }

        [TearDown]
        public async Task TearDown()
        {
            await base.TearDown();
        }
        
        protected override async Task PrepareTestData()
        {
            _dialogueVideoMergeRun = new DialogueVideoMergeRun()
            {
                ApplicationUserId = TestUserId,
                DialogueId = Guid.NewGuid(),
                BeginTime = DateTime.MinValue,
                EndTime = DateTime.MaxValue
            };

            DateTime? prevVideoEndDate = null;
            const int deltaSeconds = 15;
            
            var currentDir = Environment.CurrentDirectory;
            var testVideosFilepath = Directory
                .GetFiles(Path.Combine(currentDir, "Resources/Videos"), "testid*.mkv");

            if (testVideosFilepath == null)
                throw new Exception("Can't get test video for preparing a testset!");

            foreach (var filePath in testVideosFilepath)
            {
                var testVideoFilename = Path.GetFileName(filePath);

                var testVideoCorrectFileName = testVideoFilename?.Replace("testid", TestUserId.ToString());

                if (!(await _sftpClient.IsFileExistsAsync("videos/" + testVideoCorrectFileName)))
                    await _sftpClient.UploadAsync(filePath, "videos/", testVideoCorrectFileName);
            
                var videoDateTime = prevVideoEndDate ?? GetDateTimeFromFileVideoName(testVideoCorrectFileName);
                
                // Let's check if such video record already exists in db
                if (_repository.Get<FileVideo>().Any( fv => fv.FileName == testVideoCorrectFileName ))
                    continue;
                
                var fileVideo = new FileVideo()
                {
                    FileVideoId = Guid.NewGuid(),
                    ApplicationUserId = TestUserId,
                    BegTime = videoDateTime,
                    EndTime = videoDateTime.AddSeconds(deltaSeconds),
                    CreationTime = videoDateTime,
                    FileName = testVideoCorrectFileName,
                    FileContainer = "videos",
                    FileExist = true,
                    StatusId = 5,
                    Duration = null
                };

                prevVideoEndDate = fileVideo.EndTime;

                await _repository.CreateAsync(fileVideo);
            }
            
            var testFramesFilepath = Directory
                .GetFiles(Path.Combine(currentDir, "Resources/Frames"), "testid*.jpg");
            
            if (testFramesFilepath.Length == 0)
                throw new Exception("Can't get test frames for preparing a testset!");
            
            // Create a dialog object
            var newDialog = CreateNewTestDialog();
            
            // filling frames 
            foreach (var testFramePath in testFramesFilepath)
            {
                var testFileFramePath = Path.GetFileName(testFramePath);
                var testFrameCorrectFileName = testFileFramePath
                    .Replace("testid", TestUserId.ToString());

                if (!(await _sftpClient.IsFileExistsAsync("frames/" + testFrameCorrectFileName)))
                    await _sftpClient.UploadAsync(testFramePath, "frames/", testFrameCorrectFileName);
                
                // if frame doesn't exist => let's create it!
                if (_repository.Get<FileFrame>().Any(ff => ff.FileName == testFrameCorrectFileName)) 
                    continue;
                
                var testFileFrame = new FileFrame
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

            await _repository.CreateAsync(newDialog);
            await _repository.SaveAsync();
        }

        protected override async Task CleanTestData()
        {
            var taskList = await _sftpClient.DeleteFileIfExistsBulkAsync("videos/", $"*{TestUserId}*.mkv");
            taskList.Concat(await _sftpClient.DeleteFileIfExistsBulkAsync("frames/", $"*{TestUserId}*.jpg"));

            await CleanAllVideoFilesFromDb();
            await CleanAllFileFramesFromDb();
            
            Task.WaitAll(taskList.ToArray());
        }

        protected override void InitServices()
        {
            _repository = ServiceProvider.GetService<IGenericRepository>();
            _dialogueVideoMergeService = ServiceProvider.GetService<DialogueVideoMergeService.DialogueVideoMerge>();
        }

        [Test]
        public async Task EnsureCreatesOutputVideoFile()
        {
            _sftpClient.ChangeDirectoryToDefault();
            await _dialogueVideoMergeService.Run(_dialogueVideoMergeRun);
            Assert.IsTrue(await _sftpClient.IsFileExistsAsync($"dialoguevideos/{_dialogueVideoMergeRun.DialogueId}.mkv"));
        }

        private async Task CleanAllVideoFilesFromDb()
        {
            _repository.Delete<FileVideo>( fv => fv.ApplicationUserId == TestUserId );
            await _repository.SaveAsync();
        }
        
        private async Task CleanAllFileFramesFromDb()
        {
            _repository.Delete<FileFrame>( ff => ff.ApplicationUserId == TestUserId );
            await _repository.SaveAsync();
        }
    }
}