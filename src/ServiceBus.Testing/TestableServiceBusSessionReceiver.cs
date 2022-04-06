using Azure.Messaging.ServiceBus;
using ServiceBus.Testing.Queues;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBus.Testing
{
    internal class TestableServiceBusSessionReceiver : ServiceBusSessionReceiver
    {
        private readonly IQueue queue;
        private readonly string sessionId;
        private readonly ServiceBusSessionReceiverOptions options;
        private bool isClosed;

        public override bool IsClosed => isClosed;

        public TestableServiceBusSessionReceiver(IQueue queue, string sessionId, ServiceBusSessionReceiverOptions options)
        {
            this.queue = queue;
            this.sessionId = sessionId;
            this.options = options;
        }

        public override async Task<ServiceBusReceivedMessage> ReceiveMessageAsync(TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default)
        {
            return await queue.GetAsync(sessionId, cancellationToken);
        }
        public override IAsyncEnumerable<ServiceBusReceivedMessage> ReceiveMessagesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public override Task<IReadOnlyList<ServiceBusReceivedMessage>> ReceiveMessagesAsync(int maxMessages, TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default)
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
