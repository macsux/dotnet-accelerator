using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotnetAccelerator.Messaging;
using DotnetAccelerator.Modules.WeatherModule.Domain.Models;
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
        public IAsyncEnumerable<WeatherForecast> Get([FromQuery] WeatherForecastQuery query) => _messageBus.Send(query);

        [HttpGet("{airportId}")]
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
        public async Task<ActionResult<WeatherForecast>> Post(WeatherForecast forecast)
        {
            return await _messageBus.Send(forecast);
        }
    }
}