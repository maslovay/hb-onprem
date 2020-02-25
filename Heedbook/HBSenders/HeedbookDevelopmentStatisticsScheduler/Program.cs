using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace HeedbookDevelopmentStatisticsScheduler
{
    public class Program
    {
        public static void Main(String[] args)
        {
            Console.WriteLine("Запуск Main");
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(String[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                          .UseStartup<Startup>();
        }
    }
}