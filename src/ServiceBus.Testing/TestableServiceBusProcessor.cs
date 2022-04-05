using Azure.Messaging.ServiceBus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBus.Testing
{
    internal class TestableServiceBusProcessor : ServiceBusProcessor
    {

        private bool isClosed;
        public override bool IsClosed => isClosed;
        public TestableServiceBusProcessor() : base()
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
