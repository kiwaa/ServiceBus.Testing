using Azure.Messaging.ServiceBus;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBus.Testing.Queues
{
    internal interface IQueue
    {
        ValueTask AddAllAsync(IEnumerable<ServiceBusMessage> batch);
        ValueTask AddAsync(ServiceBusMessage message);
        IAsyncEnumerable<ServiceBusReceivedMessage> GetAllAsync(int maxMessages = int.MaxValue, CancellationToken cancellationToken = default);
        ValueTask<ServiceBusReceivedMessage> GetAsync(CancellationToken cancellationToken = default);
        ValueTask<ServiceBusReceivedMessage> GetAsync(string sessionId, CancellationToken cancellationToken = default);
        ValueTask Complete(ServiceBusReceivedMessage message);
        ValueTask Abandon(ServiceBusReceivedMessage message);
    }
}