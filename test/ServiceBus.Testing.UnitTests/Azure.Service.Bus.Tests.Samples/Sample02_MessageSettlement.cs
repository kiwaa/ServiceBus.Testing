using Azure.Messaging.ServiceBus;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ServiceBus.Testing.UnitTests.Samples
{
    public class Sample02_MessageSettlement
    {
        private string QueueName => Guid.NewGuid().ToString();
        [Fact(Skip = "not supported yet")]
        public async Task CompleteMessage()
        {
            string queueName = QueueName;
            // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
            await using var client = new TestableServiceBusClient();

            // create the sender
            var sender = client.CreateSender(queueName);

            // create a message that we can send
            var message = new ServiceBusMessage("Hello world!");

            // send the message
            await sender.SendMessageAsync(message);

            // create a receiver that we can use to receive and settle the message
            var receiver = client.CreateReceiver(queueName);

            // the received message is a different type as it contains some service set properties
            var receivedMessage = await receiver.ReceiveMessageAsync();

            // complete the message, thereby deleting it from the service
            await receiver.CompleteMessageAsync(receivedMessage);
            Assert.Null(await client.CreateReceiver(queueName).ReceiveMessageAsync());

        }

        [Fact(Skip = "not supported yet")]
        public async Task AbandonMessage()
        {
            string queueName = QueueName;
            // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
            await using var client = new TestableServiceBusClient();

            // create the sender
            var sender = client.CreateSender(queueName);

            // create a message that we can send
            var message = new ServiceBusMessage("Hello world!");

            // send the message
            await sender.SendMessageAsync(message);

            // create a receiver that we can use to receive and settle the message
            var receiver = client.CreateReceiver(queueName);

            #region Snippet:ServiceBusAbandonMessage
            var receivedMessage = await receiver.ReceiveMessageAsync();

            // abandon the message, thereby releasing the lock and allowing it to be received again by this or other receivers
            await receiver.AbandonMessageAsync(receivedMessage);
            #endregion
            Assert.NotNull(await client.CreateReceiver(queueName).ReceiveMessageAsync());

        }

        [Fact(Skip = "not supported yet")]
        public async Task DeferMessage()
        {
            string queueName = QueueName;
            // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
            await using var client = new TestableServiceBusClient();

            // create the sender
            var sender = client.CreateSender(queueName);

            // create a message that we can send
            var message = new ServiceBusMessage("Hello world!");

            // send the message
            await sender.SendMessageAsync(message);

            // create a receiver that we can use to receive and settle the message
            var receiver = client.CreateReceiver(queueName);

            var receivedMessage = await receiver.ReceiveMessageAsync();

            // defer the message, thereby preventing the message from being received again without using
            // the received deferred message API.
            await receiver.DeferMessageAsync(receivedMessage);

            // receive the deferred message by specifying the service set sequence number of the original
            // received message
            var deferredMessage = await receiver.ReceiveDeferredMessageAsync(receivedMessage.SequenceNumber);
            Assert.NotNull(deferredMessage);

        }

        [Fact(Skip = "not supported yet")]
        public async Task DeadLetterMessage()
        {
            string queueName = QueueName;
            // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
            await using var client = new TestableServiceBusClient();

            // create the sender
            ServiceBusSender sender = client.CreateSender(queueName);

            // create a message that we can send
            ServiceBusMessage message = new ServiceBusMessage("Hello world!");

            // send the message
            await sender.SendMessageAsync(message);

            // create a receiver that we can use to receive and settle the message
            ServiceBusReceiver receiver = client.CreateReceiver(queueName);

            ServiceBusReceivedMessage receivedMessage = await receiver.ReceiveMessageAsync();

            // dead-letter the message, thereby preventing the message from being received again without receiving from the dead letter queue.
            await receiver.DeadLetterMessageAsync(receivedMessage);

            // receive the dead lettered message with receiver scoped to the dead letter queue.
            ServiceBusReceiver dlqReceiver = client.CreateReceiver(queueName, new ServiceBusReceiverOptions
            {
                SubQueue = SubQueue.DeadLetter
            });
            ServiceBusReceivedMessage dlqMessage = await dlqReceiver.ReceiveMessageAsync();
            Assert.NotNull(dlqMessage);

        }
    }
}
