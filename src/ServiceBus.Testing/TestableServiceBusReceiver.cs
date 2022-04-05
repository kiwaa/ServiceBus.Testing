using Azure.Messaging.ServiceBus;
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
        private readonly InMemoryQueue queue;

        private bool isClosed;
        public override bool IsClosed => isClosed;
        public TestableServiceBusReceiver(InMemoryQueue queue) : this(queue, null)
        {
        }

        public TestableServiceBusReceiver(InMemoryQueue queue, ServiceBusReceiverOptions options)
        {
            this.queue = queue;
            this.options = options ?? new ServiceBusReceiverOptions();
        }

        public override async Task<ServiceBusReceivedMessage> ReceiveMessageAsync(TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default)
        {
            var message = await queue.GetAsync(cancellationToken);
            return ToReceived(message);
        }

        public override async IAsyncEnumerable<ServiceBusReceivedMessage> ReceiveMessagesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var message in queue.GetAllAsync(cancellationToken))
            {
                yield return ToReceived(message);
            }
        }

        public override Task<IReadOnlyList<ServiceBusReceivedMessage>> ReceiveMessagesAsync(int maxMessages, TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task CompleteMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task AbandonMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task DeadLetterMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task DeferMessageAsync(ServiceBusReceivedMessage message, IDictionary<string, object> propertiesToModify = null, CancellationToken cancellationToken = default)
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
