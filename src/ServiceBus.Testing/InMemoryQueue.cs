using Azure.Messaging.ServiceBus;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ServiceBus.Testing
{
    internal class InMemoryQueue
    {
        private readonly ConcurrentDictionary<string, Channel<ServiceBusMessage>> sessions = new ConcurrentDictionary<string, Channel<ServiceBusMessage>>();
        private readonly Channel<ServiceBusMessage> channel = Channel.CreateUnbounded<ServiceBusMessage>();

        public async ValueTask AddAsync(ServiceBusMessage message)
        {
            if (message.SessionId != null)
                await GetOrAddSession(message.SessionId).Writer.WriteAsync(message);
            await channel.Writer.WriteAsync(message);
        }
        public async ValueTask AddAllAsync(IEnumerable<ServiceBusMessage> batch)
        {
            foreach (var message in batch)
                await AddAsync(message);
        }

        public ValueTask<ServiceBusMessage> GetAsync(CancellationToken cancellationToken = default)
        {
            return channel.Reader.ReadAsync(cancellationToken);
        }
        public ValueTask<ServiceBusMessage> GetAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            return GetOrAddSession(sessionId).Reader.ReadAsync(cancellationToken);
        }

        public IAsyncEnumerable<ServiceBusMessage> GetAllAsync(int maxMessages = int.MaxValue, CancellationToken cancellationToken = default)
        {
            var list = new List<ServiceBusMessage>();
            while (list.Count < maxMessages && channel.Reader.TryRead(out ServiceBusMessage message))
                list.Add(message);

            return list.ToAsyncEnumerable();
        }

        private Channel<ServiceBusMessage> GetOrAddSession(string sessionId)
        {
            return sessions.GetOrAdd(sessionId, x => Channel.CreateUnbounded<ServiceBusMessage>());
        }
    }
}