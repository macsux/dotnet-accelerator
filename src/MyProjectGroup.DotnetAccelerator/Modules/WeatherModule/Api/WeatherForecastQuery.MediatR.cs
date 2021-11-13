using System.Collections.Generic;
using MediatR;

namespace MyProjectGroup.DotnetAccelerator.Modules.WeatherModule.Api
{
    partial class WeatherForecastQuery : IRequest<IAsyncEnumerable<WeatherForecast>>
    {
        
    }
}