using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotnetAccelerator.Modules.WeatherModule.Domain.Models;
using MediatR;
#pragma warning disable 1998

namespace DotnetAccelerator.Modules.WeatherModule.Domain.Services
{
    partial class WeatherService : IRequestHandler<WeatherForecastQuery, IAsyncEnumerable<WeatherForecast>>, IRequestHandler<WeatherForecast, WeatherForecast>
    {
        public async Task<IAsyncEnumerable<WeatherForecast>> Handle(WeatherForecastQuery request, CancellationToken cancellationToken) =>
            GetForecasts(request, cancellationToken);
        public Task<WeatherForecast> Handle(WeatherForecast request, CancellationToken cancellationToken) =>
            SaveForecast(request, cancellationToken);
    }
}