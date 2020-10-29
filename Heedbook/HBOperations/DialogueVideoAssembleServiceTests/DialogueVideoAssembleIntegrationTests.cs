using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using HBData.Models;
using HBData.Repository;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using RabbitMqEventBus;
using RabbitMqEventBus.Events;
using UnitTestExtensions;

namespace DialogueVideoAssembleService.Tests
{
    public class DialogueVideoAssembleIntegrationTests : ServiceTest
    {
        private DialogueVideoAssemble _dialogueVideoAssembleService;
        private Startup _startup;
        private DialogueVideoAssembleRun _dialogueVideoAssembleRun;
        private DateTime beginTime = DateTime.MaxValue;
        private DateTime endTime = DateTime.MinValue;
        
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

                var testVideoCorrectFileName = testVideoFilename?.Replace("testid", TestUserId.ToString() + "_" + TestDeviceId.ToString());

                if (!(await _sftpClient.IsFileExistsAsync("videos/" + testVideoCorrectFileName)))
                    await _sftpClient.UploadAsync(filePath, "videos/", testVideoCorrectFileName);
            
                var videoDateTime = prevVideoEndDate ?? GetDateTimeFromFileVideoName(testVideoCorrectFileName);

                // Let's check if such video record already exists in db and delete it if exists
                if (_repository.Get<FileVideo>().Any(fv => fv.FileName == testVideoCorrectFileName))
                {
                    _repository.Delete<FileVideo>(fv => fv.FileName == testVideoFilename);
                    _repository.Save();
                }

                var fileVideo = new FileVideo()
                {
                    FileVideoId = Guid.NewGuid(),
                    ApplicationUserId = TestUserId,
                    DeviceId = TestDeviceId,
                    BegTime = videoDateTime,
                    EndTime = videoDateTime.AddSeconds(deltaSeconds),
                    CreationTime = DateTime.Now,
                    FileName = testVideoCorrectFileName,
                    FileContainer = "videos",
                    FileExist = true,
                    StatusId = 5,
                    Duration = null
                };

                if (beginTime > fileVideo.BegTime)
                    beginTime = fileVideo.BegTime;
                
                if (endTime < fileVideo.EndTime)
                    endTime = fileVideo.EndTime;

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
                    .Replace("testid", TestUserId.ToString() + "_" + TestDeviceId.ToString());
                if (!(await _sftpClient.IsFileExistsAsync("frames/" + testFrameCorrectFileName)))
                    await _sftpClient.UploadAsync(testFramePath, "frames/", testFrameCorrectFileName);
                
                // if frame doesn't exist => let's create it!
                if (_repository.Get<FileFrame>().Any(ff => ff.FileName == testFrameCorrectFileName)) 
                    continue;
                
                var testFileFrame = new FileFrame
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
            }

            await _repository.CreateAsync(newDialog);
            await _repository.SaveAsync();
            _dialogueVideoAssembleRun = new DialogueVideoAssembleRun()
            {
                ApplicationUserId = TestUserId,
                DeviceId = TestDeviceId,
                DialogueId = newDialog.DialogueId,
                BeginTime = beginTime,
                EndTime = endTime
            };
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
            _dialogueVideoAssembleService = ServiceProvider.GetService<DialogueVideoAssemble>();
        }

        [Test, Retry(3)]
        public async Task EnsureCreatesOutputVideoFile()
        {
            Thread.Sleep(10000);
            _sftpClient.ChangeDirectoryToDefault();

            await _dialogueVideoAssembleService.Run(_dialogueVideoAssembleRun);

            //Assert
            Thread.Sleep(5000);
            Assert.IsTrue(await _sftpClient.IsFileExistsAsync($"dialoguevideos/{_dialogueVideoAssembleRun.DialogueId}.mp4"));
        }

        private async Task CleanAllVideoFilesFromDb()
        {
            _repository.Delete<FileVideo>( fv => fv.DeviceId == TestDeviceId );
            await _repository.SaveAsync();
        }
        
        private async Task CleanAllFileFramesFromDb()
        {
            _repository.Delete<FileFrame>( ff => ff.DeviceId == TestDeviceId );
            await _repository.SaveAsync();
        }
    }
}