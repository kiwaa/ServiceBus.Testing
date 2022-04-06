//using Azure.Messaging.ServiceBus;
//using System;
//using System.Threading.Tasks;
//using Xunit;

//namespace ServiceBus.Testing.UnitTests.Samples
//{
//    public class Sample10_ClaimCheck
//    {
//        private string QueueName => Guid.NewGuid().ToString();
//        [Fact]
//        public async Task ClaimCheck()
//        {
//                var containerClient = new BlobContainerClient(TestEnvironment.StorageClaimCheckConnectionString, "claim-checks");
//                await containerClient.CreateIfNotExistsAsync();

//                try
//                {
//                    byte[] body = ServiceBusTestUtilities.GetRandomBuffer(1000000);
//                    string blobName = Guid.NewGuid().ToString();
//                    await containerClient.UploadBlobAsync(blobName, new BinaryData(body));
//                    var message = new ServiceBusMessage
//                    {
//                        ApplicationProperties =
//                        {
//                            ["blob-name"] = blobName
//                        }
//                    };


//                    var client = new TestableServiceBusClient();
//                    ServiceBusSender sender = client.CreateSender(QueueName);
//                    await sender.SendMessageAsync(message);


//                    ServiceBusReceiver receiver = client.CreateReceiver(QueueName);
//                    ServiceBusReceivedMessage receivedMessage = await receiver.ReceiveMessageAsync();
//                    if (receivedMessage.ApplicationProperties.TryGetValue("blob-name", out object blobNameReceived))
//                    {
//                        var blobClient = new BlobClient(
//                            TestEnvironment.StorageClaimCheckConnectionString,
//                            "claim-checks",
//                            (string)blobNameReceived);
//                        BlobDownloadResult downloadResult = await blobClient.DownloadContentAsync();
//                        BinaryData messageBody = downloadResult.Content;

//                        // Once we determine that we are done with the message, we complete it and delete the corresponding blob.
//                        await receiver.CompleteMessageAsync(receivedMessage);
//                        await blobClient.DeleteAsync();
//                        Assert.Equal(body, messageBody.ToArray());
//                    }
//                }
//                finally
//                {
//                    await containerClient.DeleteAsync();
//                }
//            }
//        }
//    }
//}
