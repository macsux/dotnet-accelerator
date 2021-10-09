using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DotnetAcceleratorTests
{
    public interface IUseDbContext<TContext> : IDisposable where TContext : DbContext
    {
        private static ConcurrentDictionary<object, DbContextOptions> _cache = new();
       
        public TContext GetDbContext()
        {
            
            var options = _cache.GetOrAdd(this, _ =>
            {
                var contextOptions = new DbContextOptionsBuilder<TContext>()
                    .UseSqlite(CreateInMemoryDatabase())
                    .Options;
                return contextOptions;
            });
            TContext context =  (TContext)Activator.CreateInstance(typeof(TContext), options)!;
            context.Database.EnsureCreated();
            return context;
        }

        void IDisposable.Dispose() => GetDbContext().Database.GetDbConnection().Dispose();

        private static DbConnection CreateInMemoryDatabase()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();
            return connection;
        }
        
    }
}