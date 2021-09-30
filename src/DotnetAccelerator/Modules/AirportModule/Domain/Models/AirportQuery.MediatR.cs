using System.Collections.Generic;
using MediatR;

namespace DotnetAccelerator.Modules.AirportModule.Domain.Models
{
    partial class AirportQuery : IRequest<IAsyncEnumerable<Airport>>
    {
        
    }
}