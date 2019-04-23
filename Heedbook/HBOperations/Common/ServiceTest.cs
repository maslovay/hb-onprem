using System;
using HBData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common
{
    public abstract class ServiceTest
    {
        public IConfiguration Config { get; private set; }
        public ServiceCollection Services { get; private set; }
        public ServiceProvider ServiceProvider { get; private set; }

        private Action _additionalInitialization;
        
        public void Setup( Action additionalInitialization )
            
        {
            Config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();

            _additionalInitialization = additionalInitialization;
            
            InitServiceProvider();
            InitServices();
        }

        protected void InitServiceProvider()
        {
            Services = new ServiceCollection();

            Services.AddDbContext<RecordsContext>(options =>
            {
                var connectionString = Config.GetSection("ConnectionStrings")["DefaultConnection"];
                options.UseNpgsql(connectionString,
                    dbContextOptions => dbContextOptions.MigrationsAssembly(nameof(HBData)));
            });

            _additionalInitialization?.Invoke();
            ServiceProvider = Services.BuildServiceProvider();
        }
        
        protected abstract void InitServices();
    }
}