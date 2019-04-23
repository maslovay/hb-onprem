using System;
using System.IO;
using System.Linq;
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

        private Action _additionalInitialization;

        private Guid testUserId;
        
        public void Setup( Action additionalInitialization, bool prepareTestData = false )
            
        {
            testUserId = Guid.Parse("fff3cf0e-cea6-4595-9dad-654a60e8982f");
         
            Config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();

            _additionalInitialization = additionalInitialization;
         
            InitServiceProvider();
            InitGeneralServices();
            InitServices();

            if (prepareTestData)
                PrepareTestData();
        }

        private async void PrepareTestData()
        {
            var currentDir = Environment.CurrentDirectory;
            var testVideoFilepath = Directory
                .GetFiles(Path.Combine(currentDir, "Resources/videos"), "testid*.mkv").FirstOrDefault();

            if (testVideoFilepath == null)
                throw new Exception("Can't get a test video for preparing a testset!");
            
            var testVideoFilename = Path.GetFileName(testVideoFilepath);
            
            var testVideoCorrectFileName = testVideoFilename?.Replace("testid", testUserId.ToString());

            if (!(await _sftpClient.IsFileExistsAsync("videos/" + testVideoCorrectFileName)))
            {
                _sftpClient.UploadAsync(testVideoFilename, "videos/", testVideoCorrectFileName);
            }
            // TODO: init db records....
        }
        
        private void InitServiceProvider()
        {
            Services = new ServiceCollection();

            Services.AddDbContext<RecordsContext>(options =>
            {
                var connectionString = Config.GetSection("ConnectionStrings")["DefaultConnection"];
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(HBData)));
            });

            Services.Configure<SftpSettings>(Config.GetSection(nameof(SftpSettings)));
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
    }
}