using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MemoryDbEventBus
{
    public static class StartupExtensions
    {
        public static void AddMemoryDbEventBus(this IServiceCollection services, IConfiguration config)
        {
            services.AddOptions();

            var memDbHost = config.GetSection("MemoryCacheDb").GetValue<string>("Host");
            var memDbPort = config.GetSection("MemoryCacheDb").GetValue<int>("Port");
            var memAllowAdmin = config.GetSection("MemoryCacheDb").GetValue<bool>("AllowAdmin");
            var connString = $"{memDbHost}:{memDbPort}, _allowAdmin:{memAllowAdmin}";
            
            services.AddSingleton<IMemoryCache, RedisMemoryCache>(opt => new RedisMemoryCache(connString));
            services.AddSingleton<IMemoryDbPublisher, MemoryCachePublisher>();
        }
    }
}