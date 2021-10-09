using Microsoft.EntityFrameworkCore;
using MyProjectGroup.DotnetAccelerator.Modules.AirportModule.Domain.Models;
using MyProjectGroup.DotnetAccelerator.Modules.WeatherModule.Domain.Models;

namespace MyProjectGroup.DotnetAccelerator.Persistence
{
    public class DotnetAcceleratorContext : DbContext
    {
        protected DotnetAcceleratorContext()
        {
        }

        public DotnetAcceleratorContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();
        public DbSet<Airport> Airports => Set<Airport>();
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Airport>().HasData(new Airport {Id = "YYZ", Name = "Toronto"});
            modelBuilder.Entity<Airport>().HasData(new Airport {Id = "JFK", Name = "New York"});
        }
    }
}