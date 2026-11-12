using System;
using System.Threading.Tasks;
using SFA.DAS.Payments.Application.Infrastructure.Logging;
using SFA.DAS.Payments.DataLocks.Domain.Services.Apprenticeships;
using SFA.DAS.Payments.DataLocks.Messages.Events;

namespace SFA.DAS.Payments.DataLocks.Application.Services
{
    public interface IApprenticeshipEmploymentVerificationProcessor
    {
        Task Process(VerifyNotEmployedCompleted completedMessage);
    }

    public class ApprenticeshipEmploymentVerificationProcessor : IApprenticeshipEmploymentVerificationProcessor
    {
        private readonly IPaymentLogger logger;
        private readonly IApprenticeshipEmploymentVerificationService employmentVerificationService;

        public ApprenticeshipEmploymentVerificationProcessor(IPaymentLogger logger, IApprenticeshipEmploymentVerificationService employmentVerificationService)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.employmentVerificationService = employmentVerificationService ?? throw new ArgumentNullException(nameof(employmentVerificationService));
        }

        public async Task Process(VerifyNotEmployedCompleted completedMessage)
        {
            try
            {
                if (completedMessage == null)
                {
                    throw new ArgumentNullException(nameof(completedMessage));
                }

                logger.LogDebug($"Processing VerifyNotEmployedCompleted for Apprenticeship id: {completedMessage.ApprenticeshipId}, CorrelationId: {completedMessage.CorrelationId}.");

                var status = completedMessage.NotEmployed
                    ? EmploymentCheckResult.NotEmployed
                    : EmploymentCheckResult.Employed;

                var updated = await employmentVerificationService
                    .UpdatePreEmploymentStatus(completedMessage.CorrelationId, completedMessage.ApprenticeshipId, status)
                    .ConfigureAwait(false);

                if (!updated)
                {
                    logger.LogWarning($"Unable to find an employment verification record for Apprenticeship id: {completedMessage.ApprenticeshipId} and CorrelationId: {completedMessage.CorrelationId}.");
                    return;
                }

                logger.LogInfo($"Employment verification completed for apprenticeship {completedMessage.ApprenticeshipId}. Status updated to {status}.");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing VerifyNotEmployedCompleted for Apprenticeship id: {completedMessage?.ApprenticeshipId}. Error: {ex.Message}", ex);
                throw;
            }
        }
    }
}
