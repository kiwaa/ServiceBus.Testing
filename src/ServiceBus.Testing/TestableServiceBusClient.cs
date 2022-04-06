using Azure.Messaging.ServiceBus;
using ServiceBus.Testing.Queues;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBus.Testing
{
    public class TestableServiceBusClient : ServiceBusClient
    {
        private readonly ConcurrentDictionary<string, IQueue> queues = new ConcurrentDictionary<string, IQueue>();
        private ServiceBusClientOptions options;

        public TestableServiceBusClient()
        {
        }

        public TestableServiceBusClient(ServiceBusClientOptions options)
        {
            this.options = options;
        }

        public override ServiceBusSender CreateSender(string queueOrTopicName)
        {
            var queue = GetOrCreate(queueOrTopicName);
            return new TestableServiceBusSender(queue);
        }

        public override ServiceBusReceiver CreateReceiver(string queueName)
        {
            var queue = GetOrCreate(queueName);
            return new TestableServiceBusReceiver(queue);
        }

        public override ServiceBusReceiver CreateReceiver(string queueName, ServiceBusReceiverOptions options)
        {
            var queue = GetOrCreate(queueName);
            return new TestableServiceBusReceiver(queue, options);
        }

        public override ServiceBusReceiver CreateReceiver(string topicName, string subscriptionName)
        {
            throw new NotImplementedException();
        }

        public override ServiceBusReceiver CreateReceiver(string topicName, string subscriptionName, ServiceBusReceiverOptions options)
        {
            throw new NotImplementedException();
        }

        public override Task<ServiceBusSessionReceiver> AcceptNextSessionAsync(string queueName, ServiceBusSessionReceiverOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<ServiceBusSessionReceiver> AcceptNextSessionAsync(string topicName, string subscriptionName, ServiceBusSessionReceiverOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<ServiceBusSessionReceiver> AcceptSessionAsync(string queueName, string sessionId, ServiceBusSessionReceiverOptions options = null, CancellationToken cancellationToken = default)
        {
            var queue = GetOrCreate(queueName);
            return Task.FromResult((ServiceBusSessionReceiver)new TestableServiceBusSessionReceiver(queue, sessionId, options));
        }

        public override Task<ServiceBusSessionReceiver> AcceptSessionAsync(string topicName, string subscriptionName, string sessionId, ServiceBusSessionReceiverOptions options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
        private IQueue GetOrCreate(string queueName)
        {
            //return queues.GetOrAdd(queueName, x => new InMemoryQueue());
            return queues.GetOrAdd(queueName, x => new PeekLockCircularBuffer());
        }

    }
}
