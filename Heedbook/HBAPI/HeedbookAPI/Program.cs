using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace UserOperations
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
                WebHost.CreateDefaultBuilder(args)
                    .UseStartup<Startup>();
         
                
        //         public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        //             WebHost.CreateDefaultBuilder(args)
        // .UseKestrel()
        // .UseContentRoot(Directory.GetCurrentDirectory())
        // .UseUrls("http://localhost:5000/")
        // .UseStartup<Startup>();
    }
}
