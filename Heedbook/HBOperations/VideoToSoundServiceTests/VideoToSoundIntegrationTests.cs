using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using HBData.Repository;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace VideoToSoundService.Tests
{
    [TestFixture]
    public class VideoToSoundServiceIntegrationTests : ServiceTest
    {
        private VideoToSound _videoToSoundService;
        private Startup _startup;
        private string testDialogueId;
        private string testDialogVideoCorrectFileName;
        private string testDialogAudioCorrectFileName;
        
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
            testDialogueId = Guid.NewGuid().ToString();
            
            var currentDir = Environment.CurrentDirectory;

            var testDialogVideoPath = Directory
                .GetFiles(Path.Combine(currentDir, "Resources/DialogueVideos"), "dialogueid.mkv")
                .FirstOrDefault();

            if (testDialogVideoPath == null)
                throw new Exception("Can't get a test DialogueVideo for preparing a testset!");

             testDialogVideoCorrectFileName = Path.GetFileName(testDialogVideoPath
                .Replace("dialogueid", testDialogueId));

             testDialogAudioCorrectFileName = testDialogueId + ".wav";
             
            if (!(await _sftpClient.IsFileExistsAsync("dialoguevideos/" + testDialogVideoCorrectFileName)))
                await _sftpClient.UploadAsync(testDialogVideoPath, "dialoguevideos/", testDialogVideoCorrectFileName);
        }

        protected override async Task CleanTestData()
        {
            await _sftpClient.DeleteFileIfExistsAsync("dialoguevideos/" + testDialogVideoCorrectFileName);
            await _sftpClient.DeleteFileIfExistsAsync("dialogueaudios/" + testDialogAudioCorrectFileName);            
        }

        protected override void InitServices()
        {
            _repository = ServiceProvider.GetService<IGenericRepository>();
            _videoToSoundService = ServiceProvider.GetService<VideoToSound>();
        }

        [Test]
        public async Task CheckSoundFilePresents()
        {
            await _videoToSoundService.Run("dialoguevideos/" + testDialogVideoCorrectFileName);

            _sftpClient.ChangeDirectoryToDefault();
            
            Assert.IsTrue(await _sftpClient.IsFileExistsAsync("dialogueaudios/" + testDialogAudioCorrectFileName));
        }
    }
}