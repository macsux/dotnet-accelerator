using System;
using System.Collections.Generic;
using DotnetAccelerator.Messaging;
using DotnetAccelerator.Modules.AirportModule.Domain.Models;
using DotnetAccelerator.Modules.AirportModule.Domain.Services;
#if enableSecurity
using DotnetAccelerator.Security;
#endif
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace DotnetAccelerator.Modules.AirportModule
{
    [ApiController]
    [Route("[controller]")]
    public class AirportController : ControllerBase
    {
        private readonly IMessageBus _messageBus;

        public AirportController(IMessageBus messageBus)
        {
            _messageBus = messageBus;
        }

        [HttpGet]
#if enableSecurity
        [Authorize(KnownAuthorizationPolicy.AirportRead)]
#endif
        public IAsyncEnumerable<Airport> Get() => Get(null);
        [HttpGet("{airportId}")]
#if enableSecurity
        [Authorize(KnownAuthorizationPolicy.AirportRead)]
#endif
        public IAsyncEnumerable<Airport> Get(string? airportId) => _messageBus.Send(new AirportQuery{ AirportId = airportId });
        
    }
}