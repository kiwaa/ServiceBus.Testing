using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ServiceBus.Testing
{
    public class TestableServiceBusClient : ServiceBusClient
    {
        private ConcurrentDictionary<string, InMemoryQueue> queues = new ConcurrentDictionary<string, InMemoryQueue>();

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
        public override ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
        private InMemoryQueue GetOrCreate(string queueName)
        {
            return queues.GetOrAdd(queueName, x => new InMemoryQueue());
        }

    }
}
