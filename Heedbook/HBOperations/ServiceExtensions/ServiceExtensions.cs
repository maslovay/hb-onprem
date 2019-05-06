using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;
using UnitTestExtensions;

namespace ServiceExtensions
{
    public static class ServiceExtensions
    {
        public static IWebHostBuilder ConfigureBuilderDueToEnvironment(this IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, cfg) =>
            {
                var env = context.HostingEnvironment;

                var configBuilder = cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                if (UnitTestDetector.IsRunningFromNUnit) 
                    return;
                
                if (env.IsDevelopment())
                    configBuilder.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true,
                        reloadOnChange: true);
                else
                    configBuilder.AddJsonFile($"appsettings.test.json", optional: true, reloadOnChange: true);
            });
            return builder;
        }

        public static IConfigurationBuilder ConfigureBuilderForTests(this IConfigurationBuilder builder)
        {
            if (!UnitTestDetector.IsRunningFromNUnit) 
                return builder;
            
            var configBuilder = builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.test.json", optional: true, reloadOnChange: true);
            return configBuilder;

        }
    }
}