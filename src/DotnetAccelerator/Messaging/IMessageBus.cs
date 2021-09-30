using System.Collections.Generic;
using System.Threading;
using MediatR;

namespace DotnetAccelerator.Messaging
{
    public interface IMessageBus : IMediator
    {
        public IAsyncEnumerable<TResponse> Send<TResponse>(IRequest<IAsyncEnumerable<TResponse>> request, CancellationToken cancellationToken = default);
        
    }
}