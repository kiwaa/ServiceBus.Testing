using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ServiceBus.Testing.UnitTests.Samples
{
    public class Sample01_HelloWorld
    {
        private string QueueName => Guid.NewGuid().ToString();

        [Fact]
        public async Task SendAndReceiveMessage()
        {
            string queueName = QueueName;

            // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
            await using var client = new TestableServiceBusClient();

            // create the sender
            var sender = client.CreateSender(queueName);

            // create a message that we can send. UTF-8 encoding is used when providing a string.
            var message = new ServiceBusMessage("Hello world!");

            // send the message
            await sender.SendMessageAsync(message);

            // create a receiver that we can use to receive the message
            ServiceBusReceiver receiver = client.CreateReceiver(queueName);

            // the received message is a different type as it contains some service set properties
            ServiceBusReceivedMessage receivedMessage = await receiver.ReceiveMessageAsync();

            // get the message body as a string
            string body = receivedMessage.Body.ToString();
            Assert.Equal("Hello world!", receivedMessage.Body.ToString());

        }

        [Fact]
        public async Task SendAndPeekMessage()
        {
            string queueName = QueueName;
            await using var client = new TestableServiceBusClient();

            // create the sender
            ServiceBusSender sender = client.CreateSender(queueName);

            // create a message that we can send
            ServiceBusMessage message = new ServiceBusMessage("Hello world!");

            // send the message
            await sender.SendMessageAsync(message);

            // create a receiver that we can use to receive the message
            ServiceBusReceiver receiver = client.CreateReceiver(queueName);

            ServiceBusReceivedMessage peekedMessage = await receiver.PeekMessageAsync();

            // get the message body as a string
            string body = peekedMessage.Body.ToString();
            Assert.Equal("Hello world!", peekedMessage.Body.ToString());
        }

        [Fact]
        public async Task SendAndReceiveMessageBatch()
        {
            string queueName = QueueName;

            // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
            await using var client = new TestableServiceBusClient();

            // create the sender
            ServiceBusSender sender = client.CreateSender(queueName);
            IList<ServiceBusMessage> messages = new List<ServiceBusMessage>();
            messages.Add(new ServiceBusMessage("First"));
            messages.Add(new ServiceBusMessage("Second"));
            // send the messages
            await sender.SendMessagesAsync(messages);
            // create a receiver that we can use to receive the messages
            ServiceBusReceiver receiver = client.CreateReceiver(queueName);

            // the received message is a different type as it contains some service set properties
            IReadOnlyList<ServiceBusReceivedMessage> receivedMessages = await receiver.ReceiveMessagesAsync(maxMessages: 2);

            foreach (ServiceBusReceivedMessage receivedMessage in receivedMessages)
            {
                // get the message body as a string
                string body = receivedMessage.Body.ToString();
                Console.WriteLine(body);
            }

            var sentMessagesEnum = messages.GetEnumerator();
            foreach (ServiceBusReceivedMessage receivedMessage in receivedMessages)
            {
                sentMessagesEnum.MoveNext();
                Assert.Equal(sentMessagesEnum.Current.Body.ToString(), receivedMessage.Body.ToString());
            }

        }

        [Fact]
        public async Task SendAndReceiveMessageSafeBatch()
        {
            string queueName = QueueName;
            // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
            await using var client = new TestableServiceBusClient();

            // create the sender
            ServiceBusSender sender = client.CreateSender(queueName);

            // add the messages that we plan to send to a local queue
            Queue<ServiceBusMessage> messages = new Queue<ServiceBusMessage>();
            messages.Enqueue(new ServiceBusMessage("First message"));
            messages.Enqueue(new ServiceBusMessage("Second message"));
            messages.Enqueue(new ServiceBusMessage("Third message"));

            // create a message batch that we can send
            // total number of messages to be sent to the Service Bus queue
            int messageCount = messages.Count;

            // while all messages are not sent to the Service Bus queue
            while (messages.Count > 0)
            {
                // start a new batch
                using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();

                // add the first message to the batch
                if (messageBatch.TryAddMessage(messages.Peek()))
                {
                    // dequeue the message from the .NET queue once the message is added to the batch
                    messages.Dequeue();
                }
                else
                {
                    // if the first message can't fit, then it is too large for the batch
                    throw new Exception($"Message {messageCount - messages.Count} is too large and cannot be sent.");
                }

                // add as many messages as possible to the current batch
                while (messages.Count > 0 && messageBatch.TryAddMessage(messages.Peek()))
                {
                    // dequeue the message from the .NET queue as it has been added to the batch
                    messages.Dequeue();
                }

                // now, send the batch
                await sender.SendMessagesAsync(messageBatch);

                // if there are any remaining messages in the .NET queue, the while loop repeats
            }

            // create a receiver that we can use to receive the messages
            ServiceBusReceiver receiver = client.CreateReceiver(queueName);

            // the received message is a different type as it contains some service set properties
            IReadOnlyList<ServiceBusReceivedMessage> receivedMessages = await receiver.ReceiveMessagesAsync(maxMessages: 2);

            foreach (ServiceBusReceivedMessage receivedMessage in receivedMessages)
            {
                // get the message body as a string
                string body = receivedMessage.Body.ToString();
                Console.WriteLine(body);
            }

            var list = new List<ServiceBusMessage>
                {
                    new ServiceBusMessage("First message"),
                    new ServiceBusMessage("Second message"),
                    new ServiceBusMessage("Third message")
                };
            var sentMessagesEnum = list.GetEnumerator();
            foreach (ServiceBusReceivedMessage receivedMessage in receivedMessages)
            {
                sentMessagesEnum.MoveNext();
                Assert.Equal(sentMessagesEnum.Current.Body.ToString(), receivedMessage.Body.ToString());
            }
        }

        [Fact(Skip = "not supported yet")]
        public async Task ScheduleMessage()
        {
            string queueName = QueueName;
            // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
            await using var client = new TestableServiceBusClient();

            // create the sender
            ServiceBusSender sender = client.CreateSender(queueName);

            // create a message that we can send
            ServiceBusMessage message = new ServiceBusMessage("Hello world!");

            long seq = await sender.ScheduleMessageAsync(
                message,
                DateTimeOffset.Now.AddDays(1));

            // create a receiver that we can use to peek the message
            ServiceBusReceiver receiver = client.CreateReceiver(queueName);
            Assert.NotNull(await receiver.PeekMessageAsync());

            // cancel the scheduled messaged, thereby deleting from the service
            await sender.CancelScheduledMessageAsync(seq);
            Assert.Null(await receiver.PeekMessageAsync());

        }
    }

}
