using DotnetAccelerator.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Steeltoe.Bootstrap.Autoconfig;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint;

namespace DotnetAccelerator
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
                .UseYamlWithProfilesAppConfiguration(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
        

    }
}