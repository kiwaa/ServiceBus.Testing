using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace ServiceBus.Testing.UnitTests.Samples
{
    public class Sample08_Interop
    {
        private string QueueName => Guid.NewGuid().ToString();

        [Fact]
        public async Task TestInterop()
        {
            var queueName = QueueName;
            await using var client = new TestableServiceBusClient();

            // Scenario #1 - Sending a message using Azure.Messaging.ServiceBus that will be received with WindowsAzure.ServiceBus
            var sender = client.CreateSender(queueName);
            // When constructing the `DataContractSerializer`, We pass in the type for the model, which can be a strongly typed model or some pre-serialized data.
            // If you use a strongly typed model here, the model properties will be serialized into XML. Since JSON is more commonly used, we will use it in our example, and
            // and specify the type as string, since we will provide a JSON string.
            var serializer = new DataContractSerializer(typeof(string));
            using var stream = new MemoryStream();
            XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(stream);

            // serialize an instance of our type into a JSON string
            string json = JsonSerializer.Serialize(new TestModel { A = "Hello world", B = 5, C = true });

            // serialize our JSON string into the XML envelope using the DataContractSerializer
            serializer.WriteObject(writer, json);
            writer.Flush();

            // construct the ServiceBusMessage using the DataContract serialized JSON
            var message = new ServiceBusMessage(stream.ToArray());

            await sender.SendMessageAsync(message);

            // Scenario #2 - Receiving a message using Azure.Messaging.ServiceBus that was sent with WindowsAzure.ServiceBus
            var receiver = client.CreateReceiver(queueName);
            var received = await receiver.ReceiveMessageAsync();

            // Similar to the send scenario, we still rely on the DataContractSerializer and we use string as our type because we are expecting a JSON
            // message body.
            var deserializer = new DataContractSerializer(typeof(string));
            XmlDictionaryReader reader =
                XmlDictionaryReader.CreateBinaryReader(received.Body.ToStream(), XmlDictionaryReaderQuotas.Max);

            // deserialize the XML envelope into a string
            string receivedJson = (string)deserializer.ReadObject(reader);

            // deserialize the JSON string into TestModel
            TestModel output = JsonSerializer.Deserialize<TestModel>(receivedJson);

            Assert.Equal("Hello world", output.A);
            Assert.Equal(5, output.B);
            Assert.True(output.C);
        }

        public class TestModel
        {
            public string A { get; set; }
            public int B { get; set; }
            public bool C { get; set; }
        }
    }
}
