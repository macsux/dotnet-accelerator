using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MyProjectGroup.Common
{
    /// <summary>
    /// Allows early utilization of log infrastructure before log config is even read. Any providers spawned are instantly switched over to
    /// real log providers as the application utilization progresses.
    /// This class should only be used by components start are invoke before  logging infrastructure is build (prior to service container creation)
    /// </summary>
    public static class BootstrapLoggerFactory
    {
        public static ILoggerFactory Instance => _bootstrapFactory;
        private static bool isFinalSet = false;
        public static void Update(IConfiguration value)
        {
            _configuration = value;
            if (value == null ||isFinalSet)
                return;
            var newLogger = LoggerFactory.Create(_ => _
                .AddConsole()
                .AddConfiguration(value.GetSection("Logging")));
            _bootstrapFactory.UpdateFactory(newLogger);
        }

        public static void Update(ILoggerFactory value)
        {
            if (value == null)
                return;
            _bootstrapFactory.UpdateFactory(value);
            isFinalSet = true;
        }

        private static readonly BootstrapLoggerFactoryInst _bootstrapFactory = new();
        private static IConfiguration? _configuration;

        private class BootstrapLoggerFactoryInst : ILoggerFactory
        {
            private object _lock = new();
            public Dictionary<string, BoostrapLoggerInst> _loggers = new();

            public void UpdateFactory(ILoggerFactory factory)
            {
                lock (_lock)
                {
                    _factoryInstance.Dispose();
                    _factoryInstance = factory;
                    foreach (var logger in _loggers.Values)
                    {
                        logger.Logger = _factoryInstance.CreateLogger(logger.Name);
                    }
                }
            }
            private ILoggerFactory _factoryInstance = LoggerFactory.Create(_ => _
                .AddConsole()
                .SetMinimumLevel(LogLevel.Debug));

            public void Dispose()
            {
                lock (_lock)
                {
                    _factoryInstance.Dispose();
                }
            }

            public void AddProvider(ILoggerProvider provider)
            {
                lock (_lock)
                {
                    _factoryInstance.AddProvider(provider);
                }
            }

            public ILogger CreateLogger(string categoryName)
            {
                lock (_lock)
                {
                    if (!_loggers.TryGetValue(categoryName, out var logger))
                    {
                        var innerLogger = _factoryInstance.CreateLogger(categoryName);
                        logger = new BoostrapLoggerInst(innerLogger, categoryName);
                        _loggers.Add(categoryName, logger);
                    }

                    return logger;
                }
            }
        }
        private class BoostrapLoggerInst : ILogger
        {
            public volatile ILogger Logger;
            public string Name { get; set; }
            public BoostrapLoggerInst(ILogger logger, string name)
            {
                Name = name;
                Logger = logger;
            }

            public IDisposable BeginScope<TState>(TState state) => Logger.BeginScope(state);

            public bool IsEnabled(LogLevel logLevel) => Logger.IsEnabled(logLevel);

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                Logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}