
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
using HBData.Repository;

namespace UserOperations.Controllers
{
    [InProcess]
    [MemoryDiagnoser]
    public class TestRepository : ServiceTest
    {
        //private Startup _startup { get; set; }
        private RecordsContext _context { get; set; }
        //private DBOperations _dbOperation { get; set; }
        //public IConfiguration _config { get; private set; }
        //public IServiceCollection _services { get; private set; }
        //public ServiceProvider _serviceProvider { get; private set; }

        private AnalyticCommonProvider analyticCommonProvider;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            //    InitServiceProvider();
                InitServices();
            //_services = new ServiceCollection();
            //_startup = new Startup(_config);
            //_startup.ConfigureServices(_services);
            // 
            await base.Setup(null);
        }

        [Params("ca2c3ed7-4b70-46f6-9054-91c49944e5ab")]//, "c9c2648c-58cf-41b3-9ed8-50a66deb8d61", "3a0f9ddb-7385-4c1d-95a3-c94e5b51cc20", "83d47a97-ef60-4b42-8459-40038a71a34f", "7fab0005-63d0-44b5-bbf5-4eda9bcfe4f9")]
        public string N;
        [Benchmark(Description = "GetDialogueIncludedFramesByIdAsync 1")]
        public void TestGetDialogueIncludedFramesByIdAsync_1()
        {
            if (_context == null || _repository == null) return;
            analyticCommonProvider = new AnalyticCommonProvider(_repository );
            Task t = analyticCommonProvider.GetDialogueIncludedFramesByIdAsync(new Guid(N));
            t.Wait();
        }

        [Benchmark(Description = "GetDialogueIncludedFramesByIdAsync 2")]
        public void TestGetDialogueIncludedFramesByIdAsync_2()
        {
            if (_context == null || _repository == null) return;
            analyticCommonProvider = new AnalyticCommonProvider(_repository);
           // analyticCommonProvider.GetDialogueIncludedFramesByIdAsync2(new Guid(N));
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
            _context = base.ServiceProvider.GetService<RecordsContext>();
            _repository = base.ServiceProvider.GetRequiredService<IGenericRepository>();
            //  _dbOperation = ServiceProvider.GetService<DBOperations>();
        }

        //private void InitServiceProvider()
        //{
        //    _config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();

        //    _services = new ServiceCollection();
        //    _services.AddScoped<IGenericRepository, GenericRepository>();
        //    _services.AddDbContext<RecordsContext>(options =>
        //    {
        //        var connectionString = _config.GetConnectionString("DefaultConnection");
        //        options.UseNpgsql(connectionString,
        //            dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(UserOperations)));
        //    });
        //    _services.AddSingleton(Config);
        //    _serviceProvider = _services.BuildServiceProvider();
        //}
    }
}