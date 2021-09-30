using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotnetAccelerator.Modules.AirportModule.Domain.Models;
using MediatR;
#pragma warning disable 1998

namespace DotnetAccelerator.Modules.AirportModule.Domain.Services
{
    partial class AirportService : IRequestHandler<AirportQuery, IAsyncEnumerable<Airport>>
    {
        public async Task<IAsyncEnumerable<Airport>> Handle(AirportQuery request, CancellationToken cancellationToken) => GetAirports(request);
    }
}