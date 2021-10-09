using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using MyProjectGroup.Common.Configuration;
using Steeltoe.Extensions.Logging;

namespace MyProjectGroup.DotnetAccelerator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .AddDynamicLogging()
                .UseYamlWithProfilesAppConfiguration<Program>(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
        

    }
}