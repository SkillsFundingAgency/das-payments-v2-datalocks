using System;
using System.Threading.Tasks;
using NServiceBus;
using SFA.DAS.EmploymentVerificationService.Messages.Learner.Commands;
using SFA.DAS.Payments.Application.Messaging;

namespace SFA.DAS.Payments.DataLocks.Application.Infrastructure.Messaging
{
    public interface IEmploymentVerificationRequestService
    {
        Task PublishVerifyNotEmployed(
            Guid correlationId,
            long apprenticeshipId,
            long uln,
            long accountId,
            DateTime employmentStartDate,
            int durationInDays);
    }

    public class EmploymentVerificationRequestService : IEmploymentVerificationRequestService
    {
        private readonly IEndpointInstanceFactory evsEndpointInstanceFactory;

        public EmploymentVerificationRequestService(IEndpointInstanceFactory evsEndpointInstanceFactory)
        {
            this.evsEndpointInstanceFactory = evsEndpointInstanceFactory
                ?? throw new ArgumentNullException(nameof(evsEndpointInstanceFactory));
        }

        public async Task PublishVerifyNotEmployed(
            Guid correlationId,
            long apprenticeshipId,
            long uln,
            long accountId,
            DateTime employmentStartDate,
            int durationInDays)
        {
            var message = new VerifyNotEmployed
            {
                CorrelationId = correlationId,
                ApprenticeshipId = apprenticeshipId,
                ULN = uln,
                EmployerAccountIdentifier = accountId,
                StartDate = employmentStartDate,
                Reference = $"Apprenticeship Hiring Payment Eligibility Check - ApprenticeshipId:{apprenticeshipId}",
                DurationInDays = durationInDays
            };

            var endpoint = await evsEndpointInstanceFactory
                .GetEndpointInstance()
                .ConfigureAwait(false);

            await endpoint.Publish(message, new PublishOptions()).ConfigureAwait(false);
        }
    }
}