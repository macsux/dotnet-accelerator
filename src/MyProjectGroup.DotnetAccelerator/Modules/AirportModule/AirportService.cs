using System.Collections.Generic;
using System.Linq;
using LinqKit;
using Microsoft.Extensions.Logging;
using MyProjectGroup.Common.Modules;
using MyProjectGroup.DotnetAccelerator.Modules.AirportModule.Api;
using MyProjectGroup.DotnetAccelerator.Modules.WeatherModule;
using MyProjectGroup.DotnetAccelerator.Persistence;

namespace MyProjectGroup.DotnetAccelerator.Modules.AirportModule
{
    public partial class AirportService : IService
    {
        private readonly DotnetAcceleratorContext _context;
        private readonly ILogger _logger;

        public AirportService(DotnetAcceleratorContext context, ILogger<AirportService> logger)
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