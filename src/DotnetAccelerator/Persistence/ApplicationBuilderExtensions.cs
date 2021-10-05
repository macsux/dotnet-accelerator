using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetAccelerator.Persistence
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder MigrateDatabase<TContext>(this IApplicationBuilder app) where TContext : DbContext
        {
            using var scope = app.ApplicationServices.CreateScope();
            var migrator = ActivatorUtilities.CreateInstance<DbContextMigrator>(scope.ServiceProvider);
            migrator.Migrate<TContext>();
            return app;
        }
    }
}