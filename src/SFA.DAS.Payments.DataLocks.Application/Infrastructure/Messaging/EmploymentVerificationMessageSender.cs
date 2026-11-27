using System;
using System.Threading.Tasks;
using NServiceBus;
using SFA.DAS.EmploymentVerificationService.Messages.Learner.Commands;
using SFA.DAS.Payments.Core.Configuration;

namespace SFA.DAS.Payments.DataLocks.Application.Infrastructure.Messaging
{
    public interface IEmploymentVerificationMessageSender
    {
        Task Send(VerifyNotEmployed message);
    }

    public class EmploymentVerificationMessageSender : IEmploymentVerificationMessageSender
    {
        private readonly IConfigurationHelper configHelper;
        private readonly IEmploymentVerificationEndpoint endpointProvider;
        private readonly string evsQueueName;

        public EmploymentVerificationMessageSender(
            IEmploymentVerificationEndpoint endpointProvider,
            string evsQueueName)
        {
            this.endpointProvider = endpointProvider
                ?? throw new ArgumentNullException(nameof(endpointProvider));
            this.evsQueueName = evsQueueName
                ?? throw new ArgumentNullException(nameof(evsQueueName));
        }

        public async Task Send(VerifyNotEmployed message)
        {
            var endpoint = await endpointProvider.GetEndpoint();
            var sendOptions = new SendOptions();
            sendOptions.SetDestination(evsQueueName);

            await endpoint.Send(message, sendOptions);
        }
    }
}