using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProjectGroup.Common.Messaging;
using MyProjectGroup.DotnetAccelerator.Modules.AirportModule.Domain.Models;
#if enableSecurity
using MyProjectGroup.Common.Security;
#endif

namespace MyProjectGroup.DotnetAccelerator.Modules.AirportModule
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