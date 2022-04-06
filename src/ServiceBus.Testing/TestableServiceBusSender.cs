using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBus.Testing
{
    internal class TestableServiceBusSender : ServiceBusSender
    {
        private readonly ConcurrentDictionary<ServiceBusMessageBatch, List<ServiceBusMessage>> batchCollection = new ConcurrentDictionary<ServiceBusMessageBatch, List<ServiceBusMessage>>();
        private readonly InMemoryQueue queue;

        private bool isClosed;
        public override bool IsClosed => isClosed;
        public TestableServiceBusSender(InMemoryQueue queue)
        {
            this.queue = queue;
        }

        public override ValueTask<ServiceBusMessageBatch> CreateMessageBatchAsync(CreateMessageBatchOptions options, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override ValueTask<ServiceBusMessageBatch> CreateMessageBatchAsync(CancellationToken cancellationToken = default)
        {
            var batch = new List<ServiceBusMessage>();
            var msg = ServiceBusModelFactory.ServiceBusMessageBatch(99999, batch);

            while (!batchCollection.TryAdd(msg, batch))
            {
                // NOP
            }
            return ValueTask.FromResult(msg);
        }

        public override async Task SendMessagesAsync(IEnumerable<ServiceBusMessage> messages, CancellationToken cancellationToken = default)
        {
            await queue.AddAllAsync(messages);
        }

        public override async Task SendMessagesAsync(ServiceBusMessageBatch messageBatch, CancellationToken cancellationToken = default)
        {
            List<ServiceBusMessage> msg;
            while (!batchCollection.TryRemove(messageBatch, out msg))
            {
                // NOP
            }
            await queue.AddAllAsync(msg);
        }

        public override async Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
        {
            await queue.AddAsync(message);
        }

        public override Task<long> ScheduleMessageAsync(ServiceBusMessage message, DateTimeOffset scheduledEnqueueTime, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<IReadOnlyList<long>> ScheduleMessagesAsync(IEnumerable<ServiceBusMessage> messages, DateTimeOffset scheduledEnqueueTime, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task CloseAsync(CancellationToken cancellationToken = default)
        {
            isClosed = true;
            return Task.CompletedTask;
        }
    }
}
