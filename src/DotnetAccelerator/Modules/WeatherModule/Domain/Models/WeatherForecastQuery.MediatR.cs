using System.Collections.Generic;
using MediatR;

namespace DotnetAccelerator.Modules.WeatherModule.Domain.Models
{
    partial class WeatherForecastQuery : IRequest<IAsyncEnumerable<WeatherForecast>>
    {
        
    }
}