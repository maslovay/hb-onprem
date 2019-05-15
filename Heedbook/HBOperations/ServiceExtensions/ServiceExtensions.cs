using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;
using UnitTestExtensions;

namespace ServiceExtensions
{
    public static class ServiceExtensions
    {
        public static IWebHostBuilder ConfigureBuilderDueToEnvironment(this IWebHostBuilder builder, 
            string[] args = null, int portToReassignForTests = 5000)
        {
            builder.ConfigureAppConfiguration((context, cfg) =>
            {
                var env = context.HostingEnvironment;

                var configBuilder = cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                if (UnitTestDetector.IsRunningFromNUnit)
                {
                    builder.UseUrls("http://127.0.0.1:" + portToReassignForTests);
                    builder.UseConfiguration(configBuilder.Build());
                    return;
                }

                if (env.IsDevelopment())
                    configBuilder.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true,
                        reloadOnChange: true);
                else
                {
                    builder.UseUrls("http://127.0.0.1:" + portToReassignForTests);
                    configBuilder.AddJsonFile($"appsettings.test.json", optional: true, reloadOnChange: true);
                }

                if (args != null)
                    configBuilder.AddCommandLine(args);
                
                builder.UseConfiguration(configBuilder.Build());
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