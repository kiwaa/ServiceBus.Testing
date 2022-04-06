using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ServiceBus.Testing.UnitTests.Samples
{
    public class Sample07_CrudOperations
    {
        [Fact(Skip = "not supported yet")]
        public async Task CreateQueue()
        {
            string adminQueueName = Guid.NewGuid().ToString("D").Substring(0, 8);

            string queueName = adminQueueName;
            var client = new TestableServiceBusAdministrationClient();
            var options = new CreateQueueOptions(queueName)
            {
                AutoDeleteOnIdle = TimeSpan.FromDays(7),
                DefaultMessageTimeToLive = TimeSpan.FromDays(2),
                DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(1),
                EnableBatchedOperations = true,
                DeadLetteringOnMessageExpiration = true,
                EnablePartitioning = false,
                ForwardDeadLetteredMessagesTo = null,
                ForwardTo = null,
                LockDuration = TimeSpan.FromSeconds(45),
                MaxDeliveryCount = 8,
                MaxSizeInMegabytes = 2048,
                RequiresDuplicateDetection = true,
                RequiresSession = true,
                UserMetadata = "some metadata"
            };

            options.AuthorizationRules.Add(new SharedAccessAuthorizationRule(
                "allClaims",
                new[] { AccessRights.Manage, AccessRights.Send, AccessRights.Listen }));

            QueueProperties createdQueue = await client.CreateQueueAsync(options);
            Assert.Equal(options, new CreateQueueOptions(createdQueue) { MaxMessageSizeInKilobytes = options.MaxMessageSizeInKilobytes });
        }

        [Fact(Skip = "not supported yet")]
        public async Task GetUpdateDeleteQueue()
        {
            string queueName = Guid.NewGuid().ToString("D").Substring(0, 8);
            var client = new TestableServiceBusAdministrationClient();
            var qd = new CreateQueueOptions(queueName);
            await client.CreateQueueAsync(qd);

            QueueProperties queue = await client.GetQueueAsync(queueName);
            queue.LockDuration = TimeSpan.FromSeconds(60);
            QueueProperties updatedQueue = await client.UpdateQueueAsync(queue);
            Assert.Equal(TimeSpan.FromSeconds(60), updatedQueue.LockDuration);
            await client.DeleteQueueAsync(queueName);
            var ex = await Assert.ThrowsAsync<ServiceBusException>(async () => await client.GetQueueAsync(queueName));
            Assert.Equal(ServiceBusFailureReason.MessagingEntityNotFound, ex.Reason);
        }

        [Fact(Skip = "not supported yet")]
        public async Task CreateTopicAndSubscription()
        {
            string adminTopicName = Guid.NewGuid().ToString("D").Substring(0, 8);
            string adminSubscriptionName = Guid.NewGuid().ToString("D").Substring(0, 8);

            string topicName = adminTopicName;
            var client = new TestableServiceBusAdministrationClient();
            var topicOptions = new CreateTopicOptions(topicName)
            {
                AutoDeleteOnIdle = TimeSpan.FromDays(7),
                DefaultMessageTimeToLive = TimeSpan.FromDays(2),
                DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(1),
                EnableBatchedOperations = true,
                EnablePartitioning = false,
                MaxSizeInMegabytes = 2048,
                RequiresDuplicateDetection = true,
                UserMetadata = "some metadata"
            };

            topicOptions.AuthorizationRules.Add(new SharedAccessAuthorizationRule(
                "allClaims",
                new[] { AccessRights.Manage, AccessRights.Send, AccessRights.Listen }));

            TopicProperties createdTopic = await client.CreateTopicAsync(topicOptions);


            string subscriptionName = adminSubscriptionName;
            var subscriptionOptions = new CreateSubscriptionOptions(topicName, subscriptionName)
            {
                AutoDeleteOnIdle = TimeSpan.FromDays(7),
                DefaultMessageTimeToLive = TimeSpan.FromDays(2),
                EnableBatchedOperations = true,
                UserMetadata = "some metadata"
            };
            SubscriptionProperties createdSubscription = await client.CreateSubscriptionAsync(subscriptionOptions);
            Assert.Equal(topicOptions, new CreateTopicOptions(createdTopic) { MaxMessageSizeInKilobytes = topicOptions.MaxMessageSizeInKilobytes });
            Assert.Equal(subscriptionOptions, new CreateSubscriptionOptions(createdSubscription));

        }

        [Fact(Skip = "not supported yet")]
        public async Task GetUpdateDeleteTopicAndSubscription()
        {
            string topicName = Guid.NewGuid().ToString("D").Substring(0, 8);
            string subscriptionName = Guid.NewGuid().ToString("D").Substring(0, 8);
            var client = new TestableServiceBusAdministrationClient();
            var topicOptions = new CreateTopicOptions(topicName);
            var subscriptionOptions = new CreateSubscriptionOptions(topicName, subscriptionName);
            await client.CreateTopicAsync(topicOptions);
            await client.CreateSubscriptionAsync(subscriptionOptions);
            TopicProperties topic = await client.GetTopicAsync(topicName);
            SubscriptionProperties subscription = await client.GetSubscriptionAsync(topicName, subscriptionName);
            topic.UserMetadata = "some metadata";
            TopicProperties updatedTopic = await client.UpdateTopicAsync(topic);
            Assert.Equal("some metadata", updatedTopic.UserMetadata);

            subscription.UserMetadata = "some metadata";
            SubscriptionProperties updatedSubscription = await client.UpdateSubscriptionAsync(subscription);
            Assert.Equal("some metadata", updatedSubscription.UserMetadata);

            // need to delete the subscription before the topic, as deleting
            // the topic would automatically delete the subscription
            await client.DeleteSubscriptionAsync(topicName, subscriptionName);
            var ex = await Assert.ThrowsAsync<ServiceBusException>(async () => await client.GetSubscriptionAsync(topicName, subscriptionName));
            Assert.Equal(ServiceBusFailureReason.MessagingEntityNotFound, ex.Reason);

            await client.DeleteTopicAsync(topicName);
            var ex2 = await Assert.ThrowsAsync<ServiceBusException>(async () => await client.GetTopicAsync(topicName));
            Assert.Equal(ServiceBusFailureReason.MessagingEntityNotFound, ex2.Reason);
        }
    }
}
