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
        private string videoName;
        private DateTime minDate;
        private DateTime maxDate;
        
        [SetUp]
        public void SetUp()
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
            
            _framesFromVideo = new FramesFromVideo(_ftpClient, 
                _repository,
                new NotificationHandler(), 
                _elasticClient,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), _config.GetSection("FFMpegSettings")["LocalVideoPath"]),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), _config.GetSection("FFMpegSettings")["LocalFramesPath"])
                );
            
            PrepareVariables();
        }

        private void PrepareVariables()
        {
            videoName = _config.GetSection("TestFile")["VideoName"];
            appUserId = videoName.Split(("_"))[0];

            var videoTimestampText = videoName.Split(("_"))[1];
            minDate = DateTime.ParseExact(videoTimestampText, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            maxDate = minDate.AddSeconds(30);

            CleanDatabaseRecords();
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
        private void TearDown()
        {
            CleanDatabaseRecords();
        }

        [Test]
        public async Task RunTest()
        {
            await _framesFromVideo.Run(videoName);
            
            Assert.IsTrue(_repository.Get<FileFrame>()
                .Any(f => f.FileName.Contains(appUserId) && f.Time >= minDate && f.Time <= maxDate));
        }
    }
}