﻿using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMqEventBus.Events;

namespace ExtractFramesFromVideoService.Tests
{
    [TestFixture]
    public class FramesFromVideoIntegrationTests
    {
        private SftpClient _ftpClient;
        private IConfiguration _config;
        private RecordsContext _context;
        private IGenericRepository _repository;
        private string videoFileName;
        private string correctFileName;
        private Guid deviceId = Guid.Parse("9c7cbe18-517d-47b1-ab58-ffb813a3ba2e");
        private DateTime minDate;
        private DateTime maxDate;
        public Guid TestUserId => Guid.Parse("fff3cf0e-cea6-4595-9dad-654a60e8982f");
        private const string _testFilePattern = "testuser*.mkv";
        private Process _userServiceProcess;
        private Process _extractServiceProcess;
        private string _currentUri;
        
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

            _currentUri = "http://localhost:5133";
            
            _context = new RecordsContext(builder.Options);
            _repository = new GenericRepository(_context);

            _ftpClient = new SftpClient(new SftpSettings()
            {
                Host = _config.GetSection("SftpSettings")["Host"],
                Port = int.Parse(_config.GetSection("SftpSettings")["Port"]),
                UserName = _config.GetSection("SftpSettings")["UserName"],
                Password = _config.GetSection("SftpSettings")["Password"],
                DestinationPath = _config.GetSection("SftpSettings")["DestinationPath"],
                DownloadPath = _config.GetSection("SftpSettings")["DownloadPath"]
            }, _config);
            
            PrepareDirectories();
            CleanDatabaseRecords();
            
            RunServices();
        }

        private async void PrepareDirectories()
        {
            var rootDir  = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var resourceVideos = Directory.GetFiles(Path.Combine(rootDir, "Resources"), "testuser*.mkv");

            if (resourceVideos.Length == 0)
                throw new Exception("No video for testing!");

            videoFileName = Path.GetFileName(resourceVideos.First());

            correctFileName = videoFileName.Replace("testuser", TestUserId.ToString());
            
            if (!await _ftpClient.IsFileExistsAsync(Path.Combine("videos", correctFileName)))
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

        private void RunServices()
        {
            var config = "Release";

#if DEBUG
            config = "Debug";
#endif
            _userServiceProcess = Process.Start("dotnet",
                $"../../../../UserService/bin/{config}/netcoreapp2.2/UserService.dll --isCalledFromUnitTest true");
            _extractServiceProcess = Process.Start("dotnet",
                $"../../../../ExtractFramesFromVideoService/bin/{config}/netcoreapp2.2/ExtractFramesFromVideoService.dll --isCalledFromUnitTest true");
            Thread.Sleep(60000);
        }

        private void StopServices()
        {
            try
            {
                _extractServiceProcess.Kill();
                _userServiceProcess.Kill();
            }
            catch (Exception ex)
            {
                // doing nothing...
            }
        }

        private void SendRequest(string body)
        {
            using (var wc = new WebClient())
            {
                wc.UseDefaultCredentials = true;
                wc.Headers.Add("Content-Type", "application/json");
                wc.UploadData(_currentUri+"/user/FramesFromVideo", Encoding.UTF8.GetBytes(body.ToLower()));
            }
        }
        
        [Test(Description = "Framing test"), Retry(3)]
        public async Task RunTest()
        {
            var framesFromVideoRun = new FramesFromVideoRun()
            {
                Path = $"videos/{correctFileName}",
                deviceId = deviceId
            };

            var json = JsonConvert.SerializeObject(framesFromVideoRun);

            Thread.Sleep(3000);
            
            SendRequest(json);

            Thread.Sleep(20000);

            Assert.True(_repository.Get<FileFrame>()
                .Any(f => f.FileName.Contains(TestUserId.ToString()) && f.Time >= minDate && f.Time <= maxDate));

            _ftpClient.ChangeDirectoryToDefault();
            var checkTask = _ftpClient.ListDirectoryFiles("frames", TestUserId.ToString());
            Task.WaitAll(checkTask);
            var framesOnServer = checkTask.Result;
            Assert.True(framesOnServer.Count > 0);

            StopServices();
        }
    }
}