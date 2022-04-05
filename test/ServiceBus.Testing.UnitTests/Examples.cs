using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

// https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/servicebus/Azure.Messaging.ServiceBus#examples

namespace ServiceBus.Testing.UnitTests
{
    public class Examples
    {
        [Fact]
        public async Task SendAndReceiveMessage()
        {
            string queueName = "ServiceBus.Testing";

            await using var client = new TestableServiceBusClient();
            var sender = client.CreateSender(queueName);

            var message = new ServiceBusMessage("Hello world!");
            await sender.SendMessageAsync(message);

            var receiver = client.CreateReceiver(queueName);
            var receivedMessage = await receiver.ReceiveMessageAsync();

            string body = receivedMessage.Body.ToString();
            Assert.Equal("Hello world!", body);
        }

        [Fact]
        public async Task SendAndReceiveBatchOfMessages()
        {
            string queueName = "ServiceBus.Testing";

            await using var client = new TestableServiceBusClient();
            var sender = client.CreateSender(queueName);

            Queue<ServiceBusMessage> messages = new Queue<ServiceBusMessage>();
            messages.Enqueue(new ServiceBusMessage("First message"));
            messages.Enqueue(new ServiceBusMessage("Second message"));
            messages.Enqueue(new ServiceBusMessage("Third message"));

            int messageCount = messages.Count;

            while (messages.Count > 0)
            {
                using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();

                if (messageBatch.TryAddMessage(messages.Peek()))
                {
                    messages.Dequeue();
                }
                else
                {
                    throw new Exception($"Message {messageCount - messages.Count} is too large and cannot be sent.");
                }

                while (messages.Count > 0 && messageBatch.TryAddMessage(messages.Peek()))
                {
                    messages.Dequeue();
                }

                await sender.SendMessagesAsync(messageBatch);
            }

            var receiver = client.CreateReceiver(queueName);
            var receivedMessages = await receiver.ReceiveMessagesAsync()
                .Select(x => x.Body.ToString())
                .ToListAsync();

            Assert.Equal(new[]
            {
                "First message",
                "Second message",
                "Third message"
            }, receivedMessages);
        }

        [Fact]
        public async Task SendAndReceiveBatchOfMessages_Overload()
        {
            string queueName = "ServiceBus.Testing";

            await using var client = new TestableServiceBusClient();
            var sender = client.CreateSender(queueName);

            IList<ServiceBusMessage> messages = new List<ServiceBusMessage>();
            messages.Add(new ServiceBusMessage("First"));
            messages.Add(new ServiceBusMessage("Second"));
            await sender.SendMessagesAsync(messages);

            var receiver = client.CreateReceiver(queueName);
            var receivedMessages = await receiver.ReceiveMessagesAsync()
                .Select(x => x.Body.ToString())
                .ToListAsync();

            Assert.Equal(new[]
            {
                "First",
                "Second"
            }, receivedMessages);
        }

        [Fact]
        public async Task UsingProcessor()
        {
            string queueName = "ServiceBus.Testing";

            await using var client = new TestableServiceBusClient();
            var sender = client.CreateSender(queueName);

            ServiceBusMessage[] messages = new ServiceBusMessage[]
            {
                new ServiceBusMessage("First"),
                new ServiceBusMessage("Second")
            };

            await sender.SendMessagesAsync(messages);

            // create the options to use for configuring the processor
            var options = new ServiceBusProcessorOptions
            {
                // By default or when AutoCompleteMessages is set to true, the processor will complete the message after executing the message handler
                // Set AutoCompleteMessages to false to [settle messages](https://docs.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement#peeklock) on your own.
                // In both cases, if the message handler throws an exception without settling the message, the processor will abandon the message.
                AutoCompleteMessages = false,

                // I can also allow for multi-threading
                MaxConcurrentCalls = 2
            };

            // create a processor that we can use to process the messages
            await using ServiceBusProcessor processor = client.CreateProcessor(queueName, options);

            // configure the message and error handler to use
            processor.ProcessMessageAsync += MessageHandler;
            processor.ProcessErrorAsync += ErrorHandler;

            async Task MessageHandler(ProcessMessageEventArgs args)
            {
                string body = args.Message.Body.ToString();
                Console.WriteLine(body);

                // we can evaluate application logic and use that to determine how to settle the message.
                await args.CompleteMessageAsync(args.Message);
            }

            Task ErrorHandler(ProcessErrorEventArgs args)
            {
                // the error source tells me at what point in the processing an error occurred
                Console.WriteLine(args.ErrorSource);
                // the fully qualified namespace is available
                Console.WriteLine(args.FullyQualifiedNamespace);
                // as well as the entity path
                Console.WriteLine(args.EntityPath);
                Console.WriteLine(args.Exception.ToString());
                return Task.CompletedTask;
            }

            // start processing
            await processor.StartProcessingAsync();

            // since the processing happens in the background, we add a Console.ReadKey to allow the processing to continue until a key is pressed.
            Console.ReadKey();
        }

        [Fact]
        public async Task CompleteMessage()
        {
            string queueName = "ServiceBus.Testing";

            await using var client = new TestableServiceBusClient();
            var sender = client.CreateSender(queueName);

            ServiceBusMessage message = new ServiceBusMessage("Hello world!");
            await sender.SendMessageAsync(message);

            ServiceBusReceiver receiver = client.CreateReceiver(queueName);
            ServiceBusReceivedMessage receivedMessage = await receiver.ReceiveMessageAsync();

            // complete the message, thereby deleting it from the service
            await receiver.CompleteMessageAsync(receivedMessage);
        }

        [Fact]
        public async Task AbandonMessage()
        {
            string queueName = "ServiceBus.Testing";

            await using var client = new TestableServiceBusClient();
            var sender = client.CreateSender(queueName);

            ServiceBusMessage message = new ServiceBusMessage("Hello world!");
            await sender.SendMessageAsync(message);

            ServiceBusReceiver receiver = client.CreateReceiver(queueName);
            ServiceBusReceivedMessage receivedMessage = await receiver.ReceiveMessageAsync();

            // abandon the message, thereby releasing the lock and allowing it to be received again by this or other receivers
            await receiver.AbandonMessageAsync(receivedMessage);
        }

        [Fact]
        public async Task DeferMessage()
        {
            string queueName = "ServiceBus.Testing";

            await using var client = new TestableServiceBusClient();
            var sender = client.CreateSender(queueName);

            ServiceBusMessage message = new ServiceBusMessage("Hello world!");
            await sender.SendMessageAsync(message);

            ServiceBusReceiver receiver = client.CreateReceiver(queueName);
            ServiceBusReceivedMessage receivedMessage = await receiver.ReceiveMessageAsync();

            // defer the message, thereby preventing the message from being received again without using
            // the received deferred message API.
            await receiver.DeferMessageAsync(receivedMessage);

            // receive the deferred message by specifying the service set sequence number of the original
            // received message
            ServiceBusReceivedMessage deferredMessage = await receiver.ReceiveDeferredMessageAsync(receivedMessage.SequenceNumber);
        }

        [Fact]
        public async Task DeadLetterMessage()
        {
            string queueName = "ServiceBus.Testing";

            await using var client = new TestableServiceBusClient();
            var sender = client.CreateSender(queueName);

            ServiceBusMessage message = new ServiceBusMessage("Hello world!");
            await sender.SendMessageAsync(message);

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
        }
    }
}
