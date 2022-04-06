using Azure.Messaging.ServiceBus;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ServiceBus.Testing.UnitTests.Samples
{
    public class Sample03_SendReceiveSessions
    {
        private string QueueName => Guid.NewGuid().ToString();

        [Fact(Skip = "not supported yet")]
        public async Task SendAndReceiveSessionMessage()
        {
            string queueName = QueueName;
            // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
            await using var client = new TestableServiceBusClient();

            // create the sender
            var sender = client.CreateSender(queueName);

            // create a session message that we can send
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes("Hello world!"))
            {
                SessionId = "mySessionId"
            };

            // send the message
            await sender.SendMessageAsync(message);

            // create a session receiver that we can use to receive the message. Since we don't specify a
            // particular session, we will get the next available session from the service.
            var receiver = await client.AcceptNextSessionAsync(queueName);

            // the received message is a different type as it contains some service set properties
            var receivedMessage = await receiver.ReceiveMessageAsync();
            Console.WriteLine(receivedMessage.SessionId);

            // we can also set arbitrary session state using this receiver
            // the state is specific to the session, and not any particular message
            await receiver.SetSessionStateAsync(new BinaryData("some state"));

            // the state can be retrieved for the session as well
            BinaryData state = await receiver.GetSessionStateAsync();

            Assert.Equal(Encoding.UTF8.GetBytes("Hello world!"), receivedMessage.Body.ToArray());
            Assert.Equal("mySessionId", receivedMessage.SessionId);
            Assert.Equal(Encoding.UTF8.GetBytes("some state"), state.ToArray());
        }

        [Fact]
        public async Task ReceiveFromSpecificSession()
        {
            string queueName = QueueName;
            // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
            await using var client = new TestableServiceBusClient();

            // create the sender
            var sender = client.CreateSender(queueName);

            // create a message batch that we can send
            var messageBatch = await sender.CreateMessageBatchAsync();
            messageBatch.TryAddMessage(
                new ServiceBusMessage("First")
                {
                    SessionId = "Session1"
                });
            messageBatch.TryAddMessage(
                new ServiceBusMessage("Second")
                {
                    SessionId = "Session2"
                });

            // send the message batch
            await sender.SendMessagesAsync(messageBatch);

            // create a receiver specifying a particular session
            var receiver = await client.AcceptSessionAsync(queueName, "Session2");

            // the received message is a different type as it contains some service set properties
            var receivedMessage = await receiver.ReceiveMessageAsync();
            Console.WriteLine(receivedMessage.SessionId);

            Assert.Equal("Second", receivedMessage.Body.ToString());
            Assert.Equal("Session2", receivedMessage.SessionId);

        }
    }
}
