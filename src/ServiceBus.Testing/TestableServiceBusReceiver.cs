using Azure.Messaging.ServiceBus;
using ServiceBus.Testing.Queues;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBus.Testing
{
    internal class TestableServiceBusReceiver : ServiceBusReceiver
    {
        private readonly ServiceBusReceiverOptions options;
        private readonly IQueue queue;

        private bool isClosed;
        public override bool IsClosed => isClosed;
        public TestableServiceBusReceiver(IQueue queue) : this(queue, null)
        {
        }

        public TestableServiceBusReceiver(IQueue queue, ServiceBusReceiverOptions options)
        {
            this.queue = queue;
            this.options = options ?? new ServiceBusReceiverOptions();
        }

        public override async Task<ServiceBusReceivedMessage> ReceiveMessageAsync(TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default)
        {
            return await queue.GetAsync(cancellationToken);
        }

        public override IAsyncEnumerable<ServiceBusReceivedMessage> ReceiveMessagesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            return queue.GetAllAsync(cancellationToken: cancellationToken);
        }

        public override async Task<IReadOnlyList<ServiceBusReceivedMessage>> ReceiveMessagesAsync(int maxMessages, TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default)
        {
            var list = new List<ServiceBusReceivedMessage>();
            await foreach (var message in queue.GetAllAsync(maxMessages, cancellationToken))
            {
                list.Add(message);
            }
            return list;
        }

        public override async Task<ServiceBusReceivedMessage> PeekMessageAsync(long? fromSequenceNumber = null, CancellationToken cancellationToken = default)
        {
            return await queue.GetAsync(cancellationToken);
        }

        public override Task<IReadOnlyList<ServiceBusReceivedMessage>> PeekMessagesAsync(int maxMessages, long? fromSequenceNumber = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override async Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
        {
            await queue.Complete(message);
        }

        public override async Task AbandonMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify = null, CancellationToken cancellationToken = default)
        {
            await queue.Abandon(message);
        }

        public override Task DeferMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public override Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, string deadLetterReason, string deadLetterErrorDescription = null, CancellationToken cancellationToken = default)
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
