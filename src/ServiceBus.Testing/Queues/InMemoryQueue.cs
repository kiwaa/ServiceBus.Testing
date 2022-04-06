using Azure.Messaging.ServiceBus;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ServiceBus.Testing.Queues
{
    internal class InMemoryQueue : IQueue
    {
        private readonly ConcurrentDictionary<string, Channel<ServiceBusReceivedMessage>> sessions = new ConcurrentDictionary<string, Channel<ServiceBusReceivedMessage>>();
        private readonly Channel<ServiceBusReceivedMessage> channel = Channel.CreateUnbounded<ServiceBusReceivedMessage>();

        public async ValueTask AddAsync(ServiceBusMessage message)
        {
            var received = ToReceived(message);
            if (received.SessionId != null)
                await GetOrAddSession(received.SessionId).Writer.WriteAsync(received);
            await channel.Writer.WriteAsync(received);
        }
        public async ValueTask AddAllAsync(IEnumerable<ServiceBusMessage> batch)
        {
            foreach (var message in batch)
                await AddAsync(message);
        }

        public ValueTask<ServiceBusReceivedMessage> GetAsync(CancellationToken cancellationToken = default)
        {
            return channel.Reader.ReadAsync(cancellationToken);
        }
        public ValueTask<ServiceBusReceivedMessage> GetAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            return GetOrAddSession(sessionId).Reader.ReadAsync(cancellationToken);
        }

        public IAsyncEnumerable<ServiceBusReceivedMessage> GetAllAsync(int maxMessages = int.MaxValue, CancellationToken cancellationToken = default)
        {
            var list = new List<ServiceBusReceivedMessage>();
            while (list.Count < maxMessages && channel.Reader.TryRead(out ServiceBusReceivedMessage message))
                list.Add(message);

            return list.ToAsyncEnumerable();
        }

        private Channel<ServiceBusReceivedMessage> GetOrAddSession(string sessionId)
        {
            return sessions.GetOrAdd(sessionId, x => Channel.CreateUnbounded<ServiceBusReceivedMessage>());
        }

        public ValueTask Complete(ServiceBusReceivedMessage message)
        {
            // not supported
            return ValueTask.CompletedTask;
        }
        public ValueTask Abandon(ServiceBusReceivedMessage message)
        {
            // not supported
            return ValueTask.CompletedTask;
        }

        private ServiceBusReceivedMessage ToReceived(ServiceBusMessage message)
        {
            return ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: message.Body,
                messageId: message.MessageId,
                partitionKey: message.PartitionKey,
                sessionId: message.SessionId);
        }

    }
}