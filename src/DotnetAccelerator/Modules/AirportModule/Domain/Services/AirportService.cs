using System.Collections.Generic;
using System.Linq;
using DotnetAccelerator.Modules.AirportModule.Domain.Models;
using DotnetAccelerator.Modules.WeatherModule.Domain.Services;
using DotnetAccelerator.Persistence;
using LinqKit;
using Microsoft.Extensions.Logging;

namespace DotnetAccelerator.Modules.AirportModule.Domain.Services
{
    public partial class AirportService
    {
        private readonly DotnetAcceleratorContext _context;
        private readonly ILogger<WeatherService> _logger;

        public AirportService(DotnetAcceleratorContext context, ILogger<WeatherService> logger)
        {
            _context = context;
            _logger = logger;
        }
        public IAsyncEnumerable<Airport> GetAirports(AirportQuery query)
        {
            var predicate = PredicateBuilder.New<Airport>(true);
            if (query.AirportId != null)
            {
                predicate = predicate.And(x => x.Id == query.AirportId);
            }

            return _context.Airports
                .AsQueryable()
                .Where(predicate)
                .ToAsyncEnumerable();
        }
    }
}