using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MyProjectGroup.Common.Persistence
{
    /// <summary>
    /// Helper class to apply entity framework migrations
    /// </summary>
    public class DbContextMigrator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public DbContextMigrator(IServiceProvider serviceProvider, ILogger<DbContextMigrator> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void Migrate<TContext>() where TContext : DbContext
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TContext>();
            var isNewDb = false;
            var migrations = new List<string>();
            try
            {
                migrations = db.Database.GetPendingMigrations().ToList();
            }
            catch
            {
                isNewDb = true; // might not be true source of the error, but we'll catch real cause as part of Migrate call
            }

            _logger.LogInformation("Starting database migration...");
            db.Database.Migrate();
            if (isNewDb)
            {
                migrations = db.Database.GetAppliedMigrations().ToList();
            }

            if (migrations.Any())
            {
                _logger.LogInformation("The following migrations have been successfully applied:");
                foreach (var migration in migrations)
                {
                    _logger.LogInformation(migration);
                }
            }
            else
            {
                _logger.LogInformation("Database is already up to date");
            }
        }
    }
}