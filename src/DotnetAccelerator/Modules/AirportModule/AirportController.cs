using System.Collections.Generic;
using DotnetAccelerator.Messaging;
using DotnetAccelerator.Modules.AirportModule.Domain.Models;
using DotnetAccelerator.Modules.AirportModule.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotnetAccelerator.Modules.AirportModule
{
    [ApiController]
    [Route("[controller]")]
    public class AirportController : ControllerBase
    {
        private readonly IMessageBus _messageBus;
        private readonly AirportService _airportService;

        public AirportController(IMessageBus messageBus, AirportService airportService)
        {
            _messageBus = messageBus;
            _airportService = airportService;
        }

        [HttpGet]
        public IAsyncEnumerable<Airport> Get() => Get(null);
        [HttpGet("{airportId}")]
        public IAsyncEnumerable<Airport> Get(string? airportId) => _messageBus.Send(new AirportQuery{ AirportId = airportId });
        
    }
}