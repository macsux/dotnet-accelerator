using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
//using Steeltoe.Extensions.Configuration.ConfigServer;

namespace DotnetAccelerator.Configuration
{
    public static class ConfigurationBuilderExtensions
    {
        private static Lazy<string> AppSettingsConfigName = new (() => 
            File.Exists(GetFullPath("appsettings.yaml")) ? "appsettings" : AppName.Value);

        private static Lazy<string> AppName = new(() => Assembly
            .GetEntryAssembly()?
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(x => x.Key == "ApplicationName")
            .Select(x => x.Value)
            .FirstOrDefault() ?? Assembly.GetEntryAssembly()?.GetName().Name ?? throw new Exception("App name not set"));
        private static Lazy<string> ConfigFolder = new (() =>
        {

            var currentDir = new DirectoryInfo(Environment.CurrentDirectory);

            while (currentDir.Parent != null)
            {
                
                var configFolder = Path.Combine(currentDir.FullName, "config");
                if (Directory.Exists(configFolder))
                {
                    return configFolder;
                }
                currentDir = currentDir.Parent;
            }

            return Environment.CurrentDirectory;
        });
        public static IHostBuilder UseYamlWithProfilesAppConfiguration(this IHostBuilder hostBuilder, string[] args)
        {
            hostBuilder.ConfigureAppConfiguration((hostingContext, cfg) =>
            {
                cfg.Sources.Clear();
                var environment = hostingContext.HostingEnvironment.EnvironmentName;
                var configName = AppSettingsConfigName.Value;
                var bootstrapConfigBuilder = cfg
                    .AddInMemoryCollection(new Dictionary<string, string>()
                    {
                        {"spring:application:name", AppName.Value}
                    })
                    .AddYamlFile(GetFullPath("application.yaml"), true, true)
                    .AddYamlFile(GetFullPath("solution-defaults.yaml"), true, true)
                    .AddYamlFile(GetFullPath($"{configName}.yaml"), false, true)
                    .AddYamlFile(GetFullPath($"{configName}.{environment}.yaml"), true, true)
                    .AddYamlFile(GetFullPath($"{configName}-{environment}.yaml"), true, true)
#if configserver
                    .EnableConfigServer(environment)
#endif
                    
                    .AddEnvironmentVariables()
                    .AddCommandLine(args);
            });
            return hostBuilder;
        }
        
        public static IConfigurationBuilder AddProfiles(this IConfigurationBuilder builder)
        {
            if (builder is not IConfiguration config)
            {
                config = builder.Build();
            }

            var profilesCsv = config.GetValue<string>("spring:profiles:active") ?? config.GetValue<string>("profiles:active");
            if (profilesCsv != null)
            {
                var profiles = profilesCsv.Split(",").Select(x => x.Trim()).ToArray();
                foreach (var profile in profiles)
                {
                    builder.AddYamlFile(GetFullPath($"{GetAppSettingsName()}.{profile}.yaml"), true, true);
                    builder.AddYamlFile(GetFullPath($"{GetAppSettingsName()}-{profile}.yaml"), true, true);
                }
            }

            return builder;
        }

        private static string GetAppSettingsName()
        {
            return File.Exists(GetFullPath("appsettings.yaml")) ? "appsettings" : Assembly
                .GetEntryAssembly()?
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Where(x => x.Key == "ApplicationName")
                .Select(x => x.Value)
                .FirstOrDefault() ?? throw new Exception("Assembly does not have application name set. Add <ApplicationName>MyAppName</ApplicationName> to csproj.");
        }

        private static string GetFullPath(string filename) => Path.Combine(ConfigFolder.Value, filename);
    }
}