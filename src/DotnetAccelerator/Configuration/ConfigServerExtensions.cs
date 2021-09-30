using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Configuration.ConfigServer;

namespace DotnetAccelerator.Configuration
{
    public static class ConfigServerExtensions
    {
        public static IConfigurationBuilder EnableConfigServer(this IConfigurationBuilder configurationBuilder, string environment)
        {
            configurationBuilder.AddEnvironmentVariables()
                .AddCommandLine(Environment.GetCommandLineArgs())
                .AddProfiles();
            
            var bootstrapConfig = configurationBuilder.Build();
            var loggerFactory = LoggerFactory.Create(c => c.AddConfiguration(bootstrapConfig.GetSection("Logging")));
            configurationBuilder
                .AddConfigServer(environment, loggerFactory);
            return configurationBuilder;
        }
    }
}