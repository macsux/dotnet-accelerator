using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MyProjectGroup.DotnetAccelerator.Modules.WeatherModule.Api;

#pragma warning disable 1998

namespace MyProjectGroup.DotnetAccelerator.Modules.WeatherModule
{
    partial class WeatherService : IRequestHandler<WeatherForecastQuery, IAsyncEnumerable<WeatherForecast>>, IRequestHandler<WeatherForecast, WeatherForecast>
    {
        public async Task<IAsyncEnumerable<WeatherForecast>> Handle(WeatherForecastQuery request, CancellationToken cancellationToken) =>
            GetForecasts(request, cancellationToken);
        public Task<WeatherForecast> Handle(WeatherForecast request, CancellationToken cancellationToken) =>
            SaveForecast(request, cancellationToken);
    }
}