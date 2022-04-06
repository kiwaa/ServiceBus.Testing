using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Tests.Samples;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ServiceBus.Testing.UnitTests.Samples
{
    public class Sample09_Extensibility
    {
        private string QueueName => Guid.NewGuid().ToString();
        [Fact(Skip = "not supported yet")]
        public async Task Plugins()
        {
            await using var client = new TestableServiceBusClient();
            string queueName = QueueName;
            await using ServiceBusSender sender = client.CreatePluginSender(queueName, new List<Func<ServiceBusMessage, Task>>()
                {
                    message =>
                    {
                        message.Subject = "Updated subject";
                        return Task.CompletedTask;
                    },
                    message =>
                    {
                        Assert.Equal("Updated subject", message.Subject);
                        return Task.CompletedTask;
                    },
                });

            await sender.SendMessageAsync(new ServiceBusMessage(Encoding.UTF8.GetBytes("First")));
            await using ServiceBusReceiver receiver = client.CreatePluginReceiver(queueName, new List<Func<ServiceBusReceivedMessage, Task>>()
                {
                    message =>
                    {
                        Assert.Equal("Updated subject", message.Subject);
                        var rawMessage = message.GetRawAmqpMessage();
                        rawMessage.Properties.Subject = "Received subject";
                        return Task.CompletedTask;
                    },
                    message =>
                    {
                        Assert.Equal("Received subject", message.Subject);
                        var rawMessage = message.GetRawAmqpMessage();
                        rawMessage.Properties.Subject = "Last subject";
                        Console.WriteLine("Second receive plugin executed!");
                        return Task.CompletedTask;
                    },
                });
            ServiceBusReceivedMessage message = await receiver.ReceiveMessageAsync();
            Assert.Equal("Last subject", message.Subject);

        }

        [Fact(Skip = "not supported yet")]
        public async Task PluginsSessions()
        {
            await using var client = new TestableServiceBusClient();
            string queueName = QueueName;
            await using ServiceBusSender sender = client.CreatePluginSender(queueName, new List<Func<ServiceBusMessage, Task>>()
                {
                    message =>
                    {
                        message.Subject = "Updated subject";
                        message.SessionId = "sessionId";
                        return Task.CompletedTask;
                    },
                    message =>
                    {
                        Assert.Equal("Updated subject", message.Subject);
                        return Task.CompletedTask;
                    },
                });

            await sender.SendMessageAsync(new ServiceBusMessage(Encoding.UTF8.GetBytes("First")));
            await using ServiceBusReceiver receiver = await client.AccextNextSessionPluginAsync(queueName, new List<Func<ServiceBusReceivedMessage, Task>>()
                {
                    message =>
                    {
                        Assert.Equal("Updated subject", message.Subject);
                        var rawMessage = message.GetRawAmqpMessage();
                        rawMessage.Properties.Subject = "Received subject";
                        return Task.CompletedTask;
                    },
                    message =>
                    {
                        Assert.Equal("Received subject", message.Subject);
                        var rawMessage = message.GetRawAmqpMessage();
                        rawMessage.Properties.Subject = "Last subject";
                        Console.WriteLine("Second receive plugin executed!");
                        return Task.CompletedTask;
                    },
                });
            ServiceBusReceivedMessage message = await receiver.ReceiveMessageAsync();
            Assert.Equal("Last subject", message.Subject);
        }

        [Fact(Skip = "not supported yet")]
        public async Task PluginsProcessor()
        {
            await using var client = new TestableServiceBusClient();
            string queueName = QueueName;
            await using ServiceBusSender sender = client.CreatePluginSender(queueName, new List<Func<ServiceBusMessage, Task>>()
                {
                    message =>
                    {
                        message.Subject = "Updated subject";
                        return Task.CompletedTask;
                    },
                    message =>
                    {
                        Assert.Equal("Updated subject", message.Subject);
                        return Task.CompletedTask;
                    },
                });

            await sender.SendMessageAsync(new ServiceBusMessage(Encoding.UTF8.GetBytes("First")));
            await using ServiceBusProcessor processor = client.CreatePluginProcessor(queueName, new List<Func<ServiceBusReceivedMessage, Task>>()
                {
                    message =>
                    {
                        Assert.Equal("Updated subject", message.Subject);
                        var rawMessage = message.GetRawAmqpMessage();
                        rawMessage.Properties.Subject = "Received subject";
                        return Task.CompletedTask;
                    },
                    message =>
                    {

                        Assert.Equal("Received subject", message.Subject);
                        var rawMessage = message.GetRawAmqpMessage();
                        rawMessage.Properties.Subject = "Last subject";
                        return Task.CompletedTask;
                    },
                });
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            processor.ProcessMessageAsync += args =>
            {
                Assert.Equal("Last subject", args.Message.Subject);
                tcs.TrySetResult(true);
                return Task.CompletedTask;
            };

            processor.ProcessErrorAsync += args =>
            {
                return Task.CompletedTask;
            };

            await processor.StartProcessingAsync();
            await tcs.Task;
        }

        [Fact(Skip = "not supported yet")]
        public async Task PluginsSessionProcessor()
        {
            await using var client = new TestableServiceBusClient();
            string queueName = QueueName;
            await using ServiceBusSender sender = client.CreatePluginSender(queueName, new List<Func<ServiceBusMessage, Task>>()
                {
                    message =>
                    {
                        message.Subject = "Updated subject";
                        message.SessionId = "sessionId";
                    return Task.CompletedTask;
                    },
                    message =>
                    {
                        Assert.Equal("Updated subject", message.Subject);
                        Assert.Equal("sessionId", message.SessionId);
                        return Task.CompletedTask;
                    },
                });

            await sender.SendMessageAsync(new ServiceBusMessage(Encoding.UTF8.GetBytes("First")));

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using ServiceBusSessionProcessor processor = client.CreatePluginSessionProcessor(queueName, new List<Func<ServiceBusReceivedMessage, Task>>()
                {
                    message =>
                    {
                        var rawMessage = message.GetRawAmqpMessage();
                        rawMessage.Properties.Subject = "Received subject";
                        return Task.CompletedTask;
                    },
                    message =>
                    {
                        Assert.Equal("Received subject", message.Subject);
                        var rawMessage = message.GetRawAmqpMessage();
                        rawMessage.Properties.Subject = "Last subject";
                        return Task.CompletedTask;
                    },
                });

            processor.ProcessMessageAsync += args =>
            {
                Assert.Equal("Last subject", args.Message.Subject);
                tcs.TrySetResult(true);

                return Task.CompletedTask;
            };

            processor.ProcessErrorAsync += args =>
            {
                return Task.CompletedTask;
            };

            await processor.StartProcessingAsync();
            await tcs.Task;
        }

    }
}
