using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Quartz;
using UnitTestExtensions;

namespace CloneFtpOnAzureService.Tests
{
    [TestFixture]
    public class CloneFtpOnAzureServiceTests : ServiceTest
    {
        private FtpJob _FtpJob;
        private Process _schedulerProcess;
        private Startup _startup;
        private Guid testDialogueId;
        private string testDialogVideoCorrectFileName;
        private string testDialogAudioCorrectFileName;
        private FtpJob _ftpJob;
        private SftpClient _sftpClient;
        private BlobSettings _blobSettings;
        private BlobClient _blobClient;
        private ElasticClientFactory _elasticClientFactory;
        private SftpSettings _sftpSetting;
        
        [SetUp]
        public async Task Setup()
        {
            await base.Setup(() =>
            {
                Services.Configure<BlobSettings>(Config.GetSection(nameof(BlobSettings)));
                Services.AddSingleton(provider=> provider.GetRequiredService<IOptions<BlobSettings>>().Value);
                Services.AddSingleton<BlobClient>();
                Services.Configure<ElasticSettings>(Config.GetSection(nameof(ElasticSettings)));
                Services.AddSingleton(provider => provider.GetRequiredService<IOptions<ElasticSettings>>().Value);
                Services.AddSingleton<ElasticClientFactory>();
            }, true);
            RunServices();
        }
        protected override void InitServices()
        {
            _repository = ServiceProvider.GetService<IGenericRepository>();
            _sftpClient = ServiceProvider.GetService<SftpClient>();
            _blobSettings = ServiceProvider.GetService<BlobSettings>();
            _blobClient = ServiceProvider.GetService<BlobClient>();
            _elasticClientFactory = ServiceProvider.GetService<ElasticClientFactory>();
            _sftpSetting = ServiceProvider.GetService<SftpSettings>();
        }
        private void RunServices()
        {
            _ftpJob = new FtpJob(
                ScopeFactory,
                _sftpClient,
                _blobSettings,
                _blobClient,
                _elasticClientFactory,
                _sftpSetting
            );

        }
        protected override async Task PrepareTestData()
        {
            testDialogueId = Guid.NewGuid();
            var dialogue = new Dialogue()
            {
                DialogueId = testDialogueId,
                StatusId = 3,
                CreationTime = DateTime.Now.AddMinutes(5),
                BegTime = DateTime.Now,
                EndTime = DateTime.Now.AddMinutes(3),
                DeviceId = TestDeviceId
            };
            _repository.AddOrUpdate(dialogue);
            await _repository.SaveAsync();
            var currentDir = $"../../../../Common";

            var testDialogVideoPath = Directory
                .GetFiles(Path.Combine(currentDir, "Resources/DialogueVideos"), "dialogueid.mkv")
                .FirstOrDefault();

            var testDialogAudioPath = Directory
                .GetFiles(Path.Combine(currentDir, "Resources/DialogueAudios"), "dialogueid.wav")
                .FirstOrDefault();

            if (testDialogVideoPath == null)
                throw new Exception("Can't get a test DialogueVideo for preparing a testset!");

            testDialogVideoCorrectFileName = testDialogueId + ".mkv";
            testDialogAudioCorrectFileName = testDialogueId + ".wav";
            var tasks = new List<Task>();
            if (!(await _sftpClient.IsFileExistsAsync("dialoguevideos/" + testDialogVideoCorrectFileName)))
            {
                System.Console.WriteLine($"upload: {testDialogVideoCorrectFileName}");
                await _sftpClient.UploadAsync(testDialogVideoPath, "dialoguevideos/", testDialogVideoCorrectFileName);
            }
                
            if (!(await _sftpClient.IsFileExistsAsync("dialogueaudios/" + testDialogAudioCorrectFileName)))
            {
                System.Console.WriteLine($"upload: {testDialogAudioCorrectFileName}");
                await _sftpClient.UploadAsync(testDialogAudioPath, "dialogueaudios/", testDialogAudioCorrectFileName);
            }
             _sftpClient.ChangeDirectoryToDefault();
        }
        [Test]
        public async Task CheckSoundFilePresents()
        {
            //Arrange
            IJobExecutionContext mockJobExecutionContext= new Mock<IJobExecutionContext>().Object;
            //Act
            await _ftpJob.Execute(mockJobExecutionContext);
            var videoExist = await _blobClient.CheckFileExist("dialoguevideos", testDialogVideoCorrectFileName);
            var audioExist = await _blobClient.CheckFileExist("dialogueaudios", testDialogAudioCorrectFileName);
            //Assert
            Assert.IsTrue(videoExist);
            Assert.IsTrue(audioExist);
        }
        [TearDown]
        public async new Task TearDown()
        {
            await base.TearDown();
        }
        protected override async Task CleanTestData()
        {
            _repository.Delete<Dialogue>(p => p.DialogueId == testDialogueId);
            _repository.Save();                
        }
    }
}