using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HBData;
using HBData.Models;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.AspNetCore.Antiforgery.Internal;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;
using ServiceExtensions;

namespace Common
{
    public abstract class ServiceTest
    {
        protected IGenericRepository _repository;
        protected SftpClient _sftpClient;
        
        public IConfiguration Config { get; private set; }
        public ServiceCollection Services { get; private set; }
        public ServiceProvider ServiceProvider { get; private set; }

        public IServiceScopeFactory ScopeFactory { get; private set; }

        private const string FileFrameWithDatePattern = @"(.*)_([0-9]*)";

        private const string FileVideoWithDatePattern = @"(.*)_([0-9]*)_(.*)";
        
        private Action _additionalInitialization;
        
        public Guid TestUserId => Guid.Parse("fff3cf0e-cea6-4595-9dad-654a60e8982f");

        private string testCompanyName = "TESTCOMPANY_89083091293392183";

        private string testIndustryName = "TESTINDUSTRY";

        public async Task Setup( Action additionalInitialization, bool prepareTestData = false )

        {
            Config = new ConfigurationBuilder()
                    .ConfigureBuilderForTests()
                    .Build();

            _additionalInitialization = additionalInitialization;
         
            InitServiceProvider();
            InitGeneralServices();
            InitServices();
            PrepareDatabase();
            
            if (prepareTestData)
                await PrepareTestData();
        }

        public async Task TearDown()
        {
            await CleanTestData();
        }
        
        protected abstract Task PrepareTestData();

        protected abstract Task CleanTestData();

        private void PrepareDatabase()
        {
            var industry = _repository.Get<CompanyIndustry>().FirstOrDefault(c => c.CompanyIndustryName == testIndustryName);

            if (industry == default(CompanyIndustry))
            {
                industry = new CompanyIndustry()
                {
                    CompanyIndustryId = Guid.NewGuid(),
                    CompanyIndustryName = testIndustryName,
                    SatisfactionIndex = 0,
                    LoadIndex = 0.5,
                    CrossSalesIndex = 0.04
                };

                _repository.AddOrUpdate(industry);
            }

            var company = _repository.Get<Company>().FirstOrDefault(c => c.CompanyName == testCompanyName);

            if (company == default(Company))
            {
                company = new Company()
                {
                    CompanyId = Guid.NewGuid(),
                    CompanyName = testCompanyName,
                    CompanyIndustryId = industry.CompanyIndustryId,
                    CreationDate = DateTime.Now
                };

                _repository.AddOrUpdate(company);
            }

            var appUser = _repository.Get<ApplicationUser>().FirstOrDefault(u => u.Id == TestUserId);
            
            if (appUser == default(ApplicationUser))
            {
                _repository.AddOrUpdate(new ApplicationUser()
                {
                    Id = TestUserId,
                    UserName = "Test",
                    NormalizedUserName = "TEST",
                    FullName = "Test",
                    AccessFailedCount = 1000,
                    Avatar = null,
                    CompanyId = company.CompanyId,
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    CreationDate = DateTime.Now,
                    Email = "test@test.ru",
                    EmailConfirmed = true,
                    StatusId = 3,
                    EmpoyeeId = "99999"
                });
            }
            else
            {
                appUser.CompanyId = company.CompanyId;
                _repository.Update(appUser);
            }
            
            _repository.Save();
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
            Services.AddTransient(provider => provider.GetRequiredService<IOptions<SftpSettings>>().Value);
            Services.AddTransient<SftpClient>();
            Services.AddScoped<IGenericRepository, GenericRepository>();
            
            _additionalInitialization?.Invoke();
            ServiceProvider = Services.BuildServiceProvider();
        }

        private void InitGeneralServices()
        {           
            _sftpClient = ServiceProvider.GetRequiredService<SftpClient>();
            _repository = ServiceProvider.GetRequiredService<IGenericRepository>();
            ScopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        }
        
        protected abstract void InitServices();

        protected Dialogue CreateNewTestDialog()
            => CreateNewTestDialog(Guid.NewGuid());

        protected Dialogue CreateNewTestDialog(Guid dialogId)
            => new Dialogue
            {
                DialogueId = dialogId,
                CreationTime = DateTime.Now.AddYears(-1),
                BegTime = DateTime.Now.AddYears(-1),
                EndTime = DateTime.Now.AddYears(1),
                ApplicationUserId = TestUserId,
                LanguageId = null,
                StatusId = null,
                SysVersion = "",
                InStatistic = false,
                Comment = "test dialog!!!"
            };

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