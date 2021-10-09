using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using MediatR;

namespace MyProjectGroup.Common.Messaging
{
    public class MessageBus : Mediator, IMessageBus
    {
        public MessageBus(ServiceFactory serviceFactory) : base(serviceFactory)
        {
        }

        public async IAsyncEnumerable<TResponse> Send<TResponse>(IRequest<IAsyncEnumerable<TResponse>> request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var result = await base.Send(request);
            await foreach (var item in result)
            {
                yield return item;
            }
        }
    }
}