using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyProjectGroup.Common;
using MyProjectGroup.Common.Configuration;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint;

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
                .AddAllActuators()
                .AddDynamicLogging()
                .UseYamlWithProfilesAppConfiguration<Program>(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
        

    }
}