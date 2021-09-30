using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotnetAccelerator.Messaging;
using DotnetAccelerator.Modules.AirportModule.Domain.Models;
using DotnetAccelerator.Modules.WeatherModule.Domain.Models;
using DotnetAccelerator.Persistence;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotnetAccelerator.Modules.WeatherModule.Domain.Services
{
    public partial class WeatherService
    {
        private readonly DotnetAcceleratorContext _context;
        private readonly IMessageBus _messageBus;
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(DotnetAcceleratorContext context, IMessageBus messageBus,  ILogger<WeatherService> logger)
        {
            _context = context;
            _messageBus = messageBus;
            _logger = logger;
        }

        public IAsyncEnumerable<WeatherForecast> GetForecasts(CancellationToken cancellationToken = default) => GetForecasts(new WeatherForecastQuery(), cancellationToken);
        
        public IAsyncEnumerable<WeatherForecast> GetForecasts(WeatherForecastQuery query, CancellationToken cancellationToken = default)
        {
            var predicate = PredicateBuilder.New<WeatherForecast>(true);
            if (query.AirportId != null)
            {
                predicate = predicate.And(x => x.AirportId == query.AirportId);
            }
            if (query.FromDate != null)
            {
                predicate = predicate.And(x => x.Date >= query.FromDate);
            }
            if (query.ToDate != null)
            {
                predicate = predicate.And(x => x.Date <= query.ToDate);
            }
            return _context.WeatherForecasts
                .AsQueryable()
                .Where(predicate)
                .AsAsyncEnumerable();
        }

        public async Task<WeatherForecast> SaveForecast(WeatherForecast forecast, CancellationToken cancellationToken = default)
        {
            if (!await _messageBus.Send(new AirportQuery {AirportId = forecast.AirportId}, cancellationToken).AnyAsync(cancellationToken))
            {
                throw new DomainException($"{forecast.AirportId} is not a valid airport");
            }
            _context.WeatherForecasts.Update(forecast);
            await _context.SaveChangesAsync(cancellationToken);
            return forecast;
        }
    }
}