using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Xunit;

namespace ServiceBus.Testing.UnitTests.Samples
{
    public class Sample06_Transactions
    {
        private string QueueName => Guid.NewGuid().ToString();
        [Fact(Skip = "not supported yet")]
        public async Task TransactionalSendAndComplete()
        {
            string queueName = QueueName;
            await using var client = new TestableServiceBusClient();
            ServiceBusSender sender = client.CreateSender(queueName);

            await sender.SendMessageAsync(new ServiceBusMessage("First"));
            ServiceBusReceiver receiver = client.CreateReceiver(queueName);
            ServiceBusReceivedMessage firstMessage = await receiver.ReceiveMessageAsync();
            using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await sender.SendMessageAsync(new ServiceBusMessage("Second"));
                await receiver.CompleteMessageAsync(firstMessage);
                ts.Complete();
            }

            ServiceBusReceivedMessage secondMessage = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5));

            Assert.NotNull(secondMessage);
            await receiver.CompleteMessageAsync(secondMessage);
        }

        [Fact(Skip = "not supported yet")]
        public async Task TransactionalSetSessionState()
        {
            string queueName = QueueName;
            await using var client = new TestableServiceBusClient();
            ServiceBusSender sender = client.CreateSender(queueName);

            await sender.SendMessageAsync(new ServiceBusMessage("my message") { SessionId = "sessionId" });
            ServiceBusSessionReceiver receiver = await client.AcceptNextSessionAsync(queueName);
            ServiceBusReceivedMessage receivedMessage = await receiver.ReceiveMessageAsync();

            var state = Encoding.UTF8.GetBytes("some state");
            using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await receiver.CompleteMessageAsync(receivedMessage);
                await receiver.SetSessionStateAsync(new BinaryData(state));
                ts.Complete();
            }
            var bytes = await receiver.GetSessionStateAsync();
            Assert.Equal(state, bytes.ToArray());
        }

        [Fact(Skip = "not supported yet")]
        public async Task CrossEntityTransaction()
        {
            var options = new ServiceBusClientOptions { EnableCrossEntityTransactions = true };
            await using var client = new TestableServiceBusClient(options);

            ServiceBusReceiver receiverA = client.CreateReceiver("queueA");
            ServiceBusSender senderB = client.CreateSender("queueB");
            ServiceBusSender senderC = client.CreateSender("topicC");


            ServiceBusReceivedMessage receivedMessage = await receiverA.ReceiveMessageAsync();

            using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await receiverA.CompleteMessageAsync(receivedMessage);
                await senderB.SendMessageAsync(new ServiceBusMessage());
                await senderC.SendMessageAsync(new ServiceBusMessage());
                ts.Complete();
            }

            receivedMessage = await receiverA.ReceiveMessageAsync(TimeSpan.FromSeconds(5));
            Assert.Null(receivedMessage);
        }

        [Fact(Skip = "Only verifying that it compiles.")]
        public async Task CrossEntityTransactionWrongOrder()
        {

            var options = new ServiceBusClientOptions { EnableCrossEntityTransactions = true };
            await using var client = new TestableServiceBusClient(options);

            ServiceBusReceiver receiverA = client.CreateReceiver("queueA");
            ServiceBusSender senderB = client.CreateSender("queueB");
            ServiceBusSender senderC = client.CreateSender("topicC");

            // SenderB becomes the entity through which subsequent "sends" are routed through, since it is the first
            // entity on which an operation is performed with the cross-entity transaction client.
            await senderB.SendMessageAsync(new ServiceBusMessage());

            ServiceBusReceivedMessage receivedMessage = await receiverA.ReceiveMessageAsync();

            using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                // This will through an InvalidOperationException because a "receive" cannot be
                // routed through a different entity.
                await receiverA.CompleteMessageAsync(receivedMessage);
                await senderB.SendMessageAsync(new ServiceBusMessage());
                await senderC.SendMessageAsync(new ServiceBusMessage());
                ts.Complete();
            }

            receivedMessage = await receiverA.ReceiveMessageAsync();
            Assert.Null(receivedMessage);
        }
    }
}
