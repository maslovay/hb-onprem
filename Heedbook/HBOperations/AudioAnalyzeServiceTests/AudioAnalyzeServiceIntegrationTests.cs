using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using UnitTestExtensions;

namespace AudioAnalyzeService.Tests
{
    [TestFixture]
    public class AudioAnalyzeServiceIntegrationTests : ServiceTest
    {
        private AudioAnalyze _audioAnalyzeService;
        private Startup _startup;
        private GoogleConnector _googleConnector;
        private ElasticClientFactory _elasticClientFactory;
        private FFMpegWrapper _ffmpegWrapper;
        private AsrHttpClient.AsrHttpClient _asrClient;
        private string testDialogVideoCorrectFileName;
        private string testDialogAudioCorrectFileName;
        private Dialogue testDialog;


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
        public new async Task TearDown()
        {
            await base.TearDown();
        }

        protected override async Task PrepareTestData()
        {
            var currentDir = Environment.CurrentDirectory;

            testDialog = CreateNewTestDialog();
            
            var testDialogVideoPath = Directory
                .GetFiles(Path.Combine(currentDir, "Resources/DialogueVideos"), "dialogueid.mkv")
                .FirstOrDefault();

            var testDialogAudioPath = Directory
                .GetFiles(Path.Combine(currentDir, "Resources/DialogueAudios"), "dialogueid.wav")
                .FirstOrDefault();
            
            if (testDialogVideoPath == null || testDialogAudioPath == null)
                throw new Exception("Can't get a test DialogueVideo/Audio for preparing a testset!");

            testDialogVideoCorrectFileName = Path.GetFileName(testDialogVideoPath.Replace("dialogueid", 
                testDialog.DialogueId.ToString()));

            testDialogAudioCorrectFileName = Path.GetFileName(testDialogVideoPath.Replace("dialogueid", 
                testDialog.DialogueId.ToString()));
            
            if (!await _sftpClient.IsFileExistsAsync("dialoguevideos/" + testDialogVideoCorrectFileName))
                await _sftpClient.UploadAsync(testDialogVideoPath, "dialoguevideos/", 
                    testDialogVideoCorrectFileName);
            
            if (!await _sftpClient.IsFileExistsAsync("dialogueaudios/" + testDialogAudioCorrectFileName))
                await _sftpClient.UploadAsync(testDialogAudioPath, "dialogueaudios/", 
                    testDialogAudioCorrectFileName);

            await _repository.CreateAsync(testDialog);
            await _repository.SaveAsync();
        }

        protected override async Task CleanTestData()
        {
            await _sftpClient.DeleteFileIfExistsAsync("dialoguevideos/" + testDialogVideoCorrectFileName);
            await _sftpClient.DeleteFileIfExistsAsync("dialogueaudios/" + testDialogAudioCorrectFileName);
            
            if (_repository.Get<Dialogue>().Any(d => d.DialogueId == testDialog.DialogueId))
            {
                _repository.Delete(testDialog);
                await _repository.SaveAsync();
            }
        }

        protected override void InitServices()
        {
            _repository = ServiceProvider.GetService<IGenericRepository>();
            _elasticClientFactory = ServiceProvider.GetService<ElasticClientFactory>();
            
            _ffmpegWrapper = ServiceProvider.GetService<FFMpegWrapper>();
            _asrClient = ServiceProvider.GetService<AsrHttpClient.AsrHttpClient>();
            _googleConnector = ServiceProvider.GetService<GoogleConnector>();
            _audioAnalyzeService = new AudioAnalyze(ScopeFactory, _asrClient, _elasticClientFactory, _googleConnector, _sftpClient);
        }
        
        [Test, Retry(3)]
        public async Task AudioAnalyzerCreatesDbRecords()
        {
            _sftpClient.ChangeDirectoryToDefault();
            await _audioAnalyzeService.Run("dialogueaudios/" + testDialogAudioCorrectFileName);
            Assert.True( _repository.Get<FileAudioDialogue>().Any(fad => fad.DialogueId == testDialog.DialogueId) );
        }
    }
}