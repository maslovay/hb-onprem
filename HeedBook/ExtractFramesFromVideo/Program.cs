using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExtractFramesFromVideo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("#");
            Console.WriteLine("Use next line: ");
            Console.WriteLine("https://localhost:5001/api/values?videoblobname=178bd1e8-e98a-4ed9-ab2c-ac74734d1903_20190116082942_2.mkv");
            Console.WriteLine("For container(example): ");
            Console.WriteLine("http://192.168.1.51:8080/api/values?videoblobname=178bd1e8-e98a-4ed9-ab2c-ac74734d1903_20190116082942_2.mkv");
            Console.WriteLine("#\n");
            
            CreateWebHostBuilder(args).Build().Run();
            
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
