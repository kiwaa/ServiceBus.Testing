using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBus.Testing.Queues
{
    internal class PeekLockCircularBuffer : IQueue
    {
        private const int BufferSize = 1024;
        private readonly Message[] buffer = new Message[BufferSize];
        private int start = 0;
        private int end = 0;

        public ValueTask AddAsync(ServiceBusMessage message)
        {
            buffer[end++] = new Message(message);
            return ValueTask.CompletedTask;
        }

        public async ValueTask AddAllAsync(IEnumerable<ServiceBusMessage> batch)
        {
            foreach (var message in batch)
                await AddAsync(message);
        }

        public IAsyncEnumerable<ServiceBusReceivedMessage> GetAllAsync(int maxMessages = int.MaxValue, CancellationToken cancellationToken = default)
        {
            var list = new List<ServiceBusReceivedMessage>();
            var tmp = start;
            while (list.Count < maxMessages && tmp < end)
            {
                var msg = buffer[tmp++];
                if (msg.Completed)
                    continue;
                msg.Lock();
                list.Add(msg.ToReceivedMessage());
            }

            return list.ToAsyncEnumerable();
        }

        public ValueTask<ServiceBusReceivedMessage> GetAsync(CancellationToken cancellationToken = default)
        {
            var message = buffer[start];
            while (start < end && message.Completed)
            {
                buffer[start] = null;
                message = buffer[++start];
            }
            if (start == end)
                return ValueTask.FromResult((ServiceBusReceivedMessage)null);
            message.Lock();
            return ValueTask.FromResult(message.ToReceivedMessage());
        }

        public ValueTask<ServiceBusReceivedMessage> GetAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            var tmp = start;
            var message = buffer[tmp];
            while (tmp < end && (message.Completed || message.SessionId != sessionId))
            {
                message = buffer[++tmp];
            }
            if (start == end)
                return ValueTask.FromResult((ServiceBusReceivedMessage)null);
            message.Lock();
            return ValueTask.FromResult(message.ToReceivedMessage());
        }

        public ValueTask Complete(ServiceBusReceivedMessage message)
        {
            var token = Guid.Parse(message.LockToken);
            for (int i = start; i < end; i++)
                if (buffer[i] != null && buffer[i].LockToken == token)
                {
                    buffer[i].Complete();
                }
            return ValueTask.CompletedTask;
        }

        public ValueTask Abandon(ServiceBusReceivedMessage message)
        {
            var token = Guid.Parse(message.LockToken);
            for (int i = start; i < end; i++)
                if (buffer[i] != null && buffer[i].LockToken == token)
                {
                    buffer[i].Unlock();
                }
            return ValueTask.CompletedTask;
        }

        internal class Message
        {
            public BinaryData Body { get; }
            public string MessageId { get; }
            public string SessionId { get; }

            public Guid? LockToken { get; private set; }

            public bool Completed { get; private set; }

            public Message(ServiceBusMessage serviceBusMessage)
            {
                Body = serviceBusMessage.Body;
                MessageId = serviceBusMessage.MessageId;
                SessionId = serviceBusMessage.SessionId;
            }
            public void Lock()
            {
                LockToken = Guid.NewGuid();
            }
            public void Unlock()
            {
                LockToken = null;
            }
            public void Complete()
            {
                Completed = true;
            }

            public ServiceBusReceivedMessage ToReceivedMessage()
            {
                return ServiceBusModelFactory.ServiceBusReceivedMessage(
                    body: Body,
                    messageId: MessageId,
                    sessionId: SessionId,
                    lockTokenGuid: LockToken ?? Guid.Empty);
            }

        }
    }
}
