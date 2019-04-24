﻿using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HBData;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common
{
    public abstract class ServiceTest
    {
        protected IGenericRepository _repository;
        protected SftpClient _sftpClient;
        
        public IConfiguration Config { get; private set; }
        public ServiceCollection Services { get; private set; }
        public ServiceProvider ServiceProvider { get; private set; }

        private const string FileFrameWithDatePattern = @"(.*)_([0-9]*)";

        private const string FileVideoWithDatePattern = @"(.*)_([0-9]*)_(.*)";
        
        private Action _additionalInitialization;
        
        public Guid TestUserId => Guid.Parse("fff3cf0e-cea6-4595-9dad-654a60e8982f");

        public async Task Setup( Action additionalInitialization, bool prepareTestData = false )
            
        {
            Config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();

            _additionalInitialization = additionalInitialization;
         
            InitServiceProvider();
            InitGeneralServices();
            InitServices();

            if (prepareTestData)
                await PrepareTestData();
        }

        protected abstract Task PrepareTestData();
        
        private void InitServiceProvider()
        {
            Services = new ServiceCollection();

            Services.AddDbContext<RecordsContext>(options =>
            {
                var connectionString = Config.GetSection("ConnectionStrings")["DefaultConnection"];
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(HBData)));
            });

            Services.Configure<SftpSettings>(options => Config.GetSection(nameof(SftpSettings)).Bind(options));
            Services.AddTransient<SftpClient>();
            Services.AddScoped<IGenericRepository, GenericRepository>();
            
            _additionalInitialization?.Invoke();
            ServiceProvider = Services.BuildServiceProvider();
        }

        private void InitGeneralServices()
        {           
            _sftpClient = ServiceProvider.GetService<SftpClient>();
            _repository = ServiceProvider.GetService<IGenericRepository>();
        }
        
        protected abstract void InitServices();

        public DateTime GetDateTimeFromFileFrameName(string inputFilePath) =>
            GetDateTimeUsingPattern(FileFrameWithDatePattern, inputFilePath);
        public DateTime GetDateTimeFromFileVideoName(string inputFilePath) =>
            GetDateTimeUsingPattern(FileVideoWithDatePattern, inputFilePath);

        private DateTime GetDateTimeUsingPattern(string pattern, string inputFilePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(inputFilePath);

            var dateTimeRegex = new Regex(pattern);
            
            if (dateTimeRegex.IsMatch(fileName))
            {
                var dateTimeString = dateTimeRegex.Match(inputFilePath).Groups[2].ToString();
                return DateTime.ParseExact(dateTimeString, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);                
            }
            
            throw new Exception("Incorrect filename for getting a DateTime!");
        }
        
    }
}