using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyProjectGroup.Common.Messaging;
using MyProjectGroup.DotnetAccelerator.Modules.WeatherModule.Api;

#if enableSecurity
using MyProjectGroup.Common.Security;
#endif

namespace MyProjectGroup.DotnetAccelerator.Modules.WeatherModule
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

        [HttpGet("{airportId}", Name = "Weather_GetById")]
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
        [HttpPut(Name = "Weather_Put")]
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