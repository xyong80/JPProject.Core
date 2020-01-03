using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Admin.host
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var host = CreateHostBuilder(args).Build();

            AdminUiMigrationHelpers.EnsureSeedData(host.Services).Wait();

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
