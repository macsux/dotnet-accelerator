using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Configuration.Placeholder;
#if configserver
using Steeltoe.Extensions.Configuration.ConfigServer;
#endif


namespace MyProjectGroup.Common.Configuration
{
    public static class ConfigurationBuilderExtensions
    {
        private static Type? _markerType;

        private static Lazy<string> AppSettingsConfigName = new (() => 
            File.Exists(GetFullPath("appsettings.yaml")) 
                ? "appsettings" 
                : AppName!.Value);

        private static Lazy<string> AppName = new(() =>
        {
            var overridenAppName = _markerType!.Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Where(x => x.Key == "ApplicationName")
                .Select(x => x.Value)
                .FirstOrDefault();
            if (overridenAppName != null)
            {
                return overridenAppName;
            }

            return _markerType.Assembly.GetName().Name!;
        });
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
        public static WebApplicationBuilder UseYamlWithProfilesAppConfiguration<T>(this WebApplicationBuilder hostBuilder, string[] args)
        {
            _markerType = typeof(T);
            var cfg = (IConfigurationBuilder)hostBuilder.Configuration;
            
            cfg.Sources.Clear();
            var environment = hostBuilder.Environment.EnvironmentName;
            var configName = AppSettingsConfigName.Value;
            var bootstrapConfigBuilder = cfg
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    {"spring:application:name", AppName.Value}
                })
                .AddYamlFile(GetFullPath("solution-defaults.yaml"), true, true)
                .AddYamlFile(GetFullPath("application.yaml"), true, true)
                .AddYamlFile(GetFullPath($"solution-defaults.{environment}.yaml"), true, true)
                .AddYamlFile(GetFullPath($"application-{environment}.yaml"), true, true)
                .AddYamlFile(GetFullPath("appsettings.yaml"), true, true)
                .AddYamlFile(GetFullPath($"{AppName.Value}.yaml"), true, true)
                .AddYamlFile(GetFullPath($"appsettings.{environment}.yaml"), true, true)
                .AddYamlFile(GetFullPath($"{AppName.Value}-{environment}.yaml"), true, true);
#if configserver
            bootstrapConfigBuilder.AddEnvironmentVariables()
            .AddCommandLine(Environment.GetCommandLineArgs())
            .AddProfiles();
            BootstrapLoggerFactory.Update(bootstrapConfigBuilder.Build());
            bootstrapConfigBuilder.AddConfigServer(environment, BootstrapLoggerFactory.Instance);
#endif
            bootstrapConfigBuilder
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .AddPlaceholderResolver();

            var logger = BootstrapLoggerFactory.Instance.CreateLogger(typeof(ConfigurationBuilderExtensions).FullName!);
            logger.LogInformation("Configuration folder: {ConfigFolder}", ConfigFolder.Value);

            void LogSources(IList<IConfigurationSource> sources)
            {
                foreach (var source in sources)
                {
                    var sourceName = source.GetType().Name;
                    if (source is FileConfigurationSource fileSource)
                    {
                        var fullPath = Path.Combine(ConfigFolder.Value, fileSource.Path);
                        logger.LogTrace("- {ConfigSource} - {File} {Status}", sourceName, fullPath, File.Exists(fullPath) ? "" : "missing");
                    }
                    else if (source is PlaceholderResolverSource placeholderSource)
                    {
                        if (placeholderSource.GetType().GetField("_sources", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(placeholderSource) is IList<IConfigurationSource> innerSources)
                        {
                            LogSources(innerSources);
                        }
                    }
                    else
                    {
                        logger.LogTrace("Config Source: {ConfigSource}", sourceName);
                    }
                }
            }
            LogSources(bootstrapConfigBuilder.Sources);
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
                    builder.AddYamlFile(GetFullPath($"{AppSettingsConfigName.Value}.{profile}.yaml"), true, true);
                    builder.AddYamlFile(GetFullPath($"{AppSettingsConfigName.Value}-{profile}.yaml"), true, true);
                }
            }

            return builder;
        }

        private static string GetFullPath(string filename) => Path.Combine(ConfigFolder.Value, filename);
    }
}