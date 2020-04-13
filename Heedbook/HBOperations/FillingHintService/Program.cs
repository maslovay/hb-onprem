using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using ServiceExtensions;

namespace FillingHintService
{
    public class Program
    {
        public static void Main(String[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(String[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureBuilderDueToEnvironment(args: args, portToReassignForTests: 5075)
                .UseStartup<Startup>();
        }
    }
}