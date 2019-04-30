using NUnit.Framework;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using ExtractFramesFromVideo;
using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Base;

namespace ExtractFramesFromVideoService.Tests
{
    [TestFixture]
    public class FramesFromVideoIntegrationTests
    {
        private SftpClient _ftpClient;
        private FramesFromVideo _framesFromVideo;
        private IConfiguration _config;
        private RecordsContext _context;
        private IGenericRepository _repository;
        private ElasticClient _elasticClient;
        private FFMpegSettings _settings;
        private FFMpegWrapper _wrapper;
        private string videoFileName;
        private string correctFileName;
        private DateTime minDate;
        private DateTime maxDate;
        public Guid TestUserId => Guid.Parse("fff3cf0e-cea6-4595-9dad-654a60e8982f");
        private const string _testFilePattern = "testuser*.mkv";
        
        [SetUp]
        public void Setup()
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();
           
            var serviceCollection = new ServiceCollection()
                .AddEntityFrameworkNpgsql()
                .BuildServiceProvider();
            
            var builder = new DbContextOptionsBuilder<RecordsContext>();
            builder.UseNpgsql(_config.GetSection("ConnectionStrings")["DefaultConnection"])
                .UseInternalServiceProvider(serviceCollection);
            
            _context = new RecordsContext(builder.Options);
                        
            _repository = new GenericRepository(_context);
            _elasticClient = new ElasticClient(new ElasticSettings()
            {
                Host = _config.GetSection("ElasticSettings")["Host"],
                Port = int.Parse(_config.GetSection("ElasticSettings")["Port"]),
                FunctionName = _config.GetSection("ElasticSettings")["FunctionName"]
            });
            
            _ftpClient = new SftpClient(new SftpSettings()
            {
                Host = _config.GetSection("SftpSettings")["Host"],
                Port = int.Parse(_config.GetSection("SftpSettings")["Port"]),
                UserName = _config.GetSection("SftpSettings")["UserName"],
                Password = _config.GetSection("SftpSettings")["Password"],
                DestinationPath = _config.GetSection("SftpSettings")["DestinationPath"],
                DownloadPath = _config.GetSection("SftpSettings")["DownloadPath"]
            });
            
            PrepareDirectories();

            _settings = new FFMpegSettings()
            {
                FFMpegPath = _config.GetSection("FFMpegSettings")["FFMpegPath"],
                LocalVideoPath = _config.GetSection("FFMpegSettings")["LocalVideoPath"]
            };
                
            _wrapper = new FFMpegWrapper(_settings);
            
            _framesFromVideo = new FramesFromVideo(_ftpClient, 
                _repository,
                new NotificationHandler(), 
                _elasticClient,
                _settings,
                _wrapper);
            
            CleanDatabaseRecords();
        }

        private async void PrepareDirectories()
        {
            var rootDir  = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var resourceVideos = Directory.GetFiles(Path.Combine(rootDir, "Resources"), "testuser*.mkv");

            if (resourceVideos.Length == 0)
                throw new Exception("No video for testing!");

            videoFileName = Path.GetFileName(resourceVideos.First());

            correctFileName = videoFileName.Replace("testuser", TestUserId.ToString());
            
            await _ftpClient.UploadAsync(Path.Combine(rootDir, "Resources", videoFileName), "videos", correctFileName);
            
            var videoTimestampText = videoFileName.Split(("_"))[1];
            minDate = DateTime.ParseExact(videoTimestampText, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            maxDate = minDate.AddSeconds(30);
        }

        private void CleanDatabaseRecords()
        {
            var fileFrames = _repository.Get<FileFrame>()
                .Where(f => f.FileName.Contains(TestUserId.ToString()) && f.Time >= minDate && f.Time <= maxDate);

            foreach (var ff in fileFrames)
                _repository.Delete(ff);
            
            _repository.Save();
        }
        
        [TearDown]
        public void TearDown()
        {
            CleanDatabaseRecords();
        }

        [Test(Description = "Framing test")]
        public async Task RunTest()
        {
            var runTask = _framesFromVideo.Run(correctFileName);
            Task.WaitAll(runTask);
            Assert.True(_repository.Get<FileFrame>()
                .Any(f => f.FileName.Contains(TestUserId.ToString()) && f.Time >= minDate && f.Time <= maxDate));
            var checkTask = _ftpClient.ListDirectoryFiles("frames", TestUserId.ToString()+"*.jpg");
            Task.WaitAll(checkTask);
            var framesOnServer = checkTask.Result;
            Assert.True(framesOnServer.Count > 0);
        }
    }
}