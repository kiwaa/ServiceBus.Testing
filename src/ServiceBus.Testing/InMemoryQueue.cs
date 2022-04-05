using Azure.Messaging.ServiceBus;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ServiceBus.Testing
{
    internal class InMemoryQueue
    {
        private readonly Channel<ServiceBusMessage> channel = Channel.CreateUnbounded<ServiceBusMessage>();

        public ValueTask AddAsync(ServiceBusMessage message)
        {
            return channel.Writer.WriteAsync(message);
        }
        public Task AddAllAsync(IEnumerable<ServiceBusMessage> batch)
        {
            return Task.WhenAll(batch.Select(x => channel.Writer.WriteAsync(x).AsTask()));
        }

        public ValueTask<ServiceBusMessage> GetAsync(CancellationToken cancellationToken = default)
        {
            return channel.Reader.ReadAsync(cancellationToken);
        }

        public IAsyncEnumerable<ServiceBusMessage> GetAllAsync(CancellationToken cancellationToken)
        {
            var list = new List<ServiceBusMessage>();
            while (channel.Reader.TryRead(out ServiceBusMessage message))
                list.Add(message);

            return list.ToAsyncEnumerable();
        }

    }
}