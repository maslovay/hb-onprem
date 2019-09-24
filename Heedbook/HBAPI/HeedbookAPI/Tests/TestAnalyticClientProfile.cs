using Microsoft.Extensions.Configuration;
using UserOperations.Services;
using HBData;
using HBLib.Utils;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Common;
using System.Threading.Tasks;
using UserOperations.Utils;
using System;
using Microsoft.EntityFrameworkCore;
using HBLib;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace UserOperations.Controllers.Test
{
    [InProcess]
    [MemoryDiagnoser]
    public class TestAnalyticClientProfile : ServiceTest
    {
        private Startup _startup { get; set; }
        private RecordsContext _context { get; set; }
        private DBOperations _dbOperation { get; set; }
        private  ILoginService _loginService { get; set; }
        private  RequestFilters _requestFilters { get; set; }
        private ElasticClient _log { get; set; }
        public IConfiguration _config { get; private set; }
        public ServiceCollection _services { get; private set; }
        public ServiceProvider _serviceProvider { get; private set; }
     //   public SftpClient _sftpClient { get; private set; }

        private TestController _testController;

        [GlobalSetup]
        public void GlobalSetup()
        {
            InitServiceProvider();
            InitServices();
            _startup = new Startup(_config);
            _startup.ConfigureServices(_services);
        }

        [Params("caa7fc74-676b-4b03-9fd9-eb401cc8e517", "3e5aa72a-8a04-41c2-8960-e79cd4471d7f",
            "30e8682b-45a7-4757-97f9-fe65b2161921", "77df387e-17bc-46a4-b464-47b77be01074",
            "eff7ec92-ea70-4407-950e-3b0d07738406", "6f0b08a1-45af-4ac7-a306-5a40612d6053", "178bd1e8-e98a-4ed9-ab2c-ac74734d1903",
            "0d1127c8-750e-40fa-a84e-f7647ab8af97", "35feb4f3-c68a-49a5-a7a9-54631e5ffc9f")]
        public string N;
        [Benchmark(Description = "Origin")]
        public void TestMethod_1()
        {
            Guid appUserId = new Guid(N);
            var token = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJpbmZvQGhlZWRib29rLmNvbSIsImp0aSI6IjYyOGZlYTJhLWI5ZjYtNDJjNy1hMThiLWE2N2M0NDFhYjUzYSIsImFwcGxpY2F0aW9uVXNlcklkIjoiNDk4YTliNzYtMzBjYS00MTVmLTk0OTctNWE4NGY5MzE2MDllIiwiYXBwbGljYXRpb25Vc2VyTmFtZSI6ItCi0LXRgdGC0L7QstGL0LkiLCJjb21wYW55TmFtZSI6ImhlZWRib29rIiwiY29tcGFueUlkIjoiZGZlODQxYzctMjBlNy00ZjI1LThlNjctNDAwYjQzNjZjZTI3IiwiY29ycG9yYXRpb25JZCI6IjQyNDAyMzU1LWVmN2MtNDFiZC1iMjhlLTQyMzRhODg5YzNiYSIsImxhbmd1YWdlQ29kZSI6IjIiLCJyb2xlIjoiTWFuYWdlciIsImV4cCI6MTU3MTM5OTI2OCwiaXNzIjoiaHR0cHM6Ly9oZWVkYm9vay5jb20iLCJhdWQiOiJodHRwczovL2hlZWRib29rLmNvbSJ9.sYf-Es10P1gfsIwx1EyxpGx5M4J0QmleOGUOgIHrPVs";
            _testController = new TestController(_config, _loginService, _context, _dbOperation, _requestFilters, _sftpClient );
            _testController.DialoguePaginatedGet("20190807", "20190809",new List<Guid> { appUserId },null,null,null,null, null, token);
        }

        [Benchmark(Description = "Test")]
        public void TestMethod_2()
        {
            Guid appUserId = new Guid(N);
            _testController = new TestController(_config, _loginService, _context, _dbOperation, _requestFilters, _sftpClient);
            var token = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJpbmZvQGhlZWRib29rLmNvbSIsImp0aSI6IjYyOGZlYTJhLWI5ZjYtNDJjNy1hMThiLWE2N2M0NDFhYjUzYSIsImFwcGxpY2F0aW9uVXNlcklkIjoiNDk4YTliNzYtMzBjYS00MTVmLTk0OTctNWE4NGY5MzE2MDllIiwiYXBwbGljYXRpb25Vc2VyTmFtZSI6ItCi0LXRgdGC0L7QstGL0LkiLCJjb21wYW55TmFtZSI6ImhlZWRib29rIiwiY29tcGFueUlkIjoiZGZlODQxYzctMjBlNy00ZjI1LThlNjctNDAwYjQzNjZjZTI3IiwiY29ycG9yYXRpb25JZCI6IjQyNDAyMzU1LWVmN2MtNDFiZC1iMjhlLTQyMzRhODg5YzNiYSIsImxhbmd1YWdlQ29kZSI6IjIiLCJyb2xlIjoiTWFuYWdlciIsImV4cCI6MTU3MTM5OTI2OCwiaXNzIjoiaHR0cHM6Ly9oZWVkYm9vay5jb20iLCJhdWQiOiJodHRwczovL2hlZWRib29rLmNvbSJ9.sYf-Es10P1gfsIwx1EyxpGx5M4J0QmleOGUOgIHrPVs";

            _testController.DialoguePaginatedTestGet("20190915", "20190919", new List<Guid> { appUserId }, null, null, null, null, null, token);
        }

        protected override Task PrepareTestData()
        {
            throw new System.NotImplementedException();
        }

        protected override Task CleanTestData()
        {
            throw new System.NotImplementedException();
        }

        protected override void InitServices()
        {
            _context = _serviceProvider.GetService<RecordsContext>();
            _log = _serviceProvider.GetService<ElasticClient>();
            _loginService = _serviceProvider.GetService<LoginService>();
            _dbOperation = new DBOperations(_context, _config);
            _requestFilters = new RequestFilters(_context, _config);
        //    _sftpClient = new SftpClient(_sftpSettings, _config);
        }

        private void InitServiceProvider()
        {
            _config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();

            _services = new ServiceCollection();
            _services.AddDbContext<RecordsContext>(options =>
            {
                var connectionString = _config.GetConnectionString("DefaultConnection");
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(UserOperations)));
            });
            _services.Configure<ElasticSettings>(_config.GetSection(nameof(ElasticSettings)));
            _services.AddSingleton(provider => provider.GetRequiredService<IOptions<ElasticSettings>>().Value);
            _services.AddSingleton(provider =>
            {
                var settings = provider.GetRequiredService<IOptions<ElasticSettings>>().Value;
                return new ElasticClient(settings);
            });
            _services.AddScoped<DBOperations>();
            _services.AddScoped(typeof(ILoginService), typeof(LoginService));
            _services.AddScoped(typeof(RequestFilters));
            //     services.AddSingleton(Config);
            _serviceProvider = _services.BuildServiceProvider();
        }
    }
}