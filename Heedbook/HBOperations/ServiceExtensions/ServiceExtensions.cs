using System;
using System.Linq;
using System.Reflection;
using System.Threading;
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
                
                Console.WriteLine($">>>>>>>>>>>>>>>>>>>> SERVICE {Assembly.GetCallingAssembly().GetName().Name} RUNNING");
                
                if (UnitTestDetector.IsRunningFromNUnit || args.Contains("--isCalledFromUnitTest") && args.Contains("true"))
                {
                    Console.WriteLine($">>>>>>>>>>>>>>>>>>>> TEST RUNNING Port: {portToReassignForTests}");
                    builder.UseUrls("http://127.0.0.1:" + portToReassignForTests);
                    configBuilder.AddJsonFile($"appsettings.test.json", optional: true, reloadOnChange: true);
                    builder.UseConfiguration(configBuilder.Build());
                    return;
                }
                
                Console.WriteLine($">>>>>>>>>>>>>>>>>>>> IsDevelopment: {env.IsDevelopment()}");
                Console.WriteLine($">>>>>>>>>>>>>>>>>>>> IsProduction: {env.IsDevelopment()}");

                if (Environment.GetEnvironmentVariable("TESTCLUSTER") == "true")
                {
                    Console.WriteLine($">>>>>>>>>>>>>>>>>>>> TESTCLUSTER: true");
                    Console.WriteLine($">>>>>>>>>>>>>>>>>>>> URLS: {Environment.GetEnvironmentVariable("ASPNETCORE_URLS")}");
                    builder.UseUrls(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"));
                    configBuilder.AddJsonFile($"appsettings.test.json", optional: true, reloadOnChange: true);
                    builder.UseConfiguration(configBuilder.Build());
                }

                Console.WriteLine($">>>>>>>>>>>>>>>>>>>> TESTCLUSTER: false");

                
                if (env.IsDevelopment())
                    configBuilder.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true,
                        reloadOnChange: true);
                else if (env.IsProduction())
                {
                    Console.WriteLine($">>>>>>>>>>>>>>>>>>>> URLS: {Environment.GetEnvironmentVariable("ASPNETCORE_URLS")}");
                    builder.UseUrls(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"));
                    return;
                }
                else
                {
                    Console.WriteLine($">>>>>>>>>>>>>>>>>>>> URLS: {"http://127.0.0.1:" + portToReassignForTests}");
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