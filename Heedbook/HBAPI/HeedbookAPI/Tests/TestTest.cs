
using System.Linq;
using Microsoft.Extensions.Configuration;
using UserOperations.Services;
using HBData;
using HBLib.Utils;
using BenchmarkDotNet.Attributes;
using Common;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;
using ServiceExtensions;
using Microsoft.EntityFrameworkCore;

namespace UserOperations.Controllers
{
    [InProcess]
    [MemoryDiagnoser]
    public class TestTest : ServiceTest
    {
        private Startup _startup;
        private RecordsContext _context;
        private TestController _testController;
        public IConfiguration Config { get; private set; }
        public ServiceCollection services { get; private set; }
        public ServiceProvider serviceProvider { get; private set; }

        [GlobalSetup]
        public void GlobalSetup()
        {           
            InitServiceProvider();
            InitServices();
            _startup = new Startup(Config);
            _startup.ConfigureServices(services);
        }

        [Params("caa7fc74-676b-4b03-9fd9-eb401cc8e517", "3e5aa72a-8a04-41c2-8960-e79cd4471d7f",
            "30e8682b-45a7-4757-97f9-fe65b2161921", "77df387e-17bc-46a4-b464-47b77be01074",
            "eff7ec92-ea70-4407-950e-3b0d07738406", "6f0b08a1-45af-4ac7-a306-5a40612d6053", "178bd1e8-e98a-4ed9-ab2c-ac74734d1903",
            "0d1127c8-750e-40fa-a84e-f7647ab8af97", "35feb4f3-c68a-49a5-a7a9-54631e5ffc9f")]
        public string N;
        [Benchmark(Description = "Test 1")]
        public void TestSessionStatusMethod_1()
        {
            _testController = new TestController(_context);
            _testController.TestToList(new Guid(N));
        }

        [Benchmark(Description = "Test 2")]
        public void TestSessionStatusMethod_2()
        {
            _testController = new TestController(_context);
            _testController.TestWithoutToList(N);
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
            _context = serviceProvider.GetService<RecordsContext>();
        }

        private void InitServiceProvider()
        {
            Config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();

            services = new ServiceCollection();
            services.AddDbContext<RecordsContext>(options =>
            {
                var connectionString = Config.GetConnectionString("DefaultConnection");
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(UserOperations)));
            });

            //  services.Configure<SftpSettings>(Config.GetSection(nameof(SftpSettings)));
            // services.AddTransient(provider => provider.GetRequiredService<IOptions<SftpSettings>>().Value);
            // services.AddTransient<SftpClient>();

            //  services.AddScoped<IGenericRepository, GenericRepository>();
            //     services.AddSingleton(Config);
            //  services.AddSingleton<TelegramSender>();

            // _additionalInitialization?.Invoke();
            serviceProvider = services.BuildServiceProvider();
        }
    }
}