using NServiceBus;
using SFA.DAS.CommitmentsV2.Messages.Events;

namespace SFA.DAS.Payments.DataLocks.Tests.Specs.StepDefinitions
{
    public class MessagingContext
    {
        private IEndpointInstance endpointInstance;

        public MessagingContext()
        {
            this.endpointInstance = TestRunBindings.DASEndpoint;
        }

        public async Task Send<T>(T message)
        {
            await endpointInstance.Publish(message);
        }
    }
}