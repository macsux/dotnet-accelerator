using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MyProjectGroup.DotnetAccelerator.Modules.AirportModule.Domain.Models;

#pragma warning disable 1998

namespace MyProjectGroup.DotnetAccelerator.Modules.AirportModule.Domain.Services
{
    partial class AirportService : IRequestHandler<AirportQuery, IAsyncEnumerable<Airport>>
    {
        public async Task<IAsyncEnumerable<Airport>> Handle(AirportQuery request, CancellationToken cancellationToken) => GetAirports(request);
    }
}