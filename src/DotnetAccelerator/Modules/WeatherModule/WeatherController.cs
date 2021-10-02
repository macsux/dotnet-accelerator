using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotnetAccelerator.Messaging;
using DotnetAccelerator.Modules.WeatherModule.Domain.Models;
#if enableSecurity
using DotnetAccelerator.Security;
#endif
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DotnetAccelerator.Modules.WeatherModule
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly IMessageBus _messageBus;

        public WeatherController(IMessageBus messageBus)
        {
            _messageBus = messageBus;
        }

        [HttpGet]
#if enableSecurity
        [Authorize(KnownAuthorizationPolicy.WeatherRead)]
#endif
        public IAsyncEnumerable<WeatherForecast> Get([FromQuery] WeatherForecastQuery query) => _messageBus.Send(query);

        [HttpGet("{airportId}")]
#if enableSecurity
        [Authorize(KnownAuthorizationPolicy.WeatherRead)]
#endif
        public async Task<ActionResult<WeatherForecast>> Get(string airportId)
        {
            var forecast = await Get(new WeatherForecastQuery {AirportId = airportId}).FirstOrDefaultAsync();
            if (forecast == null)
            {
                return NotFound(airportId);
            }
            return forecast;
        }

        [HttpPost]
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
#if enableSecurity
        [Authorize(KnownAuthorizationPolicy.WeatherWrite)]
#endif
        public async Task<ActionResult<WeatherForecast>> Post(WeatherForecast forecast)
        {
            return await _messageBus.Send(forecast);
        }
    }
}