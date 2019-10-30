using System;
using AlarmSender;
using HBData;
using HBData.Repository;
using HBLib;
using HBLib.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiceExtensions;
using UserOperations.Providers;

namespace ApiTests
{
    public abstract class ApiServiceTest : IDisposable
    {
        protected IGenericRepository _repository;
        protected AnalyticHomeProvider _analyticHomeProvider;
        protected SftpClient _sftpClient;

        public IConfiguration Config { get; private set; }
        public ServiceCollection Services { get; private set; }
        public ServiceProvider ServiceProvider { get; private set; }

        public IServiceScopeFactory ScopeFactory { get; private set; }

        public void Setup()

        {
            Config = new ConfigurationBuilder()
                .ConfigureBuilderForTests()
                .Build();

            InitServiceProvider();
            InitGeneralServices();
            // PrepareDatabase();
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
            Services.AddSingleton(Config);
            Services.AddSingleton<TelegramSender>();

            Services.AddScoped<AnalyticContentProvider>();
            Services.AddScoped<AnalyticCommonProvider>();
            Services.AddScoped<AnalyticHomeProvider>();



            ServiceProvider = Services.BuildServiceProvider();
        }

        private void InitGeneralServices()
        {
            _sftpClient = ServiceProvider.GetRequiredService<SftpClient>();
            _repository = ServiceProvider.GetRequiredService<IGenericRepository>();
            ScopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            _analyticHomeProvider = ServiceProvider.GetService<AnalyticHomeProvider>();
        }

        public void Dispose()
        {
        }
    }
}