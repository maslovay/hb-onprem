using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using ServiceExtensions;


namespace DialoguesRecalculateScheduler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .ConfigureBuilderDueToEnvironment(args: args, portToReassignForTests: 5075)
            .UseStartup<Startup>();
    }
}