using Azure.Messaging.ServiceBus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBus.Testing
{
    internal class TestableServiceBusSessionReceiver : ServiceBusProcessor
    {

        private bool isClosed;
        public override bool IsClosed => isClosed;
        public TestableServiceBusSessionReceiver() : base()
        {
            throw new NotImplementedException();
        }

        public override Task CloseAsync(CancellationToken cancellationToken = default)
        {
            isClosed = true;
            return Task.CompletedTask;
        }
    }
}
