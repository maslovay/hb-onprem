using System;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using ServiceExtensions;

namespace UserService
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
                .UseStartup<Startup>()
                .ConfigureBuilderDueToEnvironment(args: args, portToReassignForTests:5133);            
        }
    }
}