using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExtractFramesFromVideo;
using ExtractFramesFromVideo.Handler;
using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Base;
using NUnit.Framework;
using RabbitMqEventBus.Events;

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
        private string appUserId;
        private string videoFileName;
        private DateTime minDate;
        private DateTime maxDate;
        private string _localVideoPath;
        private string _localFramesPath;
        private string _localTempPath;
        private const string _testFilePattern = "testuser*.mkv";
        
        [SetUp]
        public void Init()
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
            //_context.Database.Migrate();
                        
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
            
            _framesFromVideo = new FramesFromVideo(_ftpClient, 
                _repository,
                new NotificationHandler(), 
                _elasticClient,
                _localVideoPath,
                _localFramesPath);
            
         //   CleanDatabaseRecords();
        }

        private async void PrepareDirectories()
        {
            _localVideoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                _config.GetSection("FFMpegSettings")["LocalVideoPath"]);
            _localFramesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                _config.GetSection("FFMpegSettings")["LocalFramesPath"]);
            _localTempPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                _config.GetSection("TestSettings")["LocalTempPath"]);
            
            var rootDir  = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            var testFiles = Directory.GetFiles(Path.Combine(rootDir, "Resources"), _testFilePattern);
            if (testFiles.Length == 0)
                throw new Exception("No test video file was presented!");
            
            foreach (var file in testFiles)
                File.Copy(file, _localVideoPath, true);

            videoFileName = Path.GetFileName(testFiles.FirstOrDefault());

            //await _ftpClient.UploadAsync(Path.Combine(_localVideoPath, videoFileName), "videos", videoFileName);
            
            appUserId = videoFileName.Split(("_"))[0];

            var videoTimestampText = videoFileName.Split(("_"))[1];
            minDate = DateTime.ParseExact(videoTimestampText, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            maxDate = minDate.AddSeconds(30);
        }

        private void CleanDatabaseRecords()
        {
            var fileFrames = _repository.Get<FileFrame>()
                .Where(f => f.FileName.Contains(appUserId) && f.Time >= minDate && f.Time <= maxDate);

            foreach (var ff in fileFrames)
                _repository.Delete(ff);
            
            _repository.Save();
        }

        [TearDown]
        public void Dispose()
        {
            CleanDatabaseRecords();
        }

        [Test]
        public async Task RunTest()
        {
            await _framesFromVideo.Run(videoFileName);
            
            Assert.IsTrue(_repository.Get<FileFrame>()
                .Any(f => f.FileName.Contains(appUserId) && f.Time >= minDate && f.Time <= maxDate));

            await _ftpClient.ListDirectoryFiles("frames", "testuser*.mkv");
        }
    }
}