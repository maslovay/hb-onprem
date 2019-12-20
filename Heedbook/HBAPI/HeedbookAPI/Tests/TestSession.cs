
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
using UserOperations.Services;
using HBData.Repository;

namespace UserOperations.Controllers
{
    [InProcess]
    [MemoryDiagnoser]
    public class TestSession : ServiceTest
    {
        private Startup _startup { get; set; }
        private RecordsContext _context { get; set; }
        private IGenericRepository _repository {get; set;}
        private DBOperations _dbOperation { get; set; }
        private ElasticClient _log { get; set; }
        public IConfiguration _config { get; private set; }
        public ServiceCollection _services { get; private set; }
        public ServiceProvider _serviceProvider { get; private set; }

        private SessionController _sessionController;

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
        [Benchmark(Description = "SessionStatus 1")]
        // public void TestSessionStatusMethod_1()
        // {
        //     _sessionController = new SessionController(_context, _config, _dbOperation, _log );
        //     _sessionController.SessionStatus(new Guid(N));
        // }

        // [Benchmark(Description = "SessionStatus 2")]
        // public void TestSessionStatusMethod_2()
        // {
        //     _sessionController = new SessionController(_context, _config, _dbOperation, _log);
        //   //  _sessionController.SessionStatus2(new Guid(N));
        // }

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