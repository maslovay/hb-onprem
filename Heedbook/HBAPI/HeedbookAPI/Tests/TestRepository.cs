
using System.Linq;
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
using UserOperations.Providers;

namespace UserOperations.Controllers
{
    [InProcess]
    [MemoryDiagnoser]
    public class TestRepository : ServiceTest
    {
        private Startup _startup { get; set; }
        private RecordsContext _context { get; set; }
        private DBOperations _dbOperation { get; set; }
        private ElasticClient _log { get; set; }
        public IConfiguration _config { get; private set; }
        public ServiceCollection _services { get; private set; }
        public ServiceProvider _serviceProvider { get; private set; }

        private AnalyticCommonProvider analyticCommonProvider;

        [GlobalSetup]
        public void GlobalSetup()
        {
            InitServiceProvider();
            InitServices();
            _startup = new Startup(_config);
            _startup.ConfigureServices(_services);
        }

        [Params("ca2c3ed7-4b70-46f6-9054-91c49944e5ab", "c9c2648c-58cf-41b3-9ed8-50a66deb8d61", "3a0f9ddb-7385-4c1d-95a3-c94e5b51cc20", "83d47a97-ef60-4b42-8459-40038a71a34f")]
        public string N;
        [Benchmark(Description = "GetDialogueIncludedFramesByIdAsync 1")]
        public async void TestGetDialogueIncludedFramesByIdAsync_1()
        {
            analyticCommonProvider = new AnalyticCommonProvider(_context, _repository );
            await analyticCommonProvider.GetDialogueIncludedFramesByIdAsync(new Guid(N));
        }

        [Benchmark(Description = "GetDialogueIncludedFramesByIdAsync 2")]
        public async void TestGetDialogueIncludedFramesByIdAsync_2()
        {
            analyticCommonProvider = new AnalyticCommonProvider(_context, _repository);
            await analyticCommonProvider.GetDialogueIncludedFramesByIdAsync2(new Guid(N));
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
          //  _dbOperation = ServiceProvider.GetService<DBOperations>();
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
            //     services.AddSingleton(Config);
            _serviceProvider = _services.BuildServiceProvider();
        }
    }
}