using System.Collections.Generic;
using MediatR;

namespace MyProjectGroup.DotnetAccelerator.Modules.AirportModule.Domain.Models
{
    partial class AirportQuery : IRequest<IAsyncEnumerable<Airport>>
    {
        
    }
}